#define SWAGGER

using DocumentServer.Core;
using DocumentServer.Db;
using Microsoft.EntityFrameworkCore;
using Serilog;



namespace DocumentServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            var logger = new LoggerConfiguration().WriteTo.Console().ReadFrom.Configuration(builder.Configuration).Enrich.FromLogContext().CreateLogger();
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);
            builder.Host.UseSerilog(logger);


            // Add services to the container.
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
    }
}