using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;
using DocumentServer.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsoleTesting;

public class Program
{
    private static async Task Main(string[] args)
    {
        Serilog.ILogger Logger;
        Log.Logger = new LoggerConfiguration()
#if DEBUG
                     .MinimumLevel.Debug()
                     .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
#else
						 .MinimumLevel.Information()
			             .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
#endif
                     .Enrich.FromLogContext()
                     .WriteTo.Console()

                     //.WriteTo.Debug()
                     .CreateLogger();

        Log.Debug("Starting " + Assembly.GetEntryAssembly().FullName);


        var      host     = CreateHostBuilder(args).Build();
        MainMenu mainMenu = host.Services.GetService<MainMenu>();
        await host.StartAsync();

        await mainMenu.Start();

        return;
    }



    /// <summary>
    /// Creates the Host
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) => { config.AddJsonFile("AppSettings.json"); })
            .ConfigureServices((_, services) =>
            {
                services.AddTransient<MainMenu>();

                //services.AddTransient<DocumentServerEngine>();

                // We add the SocketsHttpHandler just to ensure if this is used in a Singleton instance that we do not run into issues.
                services.AddHttpClient<AccessDocumentServerHttpClient>().ConfigurePrimaryHttpMessageHandler(() =>
                        {
                            return new SocketsHttpHandler()
                            {
                                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                            };
                        })
                        .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
            })
            .ConfigureLogging((_, logging) =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
                logging.AddDebug();
                logging.AddConsole();

                //logging.AddSimpleConsole(options => options.IncludeScopes = true);
                //logging.AddEventLog();
            });
}