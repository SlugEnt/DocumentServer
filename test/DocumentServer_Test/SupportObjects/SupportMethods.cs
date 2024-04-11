// This should normally be defined.  This allows unit tests to run without colliding into each other as everything happens in 
// a transaction that is never committed.  
//  - Undef it for specialized cases where you need to see what happened with a particular unit test case by looking at the 
//    database.
#define ENABLE_TRANSACTIONS

using Bogus;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SlugEnt;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using SlugEnt.DocumentServer.ClientLibrary;
using ILogger = Serilog.ILogger;
using Serilog;

namespace Test_DocumentServer.SupportObjects;

/// <summary>
///     This sets up a Test Data class of common items that are needed for each test scenario
///     * A Mock File System
///     * A Database Connection to a Unit Test Database
///     * The DocumentServerEngine
///     * Some Support Methods that can be used during testing
/// </summary>
public class SupportMethods
{
    private static   Faker                                 _faker;
    private readonly MockLogger<DocumentServerEngine>      _logger     = Substitute.For<MockLogger<DocumentServerEngine>>();
    private readonly UniqueKeys                            _uniqueKeys = new("");
    private readonly MockLogger<DocumentServerInformation> _logDSI     = Substitute.For<MockLogger<DocumentServerInformation>>();
    private static   ILogger                               serilog;
    private readonly SupportMethodsConfiguration           _smConfiguration;


    /// <summary>
    /// The Preferred Constructor...
    /// </summary>
    /// <param name="smConfiguration"></param>
    public SupportMethods(SupportMethodsConfiguration smConfiguration)
    {
        _smConfiguration = smConfiguration;
        SetupStage1();
    }


    /// <summary>
    ///     Constructore.  Builds Mock Filesystem, Mock Logger, and the DocServerEngine
    /// </summary>
    /// <param name="createFolders">What Folder Creation Mode to use.  None is the default, which creates none of the Fake File System folders.</param>
    /// <param name="useTransactions">If using the database, this determines if the system should use transactions or not.  Some of the tests use
    /// code that cannot have a nested transaction so it needs to be false.</param>
    /// <param name="useDatabase">If true, the database will be setup and utilized.  If false, it will not.</param>
    /// <param name="overrideDNSName">If set this will override the DNS Host name for the DocumentServerInformation Object on creation using the HostB in the database.</param>
    public SupportMethods(EnumFolderCreation createFolders = EnumFolderCreation.None,
                          bool useTransactions = true,
                          bool useDatabase = true,
                          bool overrideDNSName = false)
    {
        _smConfiguration = new()
        {
            FolderCreationSetting = createFolders,
            UseDatabase           = useDatabase,
            UseTransactions       = useTransactions,
            OverrideDNSName       = overrideDNSName,
        };
        SetupStage1();
    }


    /// <summary>
    /// Performs initial setup of the SupportMethods object.  Then starts the SetupStage2Async 
    /// </summary>
    private void SetupStage1()
    {
        serilog = new LoggerConfiguration().WriteTo.Console().Enrich.FromLogContext().CreateLogger();

        if (_faker == null)
            _faker = new Faker();

        FileSystem = new MockFileSystem();

        Initialize = SetupStage2Async();

        switch (_smConfiguration.FolderCreationSetting)
        {
            case EnumFolderCreation.Test:
                CreateTestFolders();
                break;
            case EnumFolderCreation.Prod:
                CreateProdFolders();
                break;
            case EnumFolderCreation.All:
                CreateTestFolders();
                CreateProdFolders();
                break;
        }
    }


    /// <summary>
    /// Performs Constructor level async operations.
    /// caller must call await Initialize to ensure these operations have completed.
    /// </summary>
    /// <returns></returns>
    private async Task SetupStage2Async()
    {
        // Create a Context specific to this object.  Everything will be run in an uncommitted transaction
        if (_smConfiguration.UseDatabase)
        {
            DB = DatabaseSetup_Test.CreateContext();
            LoadDatabaseInfo();
            string tmsg = "";


            ConsoleColor color = ConsoleColor.Green;
            if (_smConfiguration.UseTransactions)
            {
                DB.Database.BeginTransaction();
                tmsg =
                    "This means nothing is committed to the database from this unit test.  In some test scenarios this can cause unexpected results.  It can be turned off in those cases.";
                color = ConsoleColor.DarkRed;
            }

            Console.ForegroundColor = color;
            Console.WriteLine("*************$$$$$$$$$$$$$$$$$$$$   Using Transactions: {0}   $$$$$$$$$$$$$$$$$$$$*************", _smConfiguration.UseTransactions.ToString());
            Console.WriteLine(tmsg);
            Console.ForegroundColor = ConsoleColor.White;


            // Setup DocumentServer Engine
            // Use OverrideHost name if specified
            string overrideHostName = "";
            if (_smConfiguration.OverrideDNSName)
            {
                ServerHost overrideHost = (ServerHost)this.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
                overrideHostName = overrideHost.NameDNS;
            }

            DocumentServerInformation = new DocumentServerInformation(DB,
                                                                      NodeKey,
                                                                      serilog,
                                                                      overrideHostName);
            await DocumentServerInformation.Initialize;


            DocumentServerEngine = new DocumentServerEngine(_logger,
                                                            DB,
                                                            DocumentServerInformation,
                                                            FileSystem);
        }

        // If start Second API Instance - Get it going...
        if (_smConfiguration.StartSecondAPIInstance)
        {
            SecondAPI  secondApi  = new SecondAPI();
            ServerHost secondHost = (ServerHost)this.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
            secondApi.StartAPI(secondHost.NameDNS);
        }

        IsInitialized = true;
    }


