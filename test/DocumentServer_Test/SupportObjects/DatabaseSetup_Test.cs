#define RESET_DATABASE



using DocumentServer.Db;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Extensions.Msal;
using NUnit.Framework;



namespace DocumentServer_Test.SupportObjects;

[TestFixture]
public static class DatabaseSetup_Test
{
    private const string ConnectionString =
        @"server=podmanb.slug.local;Database=UT_DocumentServer;User Id=TestSA;Password=vyja6XVQcPJ2d9bq8g7;TrustServerCertificate=True;";

    private static readonly object _lock = new();
    private static          bool   _databaseInitialized;


    static DatabaseSetup_Test() { Setup(); }


    /// <summary>
    /// Creates unit test DB, Seeds data
    /// </summary>
    static void Setup()
    {
#if RESET_DATABASE
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                var context = CreateContext();
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                SeedData(context);

                _databaseInitialized = true;
            }
        }
#else
        CreateContext();
#endif
    }


    /// <summary>
    /// Create the Database Context for testing
    /// </summary>
    /// <returns></returns>
    public static DocServerDbContext CreateContext()
    {
        DocServerDbContext = new DocServerDbContext(
                                                    new DbContextOptionsBuilder<DocServerDbContext>()
                                                        .UseSqlServer(ConnectionString)
                                                        .Options);
        return DocServerDbContext;
    }


    public static DocServerDbContext DocServerDbContext { get; private set; }


    static void SeedData(DocServerDbContext db)
    {
        // Add Applications
        Application appA = new Application()
        {
            Name = "App_A"
        };
        Application appB = new Application()
        {
            Name = "App_B"
        };
        db.Add<Application>(appA);
        db.Add<Application>(appB);
        db.SaveChanges();


        // Add Storage Nodes
        StorageNode testA = new StorageNode(TestConstants.STORAGE_NODE_TEST_A,
                                            "Test Node A - Primary",
                                            true,
                                            EnumStorageNodeLocation.HostedSMB,
                                            EnumStorageNodeSpeed.Hot,
                                            TestConstants.FOLDER_TEST_PRIMARY);

        StorageNode testB = new StorageNode(TestConstants.STORAGE_NODE_TEST_B,
                                            "Test Node B - Secondary",
                                            true,
                                            EnumStorageNodeLocation.HostedSMB,
                                            EnumStorageNodeSpeed.Hot,
                                            TestConstants.FOLDER_TEST_SECONDARY);

        // True Production Nodes
        StorageNode prodX = new StorageNode(TestConstants.STORAGE_NODE_PROD_X,
                                            "Production Node X - Primary",
                                            false,
                                            EnumStorageNodeLocation.HostedSMB,
                                            EnumStorageNodeSpeed.Hot,
                                            TestConstants.FOLDER_PROD_PRIMARY);

        StorageNode prodY = new StorageNode(TestConstants.STORAGE_NODE_PROD_Y,
                                            "Production Node Y - Secondary",
                                            false,
                                            EnumStorageNodeLocation.HostedSMB,
                                            EnumStorageNodeSpeed.Hot,
                                            TestConstants.FOLDER_PROD_SECONDARY);
        db.AddRange(testA,
                    testB,
                    prodX,
                    prodY);
        db.SaveChanges();


        // Add Document Types
        DocumentType docA = new DocumentType()
        {
            Name               = TestConstants.DOCTYPE_TEST_A,
            Description        = "Test Doc Type A - WORM",
            Application        = appA,
            StorageMode        = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docB = new DocumentType()
        {
            Name               = TestConstants.DOCTYPE_TEST_B,
            Description        = "Test Doc Type B - Temporary",
            Application        = appA,
            StorageMode        = EnumStorageMode.Temporary,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docC = new DocumentType()
        {
            Name               = TestConstants.DOCTYPE_TEST_C,
            Description        = "Test Doc Type C - Editable",
            Application        = appA,
            StorageMode        = EnumStorageMode.Editable,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docX = new DocumentType()
        {
            Name               = TestConstants.DOCTYPE_PROD_X,
            Description        = "Prod Doc Type X - WORM",
            Application        = appA,
            StorageMode        = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1 = prodX,
            ActiveStorageNode2 = prodY,
            IsActive           = true
        };
        DocumentType docY = new DocumentType()
        {
            Name               = TestConstants.DOCTYPE_PROD_Y,
            Description        = "Prod Doc Type Y - Temporary",
            Application        = appA,
            StorageMode        = EnumStorageMode.Temporary,
            ActiveStorageNode1 = prodX,
            ActiveStorageNode2 = prodY,
            IsActive           = true
        };

        db.AddRange(docA,
                    docB,
                    docC,
                    docX,
                    docY);
        db.SaveChanges();
    }
}