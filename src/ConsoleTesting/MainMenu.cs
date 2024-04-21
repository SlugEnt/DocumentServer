using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.FluentResults;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ConsoleTesting;

public partial class MainMenu
{
    private readonly DocServerDbContext             _db;
    private readonly AccessDocumentServerHttpClient _documentServerHttpClient;
    private readonly ILogger                        _logger;
    private readonly JsonSerializerOptions          _options;
    private          long                           lastDocSaved = 0;
    private          IConfiguration                 _configuration;
    private readonly string                         _appToken = "abc";
    private          Faker                          _faker;
    private const    long                           MEGABYTE = (1024 * 1024);
    private const    long                           GIGABYTE = (MEGABYTE * 1024);

    public string BaseFolder { get; set; }
    public string SourceFolder { get; set; }
    public string ApplicationToken { get; set; }
    public string ApiKey { get; set; }
    public string DownloadFolder { get; set; }

    /// <summary>
    /// The Id of the application we are writing too.
    /// </summary>
    public int ApplicationId { get; set; } = 1; // Default to Testing app.

    //public int RootObjectId { get; set; } = 2; // Testing App Root Obj Id

    public int DocumentTypeId { get; set; } = 1; // Testing Document Type Id

    public int ThreadSleepTimeMs { get; set; } = 750; // Thread sleep time between upload/download cycles.


    public MainMenu(ILogger<MainMenu> logger,
                    IHttpClientFactory httpClientFactory,
                    IConfiguration configuration,
                    AccessDocumentServerHttpClient documentServerHttpClient)
    {
        _logger        = logger;
        _configuration = configuration;

        _faker = new Faker();

        // Testing only
        _documentServerHttpClient             = documentServerHttpClient;
        _documentServerHttpClient.BaseAddress = new Uri(_configuration["DocumentServer:Host"]);
        _documentServerHttpClient.ApiKey      = _configuration["DocumentServer:ApiKey"];

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _logger.LogError("Main Menu Configured");
    }



    internal async Task Display()
    {
        Console.WriteLine("Press:  ");
        Console.WriteLine(" ( 1 ) Set Application Id    [ {0} ]", ApplicationId);

        //Console.WriteLine(" ( 2 ) Set Root Object Id [ {0} ]", RootObjectId);
        Console.WriteLine(" ( 3 ) Set Document Type Id  [ {0} ]", DocumentTypeId);
        Console.WriteLine();
        Console.WriteLine("   [ " + SourceFolder + " ]  |  Set Which Documents to use during Y - Load Testing");
        Console.WriteLine(" ( 5 ) Small Docs            ");
        Console.WriteLine(" ( 6 ) Big  Docs            ");
        Console.WriteLine(" ( 7 ) All Docs            ");
        Console.WriteLine();
        Console.WriteLine(" ( 9 ) Thread Sleep Time Ms  [ {0} ]", ThreadSleepTimeMs);

        Console.WriteLine(" ( U ) To Upload a the Keyboard Document");
        Console.WriteLine(" ( R ) (Broken - needs updated ClientLibrary with fix) To Retrieve a file");
        Console.WriteLine(" ( Y ) Load Test of Uploads and Download");

        Console.WriteLine(" ( T ) Testing");
        Console.WriteLine(" ( Z ) To Seed the database");
    }


    internal long StatUploadedDocumentCount { get; set; }
    internal long StatUploadedDocumentKb { get; set; }
    internal long StatUploadTimeMs { get; set; }
    internal decimal StatUploadedBytesPerMsMax { get; set; }
    internal long StatUploadedErrorCount { get; set; }


    internal long StatDownloadedDocumentCount { get; set; }
    internal long StatDownloadTimeMs { get; set; }

    internal long StatDownloadedErrorCount { get; set; }
    internal decimal StatDownloadedBytesPerMsMax { get; set; }

    /// <summary>
    /// Number of sessions in which the uploaded and downloaded exactly matched (files and file size)
    /// </summary>
    internal long StatSessionsExactMatch { get; set; }

    internal long StatSessionsNoMatch { get; set; }
    internal long StatElapsedMs { get; set; }
    internal long StatLastSessionTimeMs { get; set; }
    internal long StatLastSessionDocumentCount { get; set; }
    internal long StatLastSessionMBPerSecond { get; set; }
    internal long StatLastSessionBytes { get; set; }