    /// <summary>
    /// This is the Initialize Task.  Must be called to finalize setup of key objects
    /// </summary>
    public Task Initialize { get; private set; }

    /// <summary>
    ///     Returns the DB Context
    /// </summary>
    public DocServerDbContext DB { get; private set; }


    /// <summary>
    /// The Node key for this host when Unit Testing.
    /// </summary>
    public string NodeKey
    {
        get { return "UT_KEY"; }
    }

    /// <summary>
    /// Returns True if all initialization is completed.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Return the DocumentServerEngine
    /// </summary>
    public DocumentServerEngine DocumentServerEngine { get; private set; }

    /// <summary>
    ///    Returns the DocumentServerInformation object
    /// </summary>
    public DocumentServerInformation DocumentServerInformation { get; private set; }


    /// <summary>
    ///     Returns the Document Type for a Replaceable document
    /// </summary>
    public int DocumentType_Prod_Replaceable_A { get; private set; }

    /// <summary>
    ///     Returns the Production Document Type Y - Temporary
    /// </summary>
    public int DocumentType_Prod_Temp_Y { get; private set; }

    /// <summary>
    ///     Returns the Production Document Type X - WORM
    /// </summary>
    public int DocumentType_Prod_Worm_X { get; private set; }

    /// <summary>
    ///     Returns the Test Document Type C - Editable
    /// </summary>
    public int DocumentType_Test_Edit_C { get; private set; }

    /// <summary>
    ///     Returns the Test Document Type B - Temporary
    /// </summary>
    public int DocumentType_Test_Temp_B { get; private set; }

    /// <summary>
    ///     Returns the Test Document Type A - WORM
    /// </summary>
    public int DocumentType_Test_Worm_A { get; private set; }


    /// <summary>
    ///     Returns the faker instance
    /// </summary>
    public Faker Faker => _faker;


    /// <summary>
    ///     Many of the Mock File System commands needs a FileDataAcceesor.  This provides that.
    /// </summary>
    public IMockFileDataAccessor FileDataAccessor => FileSystem;


    /// <summary>
    /// Stores key database objects for quicker access and retrieval
    /// </summary>
    public Dictionary<string, object> IDLookupDictionary
    {
        get { return DatabaseSetup_Test.IdLookupDictionary; }
    }

    /// <summary>
    ///     Returns the Mocked File System
    /// </summary>
    public MockFileSystem FileSystem { get; private set; }

    public string Folder_Prod => TestConstants.FOLDER_PROD;
    public string Folder_Prod_Primary => TestConstants.FOLDER_PROD_PRIMARY;
    public string Folder_Prod_Secondary => TestConstants.FOLDER_PROD_SECONDARY;


    // Some Folders 
    public string Folder_Test => TestConstants.FOLDER_TEST;
    public string Folder_Test_Primary => TestConstants.FOLDER_TEST_PRIMARY;
    public string Folder_Test_Secondary => TestConstants.FOLDER_TEST_SECONDARY;

    /// <summary>
    ///     Returns Production Storage Node X Id
    /// </summary>
    public int StorageNode_Prod_X { get; private set; }

    /// <summary>
    ///     Returns Production Storage Node Y Id
    /// </summary>
    public int StorageNode_Prod_Y { get; private set; }


    /// <summary>
    ///     Returns Test Storage Node A Id
    /// </summary>
    public int StorageNode_Test_A { get; private set; }

    /// <summary>
    ///     Returns Test Storage Node B Id
    /// </summary>
    public int StorageNode_Test_B { get; private set; }


