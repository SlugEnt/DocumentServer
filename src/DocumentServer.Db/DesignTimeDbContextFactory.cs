using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SlugEnt.DocumentServer.Db
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocServerDbContext>
    {
        public DocServerDbContext CreateDbContext(string[] args)
        {
            string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");
            string sensitiveFileName    = "SlugEnt.DocumentServer.DocumentServer.API_AppSettingsSensitive.json";
            string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);


            IConfigurationRoot configuration    = new ConfigurationBuilder().AddJsonFile(sensitiveSettingFile).Build();
            var                builder          = new DbContextOptionsBuilder<DocServerDbContext>();
            var                connectionString = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
            builder.UseSqlServer(connectionString);
            return new DocServerDbContext(builder.Options);
        }
    }
}