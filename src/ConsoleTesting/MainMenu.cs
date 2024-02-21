using System.Text.Json;
using DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlugEnt.FluentResults;

namespace ConsoleTesting;

public partial class MainMenu
{
    private readonly ILogger                        _logger;
    private          IServiceProvider               _serviceProvider;
    private          bool                           _started;
    private          IHttpClientFactory             _httpClientFactory;
    private readonly JsonSerializerOptions          _options;
    private readonly AccessDocumentServerHttpClient _documentServerHttpClient;
    private readonly DocServerDbContext             _db;


    public MainMenu(ILogger<MainMenu> logger,
                    IServiceProvider serviceProvider,
                    IHttpClientFactory httpClientFactory,
                    AccessDocumentServerHttpClient documentServerHttpClient,
                    DocServerDbContext db)
    {
        _logger                   = logger;
        _serviceProvider          = serviceProvider;
        _httpClientFactory        = httpClientFactory;
        _documentServerHttpClient = documentServerHttpClient;
        _db                       = db;


        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _logger.LogError($"Main Menu Configured");
    }



    internal async Task Start()
    {
        bool keepProcessing = true;
        Display();

        while (keepProcessing)
        {
            if (Console.KeyAvailable)
            {
                Display();
                keepProcessing = await MainMenuUserInput();
            }
            else
                Thread.Sleep(1000);
        }
    }



    internal async Task Display()
    {
        Console.WriteLine("Press:  ");
        Console.WriteLine(" ( U ) To Upload a randonly generated file");
        Console.WriteLine(" ( Z ) To Seed the database");
    }


    internal async Task<bool> MainMenuUserInput()
    {
        try
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                switch (keyInfo.Key)
                {
                    case ConsoleKey.S:
                        for (int i = 0; i < 1000; i++)
                        {
                            string? answer = await _documentServerHttpClient.GetDocument();
                        }

                        break;

                    case ConsoleKey.U:
                        FileInfo       fileToSave = new FileInfo(Path.Combine(@"T:\RetarusFaxReport.pdf"));
                        Result<string> result     = await _documentServerHttpClient.SaveDocumentAsync(fileToSave);
                        if (result.IsSuccess)
                            Console.WriteLine("Document Stored   |   ID = " + result.Value);
                        else
                            Console.WriteLine("Document failed to be stored due to errors:  ");
                        foreach (IError resultError in result.Errors)
                            Console.WriteLine("Error: " + resultError);

                        break;


                    case ConsoleKey.Z:
                        Console.WriteLine("Seeding the Database...");
                        await SeedDataAsync();
                        break;

                    case ConsoleKey.D: break;

                    case ConsoleKey.R: break;

                    case ConsoleKey.X: return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error:  {Error}", ex);
        }

        Display();
        return true;
    }
}