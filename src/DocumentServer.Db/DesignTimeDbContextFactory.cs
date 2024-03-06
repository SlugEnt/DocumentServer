using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SlugEnt.DocumentServer.Db;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocServerDbContext>
{
    public DocServerDbContext CreateDbContext(string[] args)
    {
        string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");
        string sensitiveFileName    = "SlugEnt.DocumentServer.API_AppSettingsSensitive.json";
        string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);


        IConfigurationRoot                          configuration    = new ConfigurationBuilder().AddJsonFile(sensitiveSettingFile).Build();
        DbContextOptionsBuilder<DocServerDbContext> builder          = new DbContextOptionsBuilder<DocServerDbContext>();
        string?                                     connectionString = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
        builder.UseSqlServer(connectionString);
        return new DocServerDbContext(builder.Options);
    }
}