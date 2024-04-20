// See https://aka.ms/new-console-template for more information
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;

Console.WriteLine("Hello, World!");

DocServerDbContext db = new DocServerDbContext();

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
Console.WriteLine("step 1 done");

// Load the Sensitive AppSettings.JSON file.
//string sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
string sensitiveFileName    = "SlugEnt.DocumentServer_AppSettingsSensitive.json";
string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);
DisplayAppSettingStatus(sensitiveSettingFile);


// Add our custom AppSettings.JSON files
IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile(appSettingEnvFile, true, true)
                                                             .AddJsonFile(sensitiveSettingFile, true, true).Build();


using IHost host = Host.CreateDefaultBuilder(args)

                       // Add our custom config from above to the default configuration
                       .ConfigureAppConfiguration(config => { config.AddConfiguration(configuration); })
                       .ConfigureServices((_,
                                           services) =>

                                              // The main program     
                                              services
                                                  .AddDbContext<DocServerDbContext>(options =>
                                                  {
                                                      options
                                                          .UseSqlServer(configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()));
                                                  })).Build();
host.Run();
DocServerDbContext db2 = host.Services.GetService<DocServerDbContext>();
Application        app = await db2.Applications.SingleOrDefaultAsync(a => a.Id == 1);
int                j   = 0;
j++;


/// <summary>
///     Logs whether a given AppSettings file was found to exist.
/// </summary>
/// <param name="appSettingFileName"></param>
void DisplayAppSettingStatus(string appSettingFileName)
{
    if (File.Exists(appSettingFileName))
        Console.WriteLine("AppSettings File was located.  {0}", appSettingFileName);
    else
        Console.WriteLine("AppSettings File was not found.  {0}", appSettingFileName);
}