using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using DocumentServer.Db;
using DocumentServer.Models.Entities;
using Microsoft.Identity.Client.Extensions.Msal;
using NSubstitute;
using SlugEnt;

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
    private readonly MockLogger<DocumentServerEngine> _logger = Substitute.For<MockLogger<DocumentServerEngine>>();


    /// <summary>
    ///     Constructore.  Builds Mock Filesystem, Mock Logger, and the DocServerEngine
    /// </summary>
    /// <param name="databaseSetupTest"></param>
    public SupportMethods(DatabaseSetup_Test databaseSetupTest,
                          EnumFolderCreation createFolders = EnumFolderCreation.None)
    {
        // Create a Context specific to this object.  Everything will be run in an uncommitted transaction
        DB = databaseSetupTest.CreateContext();
        LoadDatabaseInfo();
        DB.Database.BeginTransaction();

        DocumentServerEngine = new DocumentServerEngine(_logger, DB, FileSystem);

        switch (createFolders)
        {
            case EnumFolderCreation.None:
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
    ///     Many of the Mock File System commands needs a FileDataAcceesor.  This provides that.
    /// </summary>
    public IMockFileDataAccessor FileDataAccessor => FileSystem;


    /// <summary>
    ///     Returns the Mocked File System
    /// </summary>
    public MockFileSystem FileSystem { get; } = new();


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

        doc                      = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_TEST_B);
        DocumentType_Test_Temp_B = doc.Id;
        doc                      = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_TEST_C);
        DocumentType_Test_Edit_C = doc.Id;
        doc                      = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_PROD_X);
        DocumentType_Prod_Worm_X = doc.Id;
        doc                      = DB.DocumentTypes.Single(s => s.Name == TestConstants.DOCTYPE_PROD_Y);
        DocumentType_Prod_Temp_Y = doc.Id;
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
        int        fileSize   = sizeInKB * 1024;
        UniqueKeys uniqueKeys = new("");
        string     filePart   = uniqueKeys.GetKey("abc");
        string     fileName   = filePart + "." + extension;
        string     fullPath   = Path.Combine(path, fileName);

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
}