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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ILogger = Serilog.ILogger;

namespace SlugEnt.DocumentServer.Api;

public class Program
{
    private static ILogger _logger;



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
                            string nodekey = "")
    {
        Console.WriteLine("API Starting Up");

        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // 10 - Logging Setup
        ILogger logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
        _logger = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.Api");
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(_logger);
        builder.Host.UseSerilog(_logger);

        LoadAppSettings(builder);


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
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()))
        });

#endif

        //*********************************************************************

        // Add Node2Node Http Client
        builder.Services.AddHttpClient<NodeToNodeHttpClient>().ConfigurePrimaryHttpMessageHandler(() =>
               {
                   return new SocketsHttpHandler
                   {
                       PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                   };
               })
               .SetHandlerLifetime(Timeout.InfiniteTimeSpan);


        // 30 - Add Services to the container.
        builder.Services.AddTransient<DocumentServerEngine>();


        // 35 - Setup APIInfo and Health Checks
        APIInfoBase apiInfoBase = SetupAPIInfoBase();
        builder.Services.Configure<DocumentServerFromAppSettings>(builder.Configuration.GetSection("DocumentServer"));

        builder.Services.AddSingleton<IAPIInfoBase>(apiInfoBase);
        builder.Services.AddSingleton<HealthCheckProcessor>();
        builder.Services.AddHostedService<HealthCheckerBackgroundProcessor>();


        // Build DocumentServerInformation Object
        ILogger                          dsiLogger  = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.DocumentServerInformation");
        DocumentServerInformationBuilder dsiBuilder = new(dsiLogger);
        dsiBuilder.UseConfiguration(builder.Configuration);
        if (overrideHostname != string.Empty)
            dsiBuilder.TestOverrideServerDNSName(overrideHostname);
        if (db != string.Empty)
            dsiBuilder.TestUseDatabase(db);
        DocumentServerInformation dsi = dsiBuilder.Build();
        builder.Services.AddSingleton<DocumentServerInformation>(dsi);

        /*
        builder.Services.AddSingleton<DocumentServerInformation>(dsi =>
        {
            IConfiguration x         = dsi.GetService<IConfiguration>();
            ILogger        dsiLogger = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.DocumentServerInformation");
            return DocumentServerInformation.Create(x,
                                                    null,
                                                    dsiLogger,
                                                    overrideHostname,
                                                    db);
        });
        */
        builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
        builder.Services.AddTransient<INodeKeyValidation, NodeKeyValidation>();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();


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
        if (port > 0)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 1024 * 1024 * 100;
                options.Listen(address, port);
            });
        }
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


        // Wait for The DocumentServerInformation object to finish initializing
        dsiBuilder.AwaitInitialization();
        /*DocumentServerInformation dsi = app.Services.GetService<DocumentServerInformation>();
        if (dsi.IsInitialized == false)
        {
            for (int i = 0; i < 10; i++)
            {
                if (dsi.Initialize.IsFaulted)
                    throw new ApplicationException("Error 9863:  Error in DocumentServerInformation object.  Cannot run without this object!");

                if (dsi.IsInitialized == true)
                    break;

                Thread.Sleep(500);
            }

            if (!dsi.IsInitialized)
                throw new ApplicationException("Error 9844: Failed to succesfully initialize DocumentServerInformation object.  Cannot run without this object initialized.");
        }
        */

        // We start it because we need to acquire the port it is listening on, we do not have it until after the app starts!
        app.Start();
        Console.WriteLine("API Successfully Started");
        _logger.Warning("API Completed Setup and Preparing to Run");

        GetListeningPorts(dsi, app.Urls);

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

            int i = appUrl.IndexOf(":", 6);
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


    private static void LoadAppSettings(WebApplicationBuilder builder)
    {
        string        versionPath          = Directory.GetCurrentDirectory();
        DirectoryInfo appRootDirectoryInfo = Directory.GetParent(versionPath);
        string        appRoot              = appRootDirectoryInfo.FullName;

        List<string> appSettingsDirectories = new()
        {
            versionPath,
            appRoot,
        };

        string? environmentName    = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string  appSettingFileName = "appsettings." + environmentName + ".json";
        string  appSettingsBase    = "appsettings.json";
        List<string> appSettingsFiles = new()
        {
            appSettingsBase,
            appSettingFileName,
        };


        Console.WriteLine("Running from Directory:  " + appRoot);


        // Now load the appsettings
        foreach (string filename in appSettingsFiles)
        {
            foreach (string directory in appSettingsDirectories)
            {
                string appSettingFile = Path.Join(directory, filename);
                builder.Configuration.AddJsonFile(appSettingFile, true);
                DisplayAppSettingStatus(appSettingFile);
            }
        }

        // Finally Check the AppSettingSensitive folder
        // Get Sensitive Appsettings.json file location
        string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");
        string sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        string path                 = Path.Join(sensitiveAppSettings, sensitiveFileName);

        builder.Configuration.AddJsonFile(path, true);
        DisplayAppSettingStatus(path);
    }
}