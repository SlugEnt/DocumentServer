using System.Reflection;
using ConsoleTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using ILogger = Serilog.ILogger;
using System.Configuration;


namespace SlugEnt.DocumentServer.ConsoleTesting;

public class Program
{
    private static ILogger _logger;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceFolder">Folder that contains source documents that can be used for testing.  It should also contain the document folders for the Y test.</param>
    /// <param name="appToken">Token for the application that is used to test with</param>
    /// <param name="apiKey">The API key</param>
    /// <param name="outputFolder">Where downloaded files are stored.</param>
    /// <param name="doctype">Document Type ID to be used for testing</param>
    /// <param name="appId">Application Id that corresponds to the DocTypeId to be used for testing</param>
    /// <returns></returns>
    public static async Task Main(string sourceFolder = "",
                                  string appToken = "abc",
                                  string apiKey = "",
                                  string outputFolder = @"T:\",
                                  int doctype = 0,
                                  int appId = 0)
    {
#if DEBUG
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                                              .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
#else
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                                              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
#endif
                                              .Enrich.FromLogContext()
                                              .WriteTo.Console()
                                              .CreateLogger().ForContext("SourceContext", "SlugEnt.DocumentServer.Api");
        _logger = Log.Logger;
        Log.Information("Starting {AppName}", Assembly.GetExecutingAssembly().GetName().Name);


        // Get Sensitive Appsettings.json file location
        string? sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");


        // 1.B.  We keep the AppSettings file in the root App folder on the servers so it never gets overwritten
        string         versionPath          = Directory.GetCurrentDirectory();
        DirectoryInfo? appRootDirectoryInfo = Directory.GetParent(versionPath);
        string?        appRoot              = appRootDirectoryInfo.FullName;
        Console.WriteLine("Running from Directory:  " + appRoot);

        // Load Environment Specific App Setting file
        string? environmentName    = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string  appSettingFileName = "appsettings." + environmentName + ".json";
        string  appSettingEnvFile  = Path.Join(appRoot, appSettingFileName);
        DisplayAppSettingStatus(appSettingEnvFile);

        string appSettingEnvFile2 = Path.Join(versionPath, appSettingFileName);
        DisplayAppSettingStatus(appSettingEnvFile2);

        // Load the Sensitive AppSettings.JSON file.
        //string sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        string sensitiveFileName    = "SlugEnt.DocumentServer_AppSettingsSensitive.json";
        string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);
        DisplayAppSettingStatus(sensitiveSettingFile);

        // We also have the ConsoleTesting AppSensitive file
        string sensitiveFileName2    = "SlugEnt.DocumentServer.ConsoleTesting_AppSettingsSensitive.json";
        string sensitiveSettingFile2 = Path.Join(sensitiveAppSettings, sensitiveFileName2);
        DisplayAppSettingStatus(sensitiveSettingFile2);


        // Add our custom AppSettings.JSON files
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile(appSettingEnvFile, true, true)
                                                                     .AddJsonFile(appSettingEnvFile2, true, true)
                                                                     .AddJsonFile(sensitiveSettingFile, true, true)
                                                                     .AddJsonFile(sensitiveSettingFile2, true, true)
                                                                     .Build();


        using IHost host = Host.CreateDefaultBuilder()

                               // Add our custom config from above to the default configuration
                               .ConfigureAppConfiguration(config => { config.AddConfiguration(configuration); })
                               .UseSerilog()
                               .ConfigureServices((_,
                                                   services) =>

                                                      // The main program     
                                                      services
                                                          .AddDbContext<DocServerDbContext>(options =>
                                                          {
                                                              options
                                                                  .UseSqlServer(configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()))

                                                                  // IF Debug then log all SQL to Console
#if (DEBUG || SWAGGER)
                                                                  .LogTo(Console.WriteLine)
                                                                  .EnableDetailedErrors();
#endif
                                                              ;
                                                          })
                                                          .AddTransient<MainMenu>()
                                                          .AddTransient<DocumentServerEngine>()
                                                          .AddSingleton<DocumentServerInformation>(dsi =>
                                                          {
                                                              DocumentServerInformationBuilder dsiBuilder =
                                                                  new DocumentServerInformationBuilder(_logger).UseConfiguration(configuration);
                                                              return dsiBuilder.Build();
                                                          })
                                                          .AddHttpClient<AccessDocumentServerHttpClient>().ConfigurePrimaryHttpMessageHandler(() =>
                                                          {
                                                              return new SocketsHttpHandler
                                                              {
                                                                  PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                              };
                                                          })
                                                          .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                                                 )
                               .Build();


        // Run the Main App. Do NOT AWAIT call
#pragma warning disable CS4014
        host.RunAsync();
        MainMenu mainMenu = host.Services.GetRequiredService<MainMenu>();
        mainMenu.BaseFolder       = sourceFolder;
        mainMenu.SourceFolder     = sourceFolder;
        mainMenu.ApplicationToken = appToken;
        mainMenu.ApiKey           = apiKey;
        mainMenu.DownloadFolder   = outputFolder;
        if (doctype > 0)
            mainMenu.DocumentTypeId = doctype;
        if (appId > 0)
            mainMenu.ApplicationId = appId;

        await mainMenu.Start();
        Log.CloseAndFlush();
#pragma warning restore
    }


    /// <summary>
    ///     Logs whether a given AppSettings file was found to exist.
    /// </summary>
    /// <param name="appSettingFileName"></param>
    private static void DisplayAppSettingStatus(string appSettingFileName)
    {
        if (File.Exists(appSettingFileName))
            _logger.Information("AppSettings File was located.  {AppSettingsFile}", appSettingFileName);
        else
            _logger.Warning("AppSettings File was not found.  {AppSettingsFile}", appSettingFileName);
    }
}