    /// <summary>
    /// One upload / download cycle is a session.
    /// </summary>
    internal long StatTotalSessions { get; set; }


    internal async Task<bool> MainMenuUserInput()
    {
        try
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                switch (keyInfo.Key)
                {
                    case ConsoleKey.D5:
                        SourceFolder = Path.Join(BaseFolder, "SmallDocs");
                        break;
                    case ConsoleKey.D6:
                        SourceFolder = Path.Join(BaseFolder, "BigDocs");
                        break;
                    case ConsoleKey.D7:
                        SourceFolder = Path.Join(BaseFolder, "AllDocs");
                        break;

                    case ConsoleKey.D9:
                        Console.WriteLine("Please enter the thread sleep time in Ms");
                        string threadStr = Console.ReadLine();
                        if (!int.TryParse(threadStr, out int threadInt))
                            break;

                        ThreadSleepTimeMs = threadInt;
                        break;

                    case ConsoleKey.S:
                        DisplayStats();
                        break;

                    // Upload / Save Document
                    case ConsoleKey.U:
                        System.IO.FileInfo fileToSave = new(Path.Combine(SourceFolder, "keyboard-order-document.pdf"));
                        TransferDocumentDto transferDocumentDto = new()
                        {
                            DocumentTypeId = DocumentTypeId,
                            Description    = "Keyboard Order Document",
                            FileExtension  = fileToSave.Extension,
                            RootObjectId   = _faker.Random.String2(6, 12),
                        };

                        Result<long> result = await _documentServerHttpClient.SaveDocumentFromFileAsync(transferDocumentDto, fileToSave.FullName, ApplicationToken);

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


                    // Retrieve Document
                    case ConsoleKey.R:
                        string tmpFileNameR = Guid.NewGuid().ToString();
                        string pathR        = Path.Join(SourceFolder, "downloaded", tmpFileNameR);

                        string prompt =
                            "Enter the Document number to retrieve.  Or 0 to retrieve last document Uploaded (must be during this session):";
                        Console.WriteLine(prompt);
                        string inputId = Console.ReadLine();

                        if (!long.TryParse(inputId, out long inputDocId))
                            break;

                        if (inputDocId == 0)
                            inputDocId = lastDocSaved;
                        Result getDocResult = await _documentServerHttpClient.GetDocumentAndSaveToFileSystem(inputDocId, pathR, ApplicationToken);
                        if (getDocResult.IsSuccess)
                            Console.WriteLine("Downloaded and saved the file - {0}", tmpFileNameR);
                        else
                            Console.WriteLine("Failed to retrieve document - Error: {0}", getDocResult.ToString());
                        break;


                    // Load Test
                    case ConsoleKey.Y:
                        await LargeTest();

                        break;

                    case ConsoleKey.G:
                        Stopwatch sw        = Stopwatch.StartNew();
                        long      totalSize = 0;
                        int       i         = 0;

                        //lastDocSaved = 108;
                        string fileNameG = "";
                        for (i = 0; i < 1; i++)
                        {
                            Result<ReturnedDocumentInfo> getResult = await _documentServerHttpClient.GetDocumentAsync(lastDocSaved, _appToken);
                            if (getResult.IsSuccess)
                            {
                                ReturnedDocumentInfo returnedDocumentInfo = getResult.Value;
                                string               extension            = returnedDocumentInfo.Extension != string.Empty ? "." + returnedDocumentInfo.Extension : string.Empty;
                                fileNameG = Guid.NewGuid().ToString() + extension;

                                totalSize += (long)returnedDocumentInfo.Size;

                                fileNameG = Path.Join($"T:\\temp", fileNameG);
                                await File.WriteAllBytesAsync(fileNameG, returnedDocumentInfo.FileInBytes);
                                File.Delete(fileNameG);
                            }
                            else
                            {
                                Console.WriteLine("GetDocument failed to retrieve the document:  Error: {0}", getResult.ToString());
                            }
                        }

                        sw.Stop();
                        long totalMB = totalSize / (1024 * 1024);
                        Console.WriteLine("Downloaded File {0} times.  Total Time: {1}  Total MB: {2}",
                                          i,
                                          sw.ElapsedMilliseconds / 1000,
                                          totalMB);

                        Console.WriteLine("SUCCESS:  File Saved as : {0}", fileNameG);
                        break;

                    case ConsoleKey.Z:
                        Console.WriteLine("Seeding the Database...");
                        await SeedDataAsync();
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



    internal async Task LargeTest()
    {
        // Create the Output folder:
        if (!Directory.Exists(DownloadFolder))
            Directory.CreateDirectory(DownloadFolder);

        try
        {
            while (true)
            {
                List<long> uploadedDocIds           = new();
                int        upI                      = -1;
                long       totalSizeUp              = 0;
                long       statTotalUploadedCount   = 0;
                long       statTotaldocumentUpCount = 0;
                long       totalSizeDown            = 0;


                // ******  UPLOAD START
                Stopwatch            swUp          = Stopwatch.StartNew();
                DirectoryInfo        directoryInfo = new(SourceFolder);
                System.IO.FileInfo[] origFiles     = directoryInfo.GetFiles();

                StatLastSessionDocumentCount = 0;
                StatLastSessionBytes         = 0;

                // Start uploading all documents in the folder
                foreach (System.IO.FileInfo xyz in origFiles)
                {
                    // This is the DTO used to send info about the document to the API
                    TransferDocumentDto tdo = new()
                    {
                        // This indicates the MDOS Invoice Document Type Id is 3.
                        DocumentTypeId = this.DocumentTypeId,

                        // This can be anything you want.  I just set to file name.
                        Description = xyz.Name,

                        // This is the extension that belongs to the file.  This is important to be correct.
                        FileExtension = xyz.Extension,

                        // For MDOS Invoices this would be the MDOS Referral Id.
                        RootObjectId = _faker.Random.String2(6, 12),
                    };

                    // Call the Client library to send the document to the server.  
                    // The file will be read from the file system.
                    // NOTE:  There is another method SaveDocumentFromBytesAsync if you already have the file bytes.
                    // This returns a Result Object which is either IsSuccess or IsFailed depending on whether document was successfully retrieved. 
                    // The Result also contains a Value object.  The Value in this case will be the Id of the StoredDocument #.  You need to store this somewhere so you can retrieve it in future.
                    Result<long> resultUp =
                        await _documentServerHttpClient.SaveDocumentFromFileAsync(tdo,
                                                                                  xyz.FullName,
                                                                                  ApplicationToken);


                    // If it worked, then you will need to store the Document Id returned to you.  Which is in ResultUp.Value
                    if (resultUp.IsSuccess)
                    {
                        // Update Stats
                        StatUploadedDocumentCount++;
                        StatUploadedDocumentKb += (xyz.Length / 1024);
                        StatLastSessionDocumentCount++;
                        StatLastSessionBytes += xyz.Length;

                        totalSizeUp += xyz.Length;

                        // This is the Document Id of the document.  Use this to retrieve it in future.
                        Console.WriteLine("Document Stored   |   ID = " + resultUp.Value);
                        uploadedDocIds.Add(resultUp.Value);
                        lastDocSaved = resultUp.Value;
                    }
                    else
                    {
                        StatUploadedErrorCount++;
                        Console.WriteLine("Document failed to be stored due to errors:  ");
                        foreach (IError resultError in resultUp.Errors)
                            Console.WriteLine("Error: " + resultError);

                        //                                throw new ApplicationException(resultUp.ToString());
                    }
                }

                swUp.Stop();
                StatUploadTimeMs          += swUp.ElapsedMilliseconds;
                StatUploadedBytesPerMsMax =  totalSizeUp / swUp.ElapsedMilliseconds;


                Console.WriteLine("Total Upload Size: {0}", (totalSizeUp / (1024 * 1024)));
                Console.WriteLine("Total Time {0} ms", swUp.ElapsedMilliseconds);


                // ******  DOWNLOADING Documents
                Console.WriteLine(Environment.NewLine + "Downloading Documents!");

                Stopwatch swDown = Stopwatch.StartNew();

                // Download each document.
                foreach (long docId in uploadedDocIds)
                {
                    // Get the document.  Returns a Result object that is IsSuccess or IsFailed.  
                    Result<ReturnedDocumentInfo> getDocResult2 =
                        await _documentServerHttpClient.GetDocumentAsync(docId, ApplicationToken);

                    // We got the document?
                    if (getDocResult2.IsSuccess)
                    {
                        StatDownloadedDocumentCount++;

                        // The document and metadata about the document is in the ReturnedDocumentInfo
                        ReturnedDocumentInfo returnedDocumentInfo = getDocResult2.Value;
                        totalSizeDown += (long)returnedDocumentInfo.Size;

                        string fileName = returnedDocumentInfo.Description;
                        fileName = Path.Join(DownloadFolder, fileName);
                        await File.WriteAllBytesAsync(fileName, returnedDocumentInfo.FileInBytes);
                        Console.WriteLine("Downloaded File: {0} [ {1} ]",
                                          returnedDocumentInfo.Description,
                                          docId);
                        StatLastSessionDocumentCount++;
                        StatLastSessionBytes += (long)returnedDocumentInfo.Size;
                    }
                    else
                    {
                        StatDownloadedErrorCount++;
                        Console.WriteLine(
                                          "Failed to retrieve the document from the Document Server.  DocumentServer returned the following message: {0}",
                                          getDocResult2.ToString());
                    }
                }

                swDown.Stop();
                StatDownloadTimeMs += swDown.ElapsedMilliseconds;
                decimal downBytesPerMs = totalSizeDown / swDown.ElapsedMilliseconds;
                if (downBytesPerMs > StatDownloadedBytesPerMsMax)
                    StatDownloadedBytesPerMsMax = downBytesPerMs;


                if (totalSizeDown == totalSizeUp)
                    StatSessionsExactMatch++;
                else
                    StatSessionsNoMatch++;


                StatLastSessionTimeMs =  swDown.ElapsedMilliseconds + swUp.ElapsedMilliseconds;
                StatElapsedMs         += StatLastSessionTimeMs;

                Console.WriteLine("Total Time {0} ms", swDown.ElapsedMilliseconds);
                Console.WriteLine("Total MegaBytes Up/Down:   {0} | {1}",
                                  (totalSizeUp / MEGABYTE),
                                  (totalSizeDown / MEGABYTE));
                Console.WriteLine("Total Session Time: {0} ms",
                                  (swDown.ElapsedMilliseconds + swUp.ElapsedMilliseconds));

                // Write Totals So far:
                DisplayStats();

                Thread.Sleep(ThreadSleepTimeMs);
                if (Console.KeyAvailable)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error:  {0}", ex.Message);
            Console.WriteLine("  Details:  {0}", ex.ToString());
            DisplayStats();
        }
    }


    internal void DisplayStats()
    {
        Console.WriteLine("Overall Statistics:    ******  {0}   *******", SourceFolder);
        Console.WriteLine("  Exact Match Sessions: {0}", StatSessionsExactMatch);
        Console.WriteLine("  Non Match Sessions:   {0}", StatSessionsNoMatch);
        Console.WriteLine("  Uploaded:");
        Console.WriteLine("     Documents:         {0}", StatUploadedDocumentCount);
        Console.WriteLine("     Errors:            {0}", StatUploadedErrorCount);
        Console.WriteLine("     Max Bytes per Ms:  {0}", StatUploadedBytesPerMsMax);
        if (StatUploadedDocumentKb > GIGABYTE)
            Console.WriteLine("     GigaBytes        :  {0}", StatUploadedDocumentKb / MEGABYTE);
        else
            Console.WriteLine("     MegaBytes        :  {0}", StatUploadedDocumentKb / 1024);

        double TotalMBPerSecond = (((double)StatUploadedDocumentKb * 2) / ((double)StatElapsedMs / 1000));
        Console.WriteLine("Up/Down Mb per Second:  {0}", TotalMBPerSecond);
        Console.WriteLine("Last Session:");
        Console.WriteLine("  Total Document Count: {0}", StatLastSessionDocumentCount);
        Console.WriteLine("  Total MegaBytes :     {0}", StatLastSessionBytes / MEGABYTE);
        Console.WriteLine("  Total Elapsed Ms:     {0}", StatLastSessionTimeMs);
        Console.WriteLine("  Total Bytes:          {0}", StatLastSessionBytes);
        double nom    = (StatLastSessionBytes / MEGABYTE);
        double denom  = (double)StatLastSessionTimeMs / (double)1000;
        double result = nom / denom;
        Console.WriteLine("  MB per Millisecond:        {0}", result);
    }


    internal async Task Start()
    {
        bool keepProcessing = true;
        Console.WriteLine("Using an API Key of: {0}", ApiKey);
        Console.WriteLine("Using an Application Token of: {0}", ApplicationToken);
        Console.WriteLine("Note:  The Appliction Token must match to the corresponding application Id");
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