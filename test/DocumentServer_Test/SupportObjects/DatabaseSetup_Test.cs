#define RESET_DATABASE


using DocumentServer.Db;
using DocumentServer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;



namespace DocumentServer_Test.SupportObjects;

[TestFixture]
public class DatabaseSetup_Test
{
    private const string ConnectionString =
        @"server=podmanb.slug.local;Database=UT_DocumentServer;User Id=TestSA;Password=vyja6XVQcPJ2d9bq8g7;TrustServerCertificate=True;";

    private static readonly object _lock = new();
    private static          bool   _databaseInitialized;


    public DatabaseSetup_Test() { Setup(); }


    /// <summary>
    /// Creates unit test DB, Seeds data
    /// </summary>
    public void Setup()
    {
#if RESET_DATABASE
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                var context = CreateContext();
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.AddRange(
                                 new Application()
                                 {
                                     Name = "App_A"
                                 },
                                 new Application()
                                 {
                                     Name = "App_B"
                                 });
                context.SaveChanges();

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
    public DocServerDbContext CreateContext()
    {
        DocServerDbContext = new DocServerDbContext(
                                                    new DbContextOptionsBuilder<DocServerDbContext>()
                                                        .UseSqlServer(ConnectionString)
                                                        .Options);
        return DocServerDbContext;
    }


    public DocServerDbContext DocServerDbContext { get; private set; }
}