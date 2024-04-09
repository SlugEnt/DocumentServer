using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using SlugEnt.DocumentServer.Db;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace BundleBuilderSimple;

/// <summary>
/// This program exists because we need to build an EF Core Migration Bundle.  But it can't be built off the ConsoleTesting app because
/// it uses the ClientLibrary which has to be build as an program.cs because of another issue with FormFile.  It might not be an issue anymore
/// since we are not using Formfile directly in the ClientLibrary.  But for now this simple program will allow us to build the
/// bundle.exe app.
/// </summary>
internal class Program
{
    private static ILogger _logger;


    static void Main(string[] args)
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
                                              .CreateLogger();
        _logger = Log.Logger;
        Log.Information("Starting {AppName}", Assembly.GetExecutingAssembly().GetName().Name);

        // Get Sensitive Appsettings.json file location
        string? sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");

        // 1.A  We use the normal Appsettings file when this is deployed.
        string  versionPath = Directory.GetCurrentDirectory();
        string? appRoot     = versionPath;

        // 1.B.  We keep the AppSettings file in the root App folder on the servers so it never gets overwritten
        DirectoryInfo? appRootDirectoryInfo = Directory.GetParent(versionPath);
        appRoot = appRootDirectoryInfo.FullName;
        Console.WriteLine("Running from Directory:  " + appRoot);

        // Load Environment Specific App Setting file
        string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Log.Information("Environment = " + environmentName);
        string appSettingFileName = "appsettings." + environmentName + ".json";
        string appSettingEnvFile  = Path.Join(appRoot, appSettingFileName);
        DisplayAppSettingStatus(appSettingEnvFile);


        // 1.A  The Bundler needs to access the file from the same directory as the exe's.
        appSettingFileName = "appsettings." + environmentName + ".json";
        string appSettingNormalEnvFile = Path.Join(versionPath, appSettingFileName);
        DisplayAppSettingStatus(appSettingNormalEnvFile);


        // Load the Sensitive AppSettings.JSON file.
        string sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);
        DisplayAppSettingStatus(sensitiveSettingFile);


        // Add our custom AppSettings.JSON files
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile(appSettingEnvFile, true, true)
                                                                     .AddJsonFile(appSettingNormalEnvFile, true, true)
                                                                     .AddJsonFile(sensitiveSettingFile, true, true).Build();


        using IHost host = Host.CreateDefaultBuilder(args)

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
                                                 )
                               .Build();


        // Run the Main App. Do NOT AWAIT call
#pragma warning disable CS4014
        host.RunAsync();
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