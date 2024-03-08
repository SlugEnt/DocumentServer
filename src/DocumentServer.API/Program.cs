#define SWAGGER

using System.Configuration;
using System.Drawing;
using System.Reflection;
using Azure.Core.Extensions;
using DocumentServer.Core;
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
using ILogger = Serilog.ILogger;

namespace DocumentServer;

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
        _logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
        builder.Logging.ClearProviders();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(_logger);
        builder.Host.UseSerilog(_logger);


        // 20 - AppSettings File loading
        IWebHostEnvironment environment          = builder.Environment;
        string              versionPath          = Directory.GetCurrentDirectory();
        DirectoryInfo       appRootDirectoryInfo = Directory.GetParent(versionPath);
        string              appRoot              = appRootDirectoryInfo.FullName;
        Console.WriteLine("Running from Directory:  " + appRoot);

        // Get Sensitive Appsettings.json file location
        string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");


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


        // 30 - Add Services to the container.
        builder.Services.AddTransient<DocumentServerEngine>();


        // 35 - Setup APIInfo and Health Checks
        APIInfoBase apiInfoBase = SetupAPIInfoBase();
        builder.Services.Configure<DocumentServerFromAppSettings>(builder.Configuration.GetSection("DocumentServer"));

        builder.Services.AddSingleton<IAPIInfoBase>(apiInfoBase);
        builder.Services.AddSingleton<HealthCheckProcessor>();
        builder.Services.AddHostedService<HealthCheckerBackgroundProcessor>();
        builder.Services.AddSingleton<DocumentServerInformation>(); // Must be just one version throughout program lifetime
        builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();


        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
#if SWAGGER
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
#endif


        // E.  Add Database access
        builder.Services.AddDbContext<DocServerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()))

                   // IF Debug then log all SQL to Console
#if (DEBUG || SWAGGER)
                   .LogTo(Console.WriteLine)
                   .EnableDetailedErrors();

#endif
        });

        builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; }).AddJwtBearer();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKeyPolicy",
                              policy =>
                              {
                                  policy.AddAuthenticationSchemes(new[]
                                  {
                                      JwtBearerDefaults.AuthenticationScheme
                                  });
                                  policy.Requirements.Add(new ApiKeyRequirement());
                              });
        });
        builder.Services.AddScoped<IAuthorizationHandler, ApiKeyHandler>();


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
}