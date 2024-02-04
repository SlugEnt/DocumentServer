using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;
using System;
using DocumentServer.Models.Entities;


namespace DocumentServer.Db
{
    public class DocServerDbContext : DbContext
    {
        private static IConfigurationRoot _Configuration;


        /// <summary>
        /// Returns the value the database is referenced by in AppSettings and other locations.
        /// </summary>
        /// <returns></returns>
        public static string DatabaseReferenceName() { return "DocumentServerDB"; }


        // Models 
        public DbSet<StoredDocument> StoredDocuments { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }


        // Reference / Lookup Models

        // Entity / Transactional Models



        // *********************************************************************************************
        public DocServerDbContext() : base() { SetChangeTrackingBehaviour(); }


        public DocServerDbContext(DbContextOptions<DocServerDbContext> options) : base(options) { SetChangeTrackingBehaviour(); }


        // Disable Change Tracking for queries.
        private void SetChangeTrackingBehaviour() { this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking; }



        /// <summary>
        /// Provides Additional functionality when creating entites.
        ///  - Seeding UserAccout with system users
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder.Entity<Employee>().HasOne(x => x.Person).WithOne(x => x.Employee).HasForeignKey<Employee>(x => x.PersonId);
            modelBuilder.Entity<Employee>().HasOne(x => x.LastModifiedByPerson);
            modelBuilder.Entity<Employee>().HasOne(x => x.CreatedByPerson);
            */
        }



        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            Console.WriteLine("Hello:  Configuring DB");
            if (!dbContextOptionsBuilder.IsConfigured)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                                          .AddJsonFile("AppSettings.json", optional: true, reloadOnChange: true);
                _Configuration = builder.Build();
                string connectionString = _Configuration.GetConnectionString(DatabaseReferenceName());
                dbContextOptionsBuilder.UseSqlServer(connectionString);
            }
        }



        /// <summary>
        /// Custom save logic, that stores Created and Modified information for those entities that have Auditable properties.
        /// </summary>
        /// <returns></returns>
        public override int SaveChanges()
        {
            CustomSaveChanges();
            return base.SaveChanges();
        }


        /// <summary>
        ///     <para>
        ///         Saves all changes made in this context to the database.
        ///     </para>
        ///     <para>
        ///         This method will automatically call <see cref="M:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges" /> to discover any
        ///         changes to entity instances before saving to the underlying database. This can be disabled via
        ///         <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" />.
        ///     </para>
        ///     <para>
        ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
        ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
        ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
        ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more information.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information.
        /// </remarks>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        ///     A task that represents the asynchronous save operation. The task result contains the
        ///     number of state entries written to the database.
        /// </returns>
        /// <exception cref="T:Microsoft.EntityFrameworkCore.DbUpdateException">
        ///     An error is encountered while saving to the database.
        /// </exception>
        /// <exception cref="T:Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">
        ///     A concurrency violation is encountered while saving to the database.
        ///     A concurrency violation occurs when an unexpected number of rows are affected during save.
        ///     This is usually because the data in the database has been modified since it was loaded into memory.
        /// </exception>
        /// <exception cref="T:System.OperationCanceledException">If the <see cref="T:System.Threading.CancellationToken" /> is canceled.</exception>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            CustomSaveChanges();
            return base.SaveChangesAsync(cancellationToken);
        }


        /// <summary>
        /// Implements custom logic to add Created and Modified audit entries upon save.
        /// </summary>
        private void CustomSaveChanges()
        {
            var tracker = ChangeTracker;
            /*
            foreach (var entry in tracker.Entries())
            {
                if (entry.State == EntityState.Unchanged) continue;
                if (entry.Entity is AbstractBaseEntity)
                {
                    IAuditModel auditEntity = (IAuditModel)entry.Entity;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntity.CreatedDateTime = DateTime.Now;
                            break;
                        case EntityState.Deleted:
                        case EntityState.Modified:
                            auditEntity.LastModifiedDateTime = DateTime.Now;
                            break;
                        default:
                            break;
                    }
                }
            }
            */
            //base.SaveChanges();
        }
    }
}