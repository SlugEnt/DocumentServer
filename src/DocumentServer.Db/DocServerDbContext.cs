﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Interfaces;

namespace SlugEnt.DocumentServer.Db;

public class DocServerDbContext : DbContext
{
    private static IConfigurationRoot? _Configuration;


    // Reference / Lookup Models

    // Entity / Transactional Models



    // *********************************************************************************************
    public DocServerDbContext() { }


    public DocServerDbContext(DbContextOptions<DocServerDbContext> options) : base(options)
    {
        // TODO Change to optional value or move back to 30 seconds.
        Database.SetCommandTimeout(new TimeSpan(0,
                                                0,
                                                0,
                                                5));
    }


    public DbSet<Application> Applications { get; set; }
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<ExpiringDocument> ExpiringDocuments { get; set; }
    public DbSet<RootObject> RootObjects { get; set; }
    public DbSet<ReplicationTask> ReplicationTasks { get; set; }
    public DbSet<StorageNode> StorageNodes { get; set; }
    public DbSet<ServerHost> ServerHosts { get; set; }
    public DbSet<VitalInfo> VitalInfos { get; set; }


    // Models 
    public DbSet<StoredDocument> StoredDocuments { get; set; }


    /// <summary>
    ///     Implements custom logic to add Created and Modified audit entries upon save.  And prevent the updating of WORM
    ///     Fields (Fields that can only be written to on initial creation)
    /// </summary>
    private void CustomSaveChanges()
    {
        ChangeTracker tracker = ChangeTracker;

        foreach (EntityEntry entry in tracker.Entries())
        {
            if (entry.State == EntityState.Unchanged)
                continue;

            if (entry.Entity is AbstractBaseEntity)
            {
                IBaseEntity baseEntity = (IBaseEntity)entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAtUTC = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                    case EntityState.Modified:
                        baseEntity.ModifiedAtUTC = DateTime.UtcNow;

                        // Call derived class to prevent updating any WORM fields
                        if (baseEntity.HasWormFields())
                            baseEntity.OnEditRemoveWORMFields(entry);
                        break;
                }
            }
        }
    }


    /// <summary>
    ///     Returns the value the database is referenced by in AppSettings and other locations.
    /// </summary>
    /// <returns></returns>
    public static string DatabaseReferenceName() => "DocumentServerDB";



    protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        //Console.WriteLine("Database:  Configuring DB Context Options");
        if (!dbContextOptionsBuilder.IsConfigured)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                                      .AddJsonFile("AppSettings.json", true, true)
                ;
            _Configuration = builder.Build();
            string connectionString = _Configuration.GetConnectionString(DatabaseReferenceName());
            dbContextOptionsBuilder.UseSqlServer(connectionString);
        }
    }



    /// <summary>
    ///     Provides Additional functionality when creating entites.
    ///     - Seeding UserAccout with system users
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Document Types have multiple fields pointing to StorageNode
        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.ActiveStorageNode1)
                    .WithMany(x => x.ActiveNode1DocumentTypes)
                    .HasForeignKey(x => x.ActiveStorageNode1Id);

        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.ActiveStorageNode2)
                    .WithMany(x => x.ActiveNode2DocumentTypes)
                    .HasForeignKey(x => x.ActiveStorageNode2Id);

        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.ArchivalStorageNode1)
                    .WithMany(x => x.ArchivalNode1DocumentTypes)
                    .HasForeignKey(x => x.ArchivalStorageNode1Id);

        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.ArchivalStorageNode2)
                    .WithMany(x => x.ArchivalNode2DocumentTypes)
                    .HasForeignKey(x => x.ArchivalStorageNode2Id);
        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.Application)
                    .WithMany(x => x.DocumentTypes)
                    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentType>()
                    .HasOne(x => x.RootObject)
                    .WithMany(x => x.DocumentTypes)
                    .OnDelete(DeleteBehavior.Restrict);

        // Stored Documents have multiple Fields for StorageNode
        modelBuilder.Entity<StoredDocument>()
                    .HasOne(x => x.PrimaryStorageNode)
                    .WithMany(x => x.PrimaryNodeStoredDocuments)
                    .HasForeignKey(x => x.PrimaryStorageNodeId);
        modelBuilder.Entity<StoredDocument>()
                    .HasOne(x => x.SecondaryStorageNode)
                    .WithMany(x => x.SecondaryNodeStoredDocuments)
                    .HasForeignKey(x => x.SecondaryStorageNodeId);

        // Replication Tasks have a To and From StorageNodes
        modelBuilder.Entity<ReplicationTask>()
                    .HasOne(x => x.ReplicateFromStorageNode)
                    .WithMany(x => x.ReplicationTaskFromNodes)
                    .HasForeignKey(x => x.ReplicateFromStorageNodeId)
                    .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReplicationTask>()
                    .HasOne(x => x.ReplicateToStorageNode)
                    .WithMany(x => x.ReplicationTaskToNodes)
                    .HasForeignKey(x => x.ReplicateToStorageNodeId)
                    .OnDelete(DeleteBehavior.Restrict);


        // Seed Data.
        modelBuilder.Entity<VitalInfo>(entity =>
        {
            entity.HasData(
                           new VitalInfo
                           {
                               Id            = VitalInfo.VI_LASTKEYENTITY_UPDATED,
                               LastUpdateUtc = DateTime.MinValue.AddMilliseconds(1),
                               Name          = "Last Update to Key Entities",
                               ValueLong     = 0,
                               ValueString   = ""
                           }
                          );
        });
    }



    /// <summary>
    ///     Custom save logic, that stores Created and Modified information for those entities that have Auditable properties.
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
    ///         This method will automatically call
    ///         <see cref="M:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges" /> to discover any
    ///         changes to entity instances before saving to the underlying database. This can be disabled via
    ///         <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" />.
    ///     </para>
    ///     <para>
    ///         Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance.
    ///         This
    ///         includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
    ///         Therefore, always await async calls immediately, or use separate DbContext instances for operations that
    ///         execute
    ///         in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for
    ///         more information.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the
    ///     task to complete.
    /// </param>
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
    /// <exception cref="T:System.OperationCanceledException">
    ///     If the <see cref="T:System.Threading.CancellationToken" /> is
    ///     canceled.
    /// </exception>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        CustomSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }


    // Disable Change Tracking for queries.
    private void SetChangeTrackingBehaviour()
    {
        // Do not do this unless you really know what you are doing!
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}