    /// <summary>
    ///     Creates the Production Node Storage Folders
    /// </summary>
    public void CreateProdFolders()
    {
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD_PRIMARY);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD_SECONDARY);
    }


    /// <summary>
    ///     Creaates the Test Node Storage Folders
    /// </summary>
    public void CreateTestFolders()
    {
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST_PRIMARY);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST_SECONDARY);
    }



    /// <summary>
    ///     Loads Key Values for database records into properties
    /// </summary>
    private void LoadDatabaseInfo()
    {
        StorageNode node = DB.StorageNodes.Single(s => s.Name == TestConstants.STORAGE_NODE_TEST_A);
        StorageNode_Test_A = node.Id;
        node               = DB.StorageNodes.Single(s => s.Name == TestConstants.STORAGE_NODE_TEST_B);
        StorageNode_Test_B = node.Id;
        node               = DB.StorageNodes.Single(s => s.Name == TestConstants.STORAGE_NODE_PROD_X);
        StorageNode_Prod_X = node.Id;
        node               = DB.StorageNodes.Single(s => s.Name == TestConstants.STORAGE_NODE_PROD_Y);
        StorageNode_Prod_Y = node.Id;

        DocumentType doc = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_TEST_A);
        DocumentType_Test_Worm_A = doc.Id;

        doc                             = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_TEST_B);
        DocumentType_Test_Temp_B        = doc.Id;
        doc                             = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_TEST_C);
        DocumentType_Test_Edit_C        = doc.Id;
        doc                             = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_PROD_X);
        DocumentType_Prod_Worm_X        = doc.Id;
        doc                             = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_PROD_Y);
        DocumentType_Prod_Temp_Y        = doc.Id;
        doc                             = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_REPLACE_A);
        DocumentType_Prod_Replaceable_A = doc.Id;
    }


    /// <summary>
    ///     Resets the DB Context, by creating a new one, which means it is completely empty.
    /// </summary>
    /// <returns></returns>
    public DocServerDbContext ResetContext()
    {
        DB = DatabaseSetup_Test.CreateContext();
        return DB;
    }



    /// <summary>
    ///     Generates a random upload file
    /// </summary>
    /// <param name="sm"></param>
    /// <param name="expectedDescription"></param>
    /// <param name="expectedExtension"></param>
    /// <param name="expectedDocTypeId"></param>
    /// <returns></returns>
    public Result<TransferDocumentDto> TFX_GenerateUploadFile(SupportMethods sm,
                                                              string expectedDescription,
                                                              string expectedExtension,
                                                              int expectedDocTypeId,
                                                              string expectedRootObjectId,
                                                              string? expectedDocExtKey,
                                                              int sizeInKB = 3)
    {
        // A10. Create A Document

        string fileName = sm.WriteRandomFile(sm.FileSystem,
                                             sm.Folder_Test,
                                             expectedExtension,
                                             sizeInKB);
        string fullPath = Path.Combine(sm.Folder_Test, fileName);
        Console.WriteLine("Generated FileName: " + fullPath);
        Assert.That(sm.FileSystem.FileExists(fullPath), Is.True, "TFX_GenerateUploadFile:");


        // Get FormFile 
        FormFile formFile = GetFormFile(fullPath);
        Assert.That(formFile.Length, Is.Not.EqualTo(0), "TFX_GenerateUploadFile Formfile is zero length");

        // B.  Now Store it in the DocumentServer
        TransferDocumentDto upload = new()
        {
            Description       = expectedDescription,
            DocumentTypeId    = expectedDocTypeId,
            FileExtension     = expectedExtension,
            RootObjectId      = expectedRootObjectId,
            DocTypeExternalId = expectedDocExtKey,
            File              = formFile,
        };

        return Result.Ok(upload);
    }


    /// <summary>
    ///     Writes a random file out with a random filename
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="path"></param>
    /// <param name="extension"></param>
    /// <param name="sizeInKB"></param>
    /// <returns></returns>
    public string WriteRandomFile(IFileSystem fileSystem,
                                  string path,
                                  string extension,
                                  int sizeInKB)
    {
        int fileSize = sizeInKB * 1024;

        string filePart = _uniqueKeys.GetKey("abc");
        string fileName = filePart + "." + extension;
        string fullPath = Path.Combine(path, fileName);

        byte[] data = new byte[1024];
        Random rng  = new();
        rng.NextBytes(data);


        MockFileStream stream = new(FileDataAccessor, fullPath, FileMode.Create);
        for (int i = 0; i < sizeInKB; i++)
        {
            rng.NextBytes(data);
            stream.Write(data, 0, data.Length);
        }

        stream.Close();
        return fileName;
    }


    /// <summary>
    /// Creates a FormFile object from a physical file
    /// </summary>
    /// <param name="fullFileName"></param>
    /// <returns></returns>
    public FormFile GetFormFile(string fullFileName)
    {
        FormFile       file;
        MockFileStream stream2 = new(FileDataAccessor, fullFileName, FileMode.Open);

        file = new FormFile(stream2,
                            0,
                            stream2.Length,
                            null,
                            Path.GetFileName(fullFileName))
        {
            Headers     = new HeaderDictionary(),
            ContentType = "application/pdf"
        };


        return file;
    }


    /// <summary>
    /// Creates a FormFile from a Byte Array
    /// </summary>
    /// <param name="fileBytes"></param>
    /// <returns></returns>
    public FormFile GetFormFile(byte[] fileBytes)
    {
        FormFile     file;
        MemoryStream ms = new MemoryStream(fileBytes);
        file = new FormFile(ms,
                            0,
                            ms.Length,
                            null,
                            "somefile.pdf")
        {
            Headers     = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        ms.Seek(0, SeekOrigin.Begin);


        file.OpenReadStream();
        return file;
    }
}