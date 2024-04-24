#define SWAGGER

using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SlugEnt.APIInfo;
using SlugEnt.APIInfo.HealthInfo;
using SlugEnt.DocumentServer.API.Security;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using SlugEnt.ResourceHealthChecker;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog.Sinks.File;
using ILogger = Serilog.ILogger;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace SlugEnt.DocumentServer.Api;

public class Program
{
    private static ILogger _logger;

    //private static ILogger _logContext;


    /// <summary>
    ///     Logs whether a given AppSettings file was found to exist.
    /// </summary>
    /// <param name="appSettingFileName"></param>
    private static void DisplayAppSettingStatus(string appSettingFileName)
    {
        if (File.Exists(appSettingFileName))
            _logger.Warning("AppSettings File was located.  {AppSettingsFile}", appSettingFileName);
        else
            _logger.Warning("AppSettings File was not found.  {AppSettingsFile}", appSettingFileName);
    }


    /// <summary>
    /// API for DocumentServer
    /// </summary>
    /// <param name="overrideHostname">Should never be used in production.  Only value is in Unit Testing.  It overrides the Hostname DNS lookup </param>
    /// <param name="listenPort">The port to listen on.  This is for unit testing only.  Never to be used in production</param>
    /// <param name="db">A complete database connection string.  Never to be used in production!</param>
    /// <param name="nodekey">The Node to Node key that is required for 2 nodes to talk to each other.  This is normally set thru
    /// configuration, but this will override it</param>
    public static void Main(string overrideHostname = "",
                            int port = 0,
                            string db = "",
                            string nodekey = "",
                            bool logtofile = false)
    {
        Console.WriteLine("API Starting Up");

        // *******  This first section is very important as to the order of things.  Be very careful and test fully before moving anything around!
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        List<string> AppSettingFiles = new List<string>();
        LoadAppSettings(builder, AppSettingFiles);

        // Logging Setup this is initial for logging during the build process! This is later replaced by the UseSerilog section later once the builder is built!
        LoggerConfiguration logconfig = new LoggerConfiguration()
                                        .ReadFrom.Configuration(builder.Configuration);


        // This provides the context for this initial logger.  If not provided then there is no context!
        Serilog.ILogger logger = logconfig.CreateLogger();
        _logger = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.Api");


        if (logtofile)
        {
            logconfig.WriteTo.Logger(lc => lc.WriteTo.File(@"t:\temp\dsapi.log"));
        }


        foreach (string appSettingFile in AppSettingFiles)
        {
            DisplayAppSettingStatus(appSettingFile);
        }


        // This is the Serilog that will be used after build!
        builder.Host.UseSerilog((hostContext,
                                 AsyncServiceScope,
                                 configuration) =>
        {
            configuration.ReadFrom.Configuration(hostContext.Configuration);
        });

        // ********   End of First Section!


        _logger.Verbose("Verbose Message");
        _logger.Debug("Debug Message");
        _logger.Information("info message");
        _logger.Warning("Warning Message");
        _logger.Error("Error Message");
        _logger.Fatal("Fatal error message");


        // 15 - Display Passed Command Line Arguments
        if (overrideHostname != String.Empty)
            _logger.Warning("Argument Found: overrideHostname: " + overrideHostname);
        if (db != String.Empty)
            _logger.Warning("Argument Found: db: " + db);
        if (port != 0)
            _logger.Warning("Argument Found: port: " + port);


        //*********************************************************************
        // E.  Add Database Access
        //  - 3 Connection possibilities.  
        //      A)  DEBUG and db override specified.  Use passed Connection string
        //      B)  DEBUG and no db override.  Read from Configuration and enable detailed errors
        //      C)  NO DEBUG.  Production.  Read from Configuration.

#if DEBUG
        if (db != string.Empty)
        {
            _logger.Warning("Database Connection:  Using passed in value of " + db);
            builder.Services.AddDbContextPool<DocServerDbContext>(options =>
            {
                options.UseSqlServer(db)

                       // IF Debug then log all SQL to Console
                       .EnableDetailedErrors()
                       .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            });
        }
        else
        {
            string connStr = builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
            _logger.Warning("Database Connection:  Using value found in Configuration: " + connStr);
            builder.Services.AddDbContextPool<DocServerDbContext>(options =>
            {
                options.UseSqlServer(connStr)
                       .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))

                       // IF Debug then log all SQL to Console
                       .EnableDetailedErrors();
            });
        }
