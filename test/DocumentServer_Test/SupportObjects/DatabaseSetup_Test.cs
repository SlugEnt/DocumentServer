#define RESET_DATABASE



using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;

namespace Test_DocumentServer.SupportObjects;

//[TestFixture]
public static class DatabaseSetup_Test
{
    private const string ConnectionString =
        @"server=podmanb.slug.local;Database=UT_DocumentServer;User Id=TestSA;Password=vyja6XVQcPJ2d9bq8g7;TrustServerCertificate=True;";

    private static readonly object _lock = new();
    private static          bool   _databaseInitialized;


    static DatabaseSetup_Test() { Setup(); }


    /// <summary>
    ///     Creates unit test DB, Seeds data
    /// </summary>
    private static void Setup()
    {
#if RESET_DATABASE
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                DocServerDbContext context = CreateContext();
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
    ///     Create the Database Context for testing
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


    private static void SeedData(DocServerDbContext db)
    {
        // Add Applications
        Application appA = new()
        {
            Name = "App_A"
        };
        Application appB = new()
        {
            Name = "App_B"
        };
        db.Add(appA);
        db.Add(appB);
        db.SaveChanges();


        // Add a Root Object For each Application
        RootObject rootA = new()
        {
            ApplicationId = appA.Id,
            Name          = "Claim #",
            Description   = "Claim Number of Auto Policy"
        };

        RootObject rootB = new()
        {
            ApplicationId = appB.Id,
            Name          = "Movie #",
            Description   = "The movie"
        };

        RootObject rootC = new()
        {
            ApplicationId = appB.Id,
            Name          = "Actor",
            Description   = "The actors professional screen writers Id"
        };

        db.Add(rootA);
        db.Add(rootB);
        db.Add(rootC);
        db.SaveChanges();


        // Add Storage Nodes
        StorageNode testA = new(TestConstants.STORAGE_NODE_TEST_A,
                                "Test Node A - Primary",
                                true,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_TEST_PRIMARY);

        StorageNode testB = new(TestConstants.STORAGE_NODE_TEST_B,
                                "Test Node B - Secondary",
                                true,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_TEST_SECONDARY);

        // True Production Nodes
        StorageNode prodX = new(TestConstants.STORAGE_NODE_PROD_X,
                                "Production Node X - Primary",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_PROD_PRIMARY);

        StorageNode prodY = new(TestConstants.STORAGE_NODE_PROD_Y,
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
        DocumentType docA = new()
        {
            Name               = TestConstants.DOCTYPE_TEST_A,
            Description        = "Test Doc Type A - WORM",
            Application        = appA,
            RootObject         = rootA,
            StorageMode        = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docB = new()
        {
            Name               = TestConstants.DOCTYPE_TEST_B,
            Description        = "Test Doc Type B - Temporary",
            Application        = appA,
            RootObject         = rootB,
            StorageMode        = EnumStorageMode.Temporary,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docC = new()
        {
            Name               = TestConstants.DOCTYPE_TEST_C,
            Description        = "Test Doc Type C - Editable",
            Application        = appA,
            RootObject         = rootC,
            StorageMode        = EnumStorageMode.Editable,
            ActiveStorageNode1 = testA,
            ActiveStorageNode2 = testB,
            IsActive           = true
        };
        DocumentType docX = new()
        {
            Name               = TestConstants.DOCTYPE_PROD_X,
            Description        = "Prod Doc Type X - WORM",
            Application        = appB,
            RootObject         = rootA,
            StorageMode        = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1 = prodX,
            ActiveStorageNode2 = prodY,
            IsActive           = true
        };
        DocumentType docY = new()
        {
            Name               = TestConstants.DOCTYPE_PROD_Y,
            Description        = "Prod Doc Type Y - Temporary",
            Application        = appB,
            RootObject         = rootB,
            StorageMode        = EnumStorageMode.Temporary,
            ActiveStorageNode1 = prodX,
            ActiveStorageNode2 = prodY,
            IsActive           = true
        };
        DocumentType docRA = new()
        {
            Name               = TestConstants.DOCTYPE_REPLACE_A,
            Description        = "Prod Doc Type RA - Replaceable",
            Application        = appB,
            RootObject         = rootC,
            StorageMode        = EnumStorageMode.Replaceable,
            ActiveStorageNode1 = prodX,
            ActiveStorageNode2 = prodY,
            IsActive           = true
        };
        db.AddRange(docA,
                    docB,
                    docC,
                    docX,
                    docY,
                    docRA);
        db.SaveChanges();
    }
}