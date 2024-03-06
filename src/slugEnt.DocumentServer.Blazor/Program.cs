using System.Reflection;
using DocumentServer.Core;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;
using SlugEnt.DocumentServer.Blazor.Components;
using SlugEnt.DocumentServer.Db;
using ILogger = Serilog.ILogger;

namespace SlugEnt.DocumentServer.Blazor;

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
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 10 - Logging Setup
        //_logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.FromAppSettings(builder.FromAppSettings).Enrich.FromLogContext().CreateLogger();
        _logger = new LoggerConfiguration().WriteTo.Debug().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(_logger);
        builder.Host.UseSerilog(_logger);

        // WSH Code
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


        // E.  Add Database access
#if (DEBUG || SWAGGER)
        builder.Services.AddDbContext<DocServerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()))
                   .LogTo(Console.WriteLine)
                   .EnableDetailedErrors();
        });
#else
        builder.Services.AddDbContext<DocServerDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()));
        });
#endif


        builder.Services.AddQuickGridEntityFrameworkAdapter();

        builder.Services.AddRadzenComponents();
        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<DocumentServerEngine>();


        // WSH End 
        // Add services to the container.
        builder.Services.AddRazorComponents()
               .AddInteractiveServerComponents();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");

            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        app.Run();
    }
}