using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SlugEnt.DocumentServer.Db;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocServerDbContext>
{
    public DocServerDbContext CreateDbContext(string[] args)
    {
        // *** START - This was copied from BundleBuilderSimple

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

        string appSettingFileName = "appsettings." + environmentName + ".json";
        string appSettingEnvFile  = Path.Join(appRoot, appSettingFileName);


        // 1.A  The Bundler needs to access the file from the same directory as the exe's.
        appSettingFileName = "appsettings." + environmentName + ".json";
        string appSettingNormalEnvFile = Path.Join(versionPath, appSettingFileName);


        // Load the Sensitive AppSettings.JSON file.
        //string sensitiveFileName    = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
        string sensitiveFileName    = "SlugEnt.DocumentServer_AppSettingsSensitive.json";
        string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);


        // Add our custom AppSettings.JSON files
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile(appSettingEnvFile, true, true)
                                                                     .AddJsonFile(appSettingNormalEnvFile, true, true)
                                                                     .AddJsonFile(sensitiveSettingFile, true, true).Build();

        // *** END - Copy From BundleBuilderSimple
        Console.WriteLine("DocServerDbContext Design Time Tools:  AppSettingEnv File:       " + appSettingEnvFile);
        Console.WriteLine("DocServerDbContext Design Time Tools:  AppSettingNormalEnv File: " + appSettingNormalEnvFile);
        Console.WriteLine("DocServerDbContext Design Time Tools:  Sensitive File:           " + sensitiveSettingFile);

        DbContextOptionsBuilder<DocServerDbContext> builder          = new DbContextOptionsBuilder<DocServerDbContext>();
        string?                                     connectionString = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
        Console.WriteLine("Connection String Found was: " + connectionString);

        builder.UseSqlServer(connectionString);
        return new DocServerDbContext(builder.Options);
    }
}