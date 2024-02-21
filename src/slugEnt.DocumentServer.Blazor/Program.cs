using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using SlugEnt.DocumentServer.Db;
using Radzen;
using SlugEnt.DocumentServer.Blazor.Components;
using Microsoft.AspNetCore.Identity;


namespace SlugEnt.DocumentServer.Blazor;

public class Program
{
    private static Serilog.ILogger _logger;


    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 10 - Logging Setup
        //_logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
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
        string appSettingFileName = $"appsettings." + environment.EnvironmentName + ".json";
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
                   .LogTo(Console.WriteLine, LogLevel.Debug)
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


        // WSH End 
        // Add services to the container.
        builder.Services.AddRazorComponents()
               .AddInteractiveServerComponents();

        var app = builder.Build();

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



    /// <summary>
    /// Logs whether a given AppSettings file was found to exist.
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