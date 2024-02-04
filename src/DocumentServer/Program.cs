#define SWAGGER

using System.Reflection;
using DocumentServer.Core;
using DocumentServer.Db;
using Microsoft.EntityFrameworkCore;
using Serilog;



namespace DocumentServer
{
    public class Program
    {
        private static Serilog.ILogger _logger;


        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 10 - Logging Setup
            _logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
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
            string appSettingFileName = $"appsettings." + environment.EnvironmentName + ".json";
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
            ;


            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();

            //           builder.Services.AddDatabaseDeveloperPageExceptionFilter();


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
                       .LogTo(Console.WriteLine, LogLevel.Debug)
                       .EnableDetailedErrors();

#endif
                ;
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //TODO Uncomment
#if SWAGGER
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
#endif
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();


            app.UseExceptionHandler();
            app.UseStatusCodePages();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

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
}