#else
        builder.Services.AddDbContextPool<DocServerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()));
        });

#endif

        //*********************************************************************


        // 30 - Add Services to the container.
        builder.Services.AddTransient<DocumentServerEngine>();


        // 35 - Setup APIInfo and Health Checks
        APIInfoBase apiInfoBase = SetupAPIInfoBase();
        builder.Services.Configure<DocumentServerFromAppSettings>(builder.Configuration.GetSection("DocumentServer"));

        builder.Services.AddSingleton<IAPIInfoBase>(apiInfoBase);
        builder.Services.AddSingleton<HealthCheckProcessor>();
        builder.Services.AddHostedService<HealthCheckerBackgroundProcessor>();


        // Build DocumentServerInformation Object
        ILogger                          dsiLogger  = _logger.ForContext("SourceContext", "SlugEnt.DocumentServer.DocumentServerInformation");
        DocumentServerInformationBuilder dsiBuilder = new(dsiLogger);
        dsiBuilder.UseConfiguration(builder.Configuration);
        if (overrideHostname != string.Empty)
            dsiBuilder.TestOverrideServerDNSName(overrideHostname);
        if (db != string.Empty)
            dsiBuilder.TestUseDatabase(db);
        if (nodekey != string.Empty)
            dsiBuilder.UseNodeKey(nodekey);
        DocumentServerInformation dsi = dsiBuilder.Build();
        builder.Services.AddSingleton<DocumentServerInformation>(dsi);


        // Add Node2Node Http Client
        builder.Services.AddHttpClient<NodeToNodeHttpClient>().ConfigurePrimaryHttpMessageHandler(() =>
               {
                   return new SocketsHttpHandler
                   {
                       PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                   };
               })
               .SetHandlerLifetime(Timeout.InfiniteTimeSpan);


        builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
        builder.Services.AddTransient<INodeKeyValidation, NodeKeyValidation>();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpLogging(options => { });


        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
#if SWAGGER
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
#endif


        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; }).AddJwtBearer();
        builder.Services.AddAuthorization(options =>
        {
            // This defines a policy for clients to talk to us
            options.AddPolicy("ApiKeyPolicy",
                              policy =>
                              {
                                  policy.AddAuthenticationSchemes(new[]
                                  {
                                      JwtBearerDefaults.AuthenticationScheme
                                  });
                                  policy.Requirements.Add(new ApiKeyRequirement());
                              });

            // This defines policy for nodes that need to talk to each other
            options.AddPolicy("NodeKeyPolicy",
                              policy =>
                              {
                                  policy.AddAuthenticationSchemes(new[]
                                  {
                                      JwtBearerDefaults.AuthenticationScheme
                                  });
                                  policy.Requirements.Add(new NodeKeyRequirement());
                              });
        });
        builder.Services.AddSingleton<IAuthorizationHandler, ApiKeyHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, NodeKeyHandler>();


#if DEBUG
        IPAddress address = IPAddress.Parse("127.0.0.1");
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1024 * 1024 * 100;
            if (port > 0)
                options.Listen(address, port);

            //options.ConfigureEndpointDefaults(listenOptions => { listenOptions.UseConnectionLogging(); });
        });
#else
        builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; });
#endif
        WebApplication app = builder.Build();

        app.UseRouting();

        // Configure the HTTP request pipeline.
        //TODO Uncomment
#if SWAGGER

        //if (app.Environment.IsDevelopment())
        // {
        app.UseSwagger();
        app.UseSwaggerUI();

        // }
