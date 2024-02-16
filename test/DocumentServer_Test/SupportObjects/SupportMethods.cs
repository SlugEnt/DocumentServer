// This should normally be defined.  This allows unit tests to run without colliding into each other as everything happens in 
// a transaction that is never committed.  
//  - Undef it for specialized cases where you need to see what happened with a particular unit test case by looking at the 
//    database.
#define ENABLE_TRANSACTIONS

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Bogus;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using DocumentServer.Db;
using DocumentServer.Models.Entities;
using Microsoft.Identity.Client.Extensions.Msal;
using NSubstitute;
using SlugEnt;
using SlugEnt.FluentResults;

namespace DocumentServer_Test.SupportObjects;

/// <summary>
///     This sets up a Test Data class of common items that are needed for each test scenario
///     * A Mock File System
///     * A Database Connection to a Unit Test Database
///     * The DocumentServerEngine
///     * Some Support Methods that can be used during testing
/// </summary>
public class SupportMethods
{
    private readonly MockLogger<DocumentServerEngine> _logger     = Substitute.For<MockLogger<DocumentServerEngine>>();
    private static   Faker                            _faker      = null;
    private          UniqueKeys                       _uniqueKeys = new("");


    /// <summary>
    ///     Constructore.  Builds Mock Filesystem, Mock Logger, and the DocServerEngine
    /// </summary>
    /// <param name="databaseSetupTest"></param>
    public SupportMethods(EnumFolderCreation createFolders = EnumFolderCreation.None,
                          bool useTransactions = true)
    {
        if (_faker == null)
            _faker = new("en");

        FileSystem = new MockFileSystem();

        // Create a Context specific to this object.  Everything will be run in an uncommitted transaction
        DB = DatabaseSetup_Test.CreateContext();
        LoadDatabaseInfo();
        if (useTransactions)
            DB.Database.BeginTransaction();

        DocumentServerEngine = new DocumentServerEngine(_logger, DB, FileSystem);

        switch (createFolders)
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
    ///     Returns the DB Context
    /// </summary>
    public DocServerDbContext DB { get; }


    /// <summary>
    ///     Return the DocumentServerEngine
    /// </summary>
    public DocumentServerEngine DocumentServerEngine { get; }


    /// <summary>
    /// Returns the faker instance
    /// </summary>
    public Faker Faker
    {
        get { return _faker; }
    }


    /// <summary>
    ///     Many of the Mock File System commands needs a FileDataAcceesor.  This provides that.
    /// </summary>
    public IMockFileDataAccessor FileDataAccessor => FileSystem;


    /// <summary>
    ///     Returns the Mocked File System
    /// </summary>
    public MockFileSystem FileSystem { get; private set; }


    /// <summary>
    /// Creaates the Test Node Storage Folders
    /// </summary>
    public void CreateTestFolders()
    {
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST_PRIMARY);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_TEST_SECONDARY);
    }


    /// <summary>
    /// Creates the Production Node Storage Folders
    /// </summary>
    public void CreateProdFolders()
    {
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD_PRIMARY);
        FileSystem.Directory.CreateDirectory(TestConstants.FOLDER_PROD_SECONDARY);
    }


    // Some Folders 
    public string Folder_Test => TestConstants.FOLDER_TEST;
    public string Folder_Test_Primary => TestConstants.FOLDER_TEST_PRIMARY;
    public string Folder_Test_Secondary => TestConstants.FOLDER_TEST_SECONDARY;
    public string Folder_Prod => TestConstants.FOLDER_PROD;
    public string Folder_Prod_Primary => TestConstants.FOLDER_PROD_PRIMARY;
    public string Folder_Prod_Secondary => TestConstants.FOLDER_PROD_SECONDARY;


    /// <summary>
    /// Returns Test Storage Node A Id
    /// </summary>
    public int StorageNode_Test_A { get; private set; }

    /// <summary>
    /// Returns Test Storage Node B Id
    /// </summary>
    public int StorageNode_Test_B { get; private set; }

    /// <summary>
    /// Returns Production Storage Node X Id
    /// </summary>
    public int StorageNode_Prod_X { get; private set; }

    /// <summary>
    /// Returns Production Storage Node Y Id
    /// </summary>
    public int StorageNode_Prod_Y { get; private set; }

    /// <summary>
    /// Returns the Test Document Type A - WORM
    /// </summary>
    public int DocumentType_Test_Worm_A { get; private set; }

    /// <summary>
    /// Returns the Test Document Type B - Temporary
    /// </summary>
    public int DocumentType_Test_Temp_B { get; private set; }

    /// <summary>
    /// Returns the Test Document Type C - Editable
    /// </summary>
    public int DocumentType_Test_Edit_C { get; private set; }

    /// <summary>
    /// Returns the Production Document Type X - WORM
    /// </summary>
    public int DocumentType_Prod_Worm_X { get; private set; }

    /// <summary>
    /// Returns the Production Document Type Y - Temporary
    /// </summary>
    public int DocumentType_Prod_Temp_Y { get; private set; }

    /// <summary>
    /// Returns the Document Type for a Replaceable document
    /// </summary>
    public int DocumentType_Prod_Replaceable_A { get; private set; }



    /// <summary>
    /// Loads Key Values for database records into properties
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


        MockFileStream stream = new(FileDataAccessor, fullPath, FileMode.Create);
        for (int i = 0; i < sizeInKB; i++)
        {
            rng.NextBytes(data);
            stream.Write(data, 0, data.Length);
        }

        return fileName;
    }



    /// <summary>
    /// Generates a random upload file
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
                                                              string? expectedDocExtKey)
    {
        // A10. Create A Document

        string fileName = sm.WriteRandomFile(sm.FileSystem,
                                             sm.Folder_Test,
                                             expectedExtension,
                                             3);
        string fullPath = Path.Combine(sm.Folder_Test, fileName);
        Console.WriteLine("Generated FileName: " + fullPath);
        Assert.IsTrue(sm.FileSystem.FileExists(fullPath), "TFX_GenerateUploadFile:");


        // A20. Read the File
        string file = Convert.ToBase64String(sm.FileSystem.File.ReadAllBytes(fullPath));


        // B.  Now Store it in the DocumentServer
        TransferDocumentDto upload = new TransferDocumentDto()
        {
            Description        = expectedDescription,
            DocumentTypeId     = expectedDocTypeId,
            FileExtension      = expectedExtension,
            FileInBase64Format = file,
            RootObjectId       = expectedRootObjectId,
            DocTypeExternalId  = expectedDocExtKey
        };
        return Result.Ok<TransferDocumentDto>(upload);
    }
}