#define SWAGGER

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


    public static void Main(string[] args)
    {
        Console.WriteLine("API Starting Up");
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 10 - Logging Setup
        ILogger logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
        _logger = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.Api");


        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(_logger);
        builder.Host.UseSerilog(_logger);

        LoadAppSettings(builder);
        /*
        // 20 - AppSettings File loading
        string        versionPath          = Directory.GetCurrentDirectory();
        DirectoryInfo appRootDirectoryInfo = Directory.GetParent(versionPath);
        string        appRoot              = appRootDirectoryInfo.FullName;

        List<string> AppSettingsDirectories = new()
        {
            {
                versionPath
            },
            {
                appRoot
            }
        };

        // Get Sensitive Appsettings.json file location
        string              sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");
        string              sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        IWebHostEnvironment environment          = builder.Environment;
        string              appSettingFileName   = "appsettings." + environment.EnvironmentName + ".json";

        List<string> AppSettingsFiles = new()
        {
            {
                sensitiveFileName
            },
            {
                appSettingFileName
            },

        };



        Console.WriteLine("Running from Directory:  " + appRoot);

        */
        /*
        // Load Environment Specific App Setting file
        string appSettingFileName = "appsettings." + environment.EnvironmentName + ".json";
        string appSettingFile     = Path.Join(appRoot, appSettingFileName);
        builder.Configuration.AddJsonFile(appSettingFile, true);
        DisplayAppSettingStatus(appSettingFile);

        // Load the Sensitive AppSettings.JSON file.
        string sensitiveFileName = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        appSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);
        builder.Configuration.AddJsonFile(appSettingFile, true);
        DisplayAppSettingStatus(appSettingFile);
        */

        // 30 - Add Services to the container.
        builder.Services.AddTransient<DocumentServerEngine>();


        // 35 - Setup APIInfo and Health Checks
        APIInfoBase apiInfoBase = SetupAPIInfoBase();
        builder.Services.Configure<DocumentServerFromAppSettings>(builder.Configuration.GetSection("DocumentServer"));

        builder.Services.AddSingleton<IAPIInfoBase>(apiInfoBase);
        builder.Services.AddSingleton<HealthCheckProcessor>();
        builder.Services.AddHostedService<HealthCheckerBackgroundProcessor>();

        builder.Services.AddSingleton<DocumentServerInformation>(dsi =>
        {
            IConfiguration x         = dsi.GetService<IConfiguration>();
            ILogger        dsiLogger = logger.ForContext("SourceContext", "SlugEnt.DocumentServer.DocumentServerInformation");
            return DocumentServerInformation.Create(x, null, dsiLogger);
        });

        builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
        builder.Services.AddTransient<INodeKeyValidation, NodeKeyValidation>();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();


        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
#if SWAGGER
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
#endif

        // E.  Add Database Access
        builder.Services.AddDbContextPool<DocServerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()))

                   // IF Debug then log all SQL to Console
#if (DEBUG || SWAGGER)
                   .EnableDetailedErrors();

#endif
        });


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


        builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; });

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


        // Configure The Document Server Engine for first time.
        DocumentServerInformation dsi = app.Services.GetService<DocumentServerInformation>();


        app.Run();
        Log.CloseAndFlush();
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