#endif
        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();


        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();
        app.UseHttpLogging();
        app.UseHttpsRedirection();

        app.UseAuthorization();

        // 4.C Endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSlugEntPing();
            endpoints.MapSlugEntSimpleInfo();
            endpoints.MapSlugEntConfig();
            endpoints.MapSlugEntHealth();
        });


        // Build a NodeKeyValidation object so we can set its Static NodeKey.
        INodeKeyValidation? nodeKeyValidation = app.Services.GetService<INodeKeyValidation>();

        // Wait for The DocumentServerInformation object to finish initializing
        dsiBuilder.AwaitInitialization();

        // We start it because we need to acquire the port it is listening on, we do not have it until after the app starts!
        app.Start();
        Console.WriteLine("API Successfully Started");
        _logger.Warning("API Completed Setup and Preparing to Run");

        GetListeningPorts(dsi, app.Urls);

        // Test Code - Delete 

        // End test code
        app.WaitForShutdown();

        Log.CloseAndFlush();
    }


    /// <summary>
    /// Determines what ports the API is listening on.
    /// </summary>
    /// <param name="dsi"></param>
    /// <param name="urls"></param>
    private static void GetListeningPorts(DocumentServerInformation dsi,
                                          ICollection<string> urls)
    {
        // There really should only be one listening port for unit testing.
        foreach (string appUrl in urls)
        {
            _logger.Information("Listening on: " + appUrl);

            int i = appUrl.LastIndexOf(":");
            if (i > 0)
            {
                string portStr = appUrl.Substring(i + 1);
                int    port    = int.Parse(portStr);
                bool?  http    = null;

                // Determine if http or https
                if (appUrl.StartsWith("https"))
                    http = false;
                else if (appUrl.StartsWith("http"))
                    http = true;

                dsi.RemoteNodePort = port;
            }
        }
    }


    /// <summary>
    /// Sets up the API Info Base object
    /// </summary>
    /// <returns></returns>
    private static APIInfoBase SetupAPIInfoBase()
    {
        // This is how you override the api endpoint to something other than info
        APIInfoBase apiInfoBase = new();
        apiInfoBase.AddConfigHideCriteria("password");
        apiInfoBase.AddConfigHideCriteria("os");
        return apiInfoBase;
    }


    //private static void DisplayAppSettings()


    /// <summary>
    ///   Adds the parent directory appsettings.[Env].json file and a sensitive appsettings.json file
    /// </summary>
    /// <param name="builder"></param>
    /// <remarks>Because this is from a WebApplicationBuilder there is no way to influence the initial set of appsettings files.
    /// The initial order provided by WebApplicationBuilder is Appsettings.json then AppSettings.[Env].Json which overrides appsettings.json
    /// These come from the base directory.
    /// see:  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0
    /// </remarks>
    private static void LoadAppSettings(WebApplicationBuilder builder,
                                        List<string> appSettingsFiles)
    {
        string        versionPath = Directory.GetCurrentDirectory();
        DirectoryInfo parentPath  = Directory.GetParent(versionPath);
        string        appRoot     = parentPath.FullName;


        string? environmentName   = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string  appSettingEnvFile = "appsettings." + environmentName + ".json";

        Console.WriteLine("Running from Directory:  " + appRoot);


        // The 2 initial appsettings provided by WebAppBuilder we just need to display if they exist.
        string settingFile = "appsettings.json";
        string path        = Path.Join(appRoot, settingFile);
        appSettingsFiles.Add(path);

        settingFile = "appsettings." + environmentName + ".json";
        path        = Path.Join(appRoot, settingFile);
        appSettingsFiles.Add(path);

        // Now load the environment specific one from the parent directory
        settingFile = "appsettings." + environmentName + ".json";
        path        = Path.Join(appRoot, settingFile);
        builder.Configuration.AddJsonFile(path, true);
        appSettingsFiles.Add(path);


        // Finally load the Sensitive file one, looking in runtime folder
        string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");
        string sensitiveFileName    = "SlugEnt.DocumentServer_AppSettingsSensitive.json";
        path = Path.Join(sensitiveAppSettings, sensitiveFileName);

        builder.Configuration.AddJsonFile(path, true);
        appSettingsFiles.Add(path);
    }
}