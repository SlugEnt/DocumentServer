﻿using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
    private          long                           lastDocSaved = 0;
    private          IConfiguration                 _configuration;


    public MainMenu(ILogger<MainMenu> logger,
                    IHttpClientFactory httpClientFactory,
                    IConfiguration configuration,
                    AccessDocumentServerHttpClient documentServerHttpClient,
                    DocServerDbContext db)
    {
        _logger                   = logger;
        _documentServerHttpClient = documentServerHttpClient;
        _db                       = db;
        _configuration            = configuration;

        _documentServerHttpClient.BaseAddress = new Uri(_configuration["DocumentServer:Host"]);

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
                        for (int j = 0; j < 100; j++)
                        {
                            string tmpFileName = Guid.NewGuid().ToString();
                            string path        = Path.Join($"T:\\temp", tmpFileName);

                            await _documentServerHttpClient.GetDocumentAndSaveToFileSystem(1, path);
                        }

                        break;

                    case ConsoleKey.V:
                        //System.IO.ReturnedDocumentInfo fileToSave2 = new(Path.Combine(@"T:\Temp\RadzenBlazorStudioSetup.exe"));
                        System.IO.FileInfo fileToSave2 = new(Path.Combine(@"T:\downloads\discordsetup.exe"));

                        //System.IO.ReturnedDocumentInfo fileToSave2 = new(Path.Combine(@"T:\crystaldiskinfo.exe"));
                        TransferDocumentDto transferDocumentDto2 = new()
                        {
                            DocumentTypeId = 3,
                            Description    = "Radzen Blazor Studio Setup",
                            FileExtension  = fileToSave2.Extension,
                            RootObjectId   = "1",
                        };

                        Result<long> result2 = await _documentServerHttpClient.SaveDocumentAsync(transferDocumentDto2, fileToSave2.FullName);

                        if (result2.IsSuccess)
                        {
                            Console.WriteLine("Document Stored   |   ID = " + result2.Value);
                            lastDocSaved = result2.Value;
                        }
                        else
                            Console.WriteLine("Document failed to be stored due to errors:  ");

                        foreach (IError resultError in result2.Errors)
                            Console.WriteLine("Error: " + resultError);

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

                        Result<long> result = await _documentServerHttpClient.SaveDocumentAsync(transferDocumentDto, fileToSave.FullName);

                        if (result.IsSuccess)
                        {
                            Console.WriteLine("Document Stored   |   ID = " + result.Value);
                            lastDocSaved = result.Value;
                        }
                        else
                            Console.WriteLine("Document failed to be stored due to errors:  ");

                        foreach (IError resultError in result.Errors)
                            Console.WriteLine("Error: " + resultError);

                        break;


                    case ConsoleKey.Y:
                        List<long>           uploadedDocIds = new();
                        int                  upI            = -1;
                        long                 totalSizeUp    = 0;
                        Stopwatch            swUp           = Stopwatch.StartNew();
                        DirectoryInfo        directoryInfo  = new(@"T:\ProgrammingTesting\Original");
                        System.IO.FileInfo[] origFiles      = directoryInfo.GetFiles();

                        foreach (System.IO.FileInfo xyz in origFiles)
                        {
                            TransferDocumentDto tdo = new()
                            {
                                DocumentTypeId = 3,
                                Description    = xyz.Name,
                                FileExtension  = xyz.Extension,
                                RootObjectId   = "1",
                            };
                            Result<long> resultUp = await _documentServerHttpClient.SaveDocumentAsync(tdo, xyz.FullName);
                            swUp.Stop();

                            if (resultUp.IsSuccess)
                            {
                                totalSizeUp += xyz.Length;
                                Console.WriteLine("Document Stored   |   ID = " + resultUp.Value);
                                uploadedDocIds.Add(resultUp.Value);
                                lastDocSaved = resultUp.Value;
                            }
                            else
                            {
                                Console.WriteLine("Document failed to be stored due to errors:  ");
                                throw new ApplicationException(resultUp.ToString());
                            }

                            foreach (IError resultError in resultUp.Errors)
                                Console.WriteLine("Error: " + resultError);
                        }

                        Console.WriteLine("Total Upload Size: {0}", (totalSizeUp / (1024 * 1024)));
                        Console.WriteLine("Total Time {0} ms", swUp.ElapsedMilliseconds);

                        Console.WriteLine(Environment.NewLine + "Downloading Documents!");

                        Stopwatch swDown = Stopwatch.StartNew();
                        foreach (long docId in uploadedDocIds)
                        {
                            DocumentContainer documentContainerDown = await _documentServerHttpClient.GetDocumentAsync(docId);
                            string extension = documentContainerDown.DocumentInfo.Extension != string.Empty ? "." + documentContainerDown.DocumentInfo.Extension : string.Empty;
                            string fileName = documentContainerDown.DocumentInfo.Description + extension;
                            fileName = Path.Join(@"T:\ProgrammingTesting\Downloaded", fileName);
                            await File.WriteAllBytesAsync(fileName, documentContainerDown.DocumentInfo.FileInBytes);
                            Console.WriteLine("Downloaded File: {0} [ {1} ]", documentContainerDown.DocumentInfo.Description, docId);
                        }

                        swDown.Stop();
                        Console.WriteLine("Total Time {0} ms", swDown.ElapsedMilliseconds);


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

                        string tmpFileNameR = Guid.NewGuid().ToString();
                        string pathR        = Path.Join($"T:\\temp", tmpFileNameR);

                        await _documentServerHttpClient.GetDocumentAndSaveToFileSystem(1, pathR);
                        break;
                    case ConsoleKey.G:
                        Stopwatch sw        = Stopwatch.StartNew();
                        long      totalSize = 0;
                        int       i         = 0;

                        lastDocSaved = 3;
                        for (i = 0; i < 1; i++)
                        {
                            DocumentContainer documentContainer = await _documentServerHttpClient.GetDocumentAsync(lastDocSaved);
                            string            extension = documentContainer.DocumentInfo.Extension != string.Empty ? "." + documentContainer.DocumentInfo.Extension : string.Empty;
                            string            fileName = Guid.NewGuid().ToString() + extension;

                            totalSize += (long)documentContainer.DocumentInfo.Size;

                            fileName = Path.Join($"T:\\temp", fileName);
                            await File.WriteAllBytesAsync(fileName, documentContainer.DocumentInfo.FileInBytes);
                            File.Delete(fileName);
                        }

                        sw.Stop();
                        long totalMB = totalSize / (1024 * 1024);
                        Console.WriteLine("Downloaded File {0} times.  Total Time: {1}  Total MB: {2}",
                                          i,
                                          sw.ElapsedMilliseconds / 1000,
                                          totalMB);

                        Console.WriteLine("SUCCESS:  File Saved as :");
                        break;

                    case ConsoleKey.X: return false;
                }
            }

            // Empty Key Queue
            while (Console.KeyAvailable)
                Console.ReadKey();
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