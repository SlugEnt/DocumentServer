using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ConsoleTesting;

public class MainMenu
{
    private readonly ILogger                        _logger;
    private          IServiceProvider               _serviceProvider;
    private          bool                           _started;
    private          IHttpClientFactory             _httpClientFactory;
    private readonly JsonSerializerOptions          _options;
    private readonly AccessDocumentServerHttpClient _documentServerHttpClient;


    public MainMenu(ILogger<MainMenu> logger, IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory,
                    AccessDocumentServerHttpClient documentServerHttpClient)
    {
        _logger                   = logger;
        _serviceProvider          = serviceProvider;
        _httpClientFactory        = httpClientFactory;
        _documentServerHttpClient = documentServerHttpClient;


        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _logger.LogError($"Main Menu Configured");
    }



    internal async Task Start()
    {
        bool keepProcessing = true;

        Console.WriteLine("Press S");

        while (keepProcessing)
        {
            if (Console.KeyAvailable)
            {
                keepProcessing = await MainMenuUserInput();
            }
            else
                Thread.Sleep(1000);


            Display();
        }
    }



    internal async Task Display() { }


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
                        FileInfo fileToSave = new FileInfo(Path.Combine(@"T:\RetarusFaxReport.pdf"));
                        bool result = await _documentServerHttpClient.SaveDocumentAsync(fileToSave);
                        if (result) Console.WriteLine("Document Stored");
                        else Console.WriteLine("Document failed to be stored due to errors");
                        break;


                    case ConsoleKey.I:
                        Console.WriteLine("Enter the number of minutes between flight creations");
                        string interval = Console.ReadLine();
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

        return true;
    }
}