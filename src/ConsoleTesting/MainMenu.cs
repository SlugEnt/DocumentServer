using System.Text.Json;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;

namespace ConsoleTesting;

public partial class MainMenu
{
    private readonly DocServerDbContext             _db;
    private readonly AccessDocumentServerHttpClient _documentServerHttpClient;
    private readonly ILogger                        _logger;
    private readonly JsonSerializerOptions          _options;
    private          IHttpClientFactory             _httpClientFactory;
    private          IServiceProvider               _serviceProvider;
    private          bool                           _started;


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

        _logger.LogError("Main Menu Configured");
    }



    internal async Task Display()
    {
        Console.WriteLine("Press:  ");
        Console.WriteLine(" ( T ) Testing");
        Console.WriteLine(" ( U ) To Upload a randonly generated file");
        Console.WriteLine(" ( R ) To Retrieve a file");
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
                        for (int i = 0; i < 100; i++)
                        {
                            await _documentServerHttpClient.DoDownload(1);
                        }

                        break;

                    case ConsoleKey.U:
                        System.IO.FileInfo fileToSave = new(Path.Combine(@"T:\RetarusFaxReport.pdf"));
                        TransferDocumentDto transferDocumentDto = new()
                        {
                            DocumentTypeId = 3,
                            Description    = "Some document about something",
                            FileExtension  = fileToSave.Extension,
                            RootObjectId   = "1",
                        };

                        // TODO Fix this - it is not reading any file in.
                        //transferDocumentDto.ReadFileIn(fileToSave.FullName);
                        Result<long> result = await _documentServerHttpClient.SaveDocumentAsync(transferDocumentDto, fileToSave.FullName);

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

                    case ConsoleKey.T:

                        IEnumerable<RootObject>  rootObjects  = _db.RootObjects;
                        IEnumerable<Application> applications = _db.Applications;

                        break;

                    case ConsoleKey.R:
                        await _documentServerHttpClient.DoDownload(1);
                        break;
                    case ConsoleKey.G:
                        await _documentServerHttpClient.GetDocumentAndInfo(1);
                        break;

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
            {
                Thread.Sleep(1000);
            }
        }
    }
}