#define RESET_DATABASE



using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.CompilerServices;
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


    /// <summary>
    /// Used to stored Id's for objects used during testing.
    /// </summary>
    public static Dictionary<string, object> IdLookupDictionary { get; private set; } = new Dictionary<string, object>();


    private static void SeedData(DocServerDbContext db)
    {
        // Add Applications
        Application appA = new()
        {
            Name     = "App_A",
            Token    = TestConstants.APPA_TOKEN,
            IsActive = true,
        };
        Application appB = new()
        {
            Name     = "App_B",
            Token    = TestConstants.APPB_TOKEN,
            IsActive = true,
        };
        db.Add(appA);
        db.Add(appB);
        db.SaveChanges();
        IdLookupDictionary.Add(appA.Name, appA);
        IdLookupDictionary.Add(appB.Name, appB);


        // Add a Root Object For each Application
        RootObject rootA = new()
        {
            ApplicationId = appA.Id,
            Name          = "Claim #",
            Description   = "Claim Number of Auto Policy",
            IsActive      = true,
        };

        RootObject rootB = new()
        {
            ApplicationId = appB.Id,
            Name          = "Movie #",
            Description   = "The movie",
            IsActive      = true,
        };

        RootObject rootC = new()
        {
            ApplicationId = appB.Id,
            Name          = "Actor",
            Description   = "The actors professional screen writers Id",
            IsActive      = true,
        };

        db.Add(rootA);
        db.Add(rootB);
        db.Add(rootC);
        db.SaveChanges();
        IdLookupDictionary.Add("Root_A", rootA);
        IdLookupDictionary.Add("Root_B", rootB);
        IdLookupDictionary.Add("Root_C", rootC);


        //  Add ServerHost
        string localHost = Dns.GetHostName();
        ServerHost hostA = new()
        {
            IsActive = true,
            NameDNS  = localHost,
            FQDN     = localHost + ".abc.local",
            Path     = "hostA",
        };
        db.Add(hostA);
        ServerHost hostB = new()
        {
            IsActive = true,
            NameDNS  = "otherHost",
            FQDN     = "otherHost.abc.local",
            Path     = "hostB",
        };
        db.Add(hostB);
        db.SaveChanges();
        IdLookupDictionary.Add("ServerHost_A", hostA);
        IdLookupDictionary.Add("ServerHost_B", hostB);


        // Add Storage Nodes
        StorageNode testA = new(TestConstants.STORAGE_NODE_TEST_A,
                                "Test Node A - Primary",
                                true,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_TEST_PRIMARY,
                                true);
        testA.ServerHostId = hostA.Id;

        StorageNode testB = new(TestConstants.STORAGE_NODE_TEST_B,
                                "Test Node B - Secondary",
                                true,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_TEST_SECONDARY,
                                true);
        testB.ServerHostId = hostA.Id;

        // True Production Nodes
        StorageNode prodX = new(TestConstants.STORAGE_NODE_PROD_X,
                                "Production Node X - Primary",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_PROD_PRIMARY,
                                true);
        prodX.ServerHostId = hostA.Id;

        StorageNode prodY = new(TestConstants.STORAGE_NODE_PROD_Y,
                                "Production Node Y - Secondary",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_PROD_SECONDARY,
                                true);
        prodY.ServerHostId = hostA.Id;

        StorageNode testC = new(TestConstants.STORAGE_NODE_TEST_C,
                                "Test Node C - Other Host",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                TestConstants.FOLDER_TEST_SECONDARYC,
                                true);
        testC.ServerHostId = hostB.Id;

        db.AddRange(testA,
                    testB,
                    testC,
                    prodX,
                    prodY);
        db.SaveChanges();
        IdLookupDictionary.Add("StorageNodeA", testA);
        IdLookupDictionary.Add("StorageNodeB", testB);
        IdLookupDictionary.Add("StorageNodeC", testC);
        IdLookupDictionary.Add("StorageNodeX", prodX);
        IdLookupDictionary.Add("StorageNodeY", prodY);

        // Add Document Types
        DocumentType docA = new()
        {
            Name               = TestConstants.DOCTYPE_TEST_A,
            Description        = "Test Doc Type A - WORM",
            Application        = appA,
            RootObject         = rootA,
            StorageFolderName  = "WormA",
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
            StorageFolderName  = "TempB",
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
            StorageFolderName  = "EditC",
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
            StorageFolderName  = "PWormX",
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
            StorageFolderName  = "PTempY",
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
            StorageFolderName  = "PReplaceeRA",
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


        IdLookupDictionary.Add("DocType_A", docA);
        IdLookupDictionary.Add("DocType_B", docB);
        IdLookupDictionary.Add("DocType_C", docC);
        IdLookupDictionary.Add("DocType_X", docX);
        IdLookupDictionary.Add("DocType_Y", docY);
        IdLookupDictionary.Add("DocType_RA", docRA);
    }
}