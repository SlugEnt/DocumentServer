using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;

namespace ConsoleTesting;

public partial class MainMenu
{
    /// <summary>
    ///     Seed Some Info
    /// </summary>
    /// <returns></returns>
    public async Task SeedSomeAppDataAsync()
    {
        try
        {
            Application application = new()
            {
                Name = "AppC"
            };
            _db.Add(application);
            await _db.SaveChangesAsync();


            RootObject rootA = new()
            {
                Application = application,
                Description = "some reference",
                Name        = "Some Reference",
                IsActive    = true,
            };
            _db.Add(rootA);
            await _db.SaveChangesAsync();


            // Create Document Types
            DocumentType docType = new()
            {
                Name        = "Referral Acceptance Form",
                Description = "Signed Referral Acceptance Form",
                StorageMode = EnumStorageMode.WriteOnceReadMany,
                RootObject  = rootA,
                Application = application,
                IsActive    = true,
            };
            _db.Add(docType);


            docType = new DocumentType
            {
                Name        = "Drug Results",
                Description = "Official Drug Test Results",
                StorageMode = EnumStorageMode.WriteOnceReadMany,
                RootObject  = rootA,
                Application = application,
                IsActive    = true,
            };
            _db.Add(docType);


            docType = new DocumentType
            {
                Name        = "Draft Work Plan",
                Description = "Draft of a work plan",
                StorageMode = EnumStorageMode.Editable,
                RootObject  = rootA,
                Application = application,
                IsActive    = true,
            };
            _db.Add(docType);


            docType = new DocumentType
            {
                Name        = "Temporary Notes",
                Description = "Notes taken during a meeting",
                StorageMode = EnumStorageMode.Temporary,
                RootObject  = rootA,
                Application = application,
                IsActive    = true,
            };
            _db.Add(docType);

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("SeedSomeAppDataAsync:  Error: {Error}  InnerError: {InnerError}",
                             ex.Message,
                             ex.InnerException != null ? ex.InnerException.Message : "N/A");
        }
    }


    /// <summary>
    ///     Seeds the database with some preliminary values
    /// </summary>
    /// <returns></returns>
    public async Task SeedDataAsync()
    {
        await SeedStorageNodesAsync();

        await SeedSomeAppDataAsync();

        // Load App A Data
        Application application = new()
        {
            Name = "AppA"
        };
        _db.Add(application);
        await _db.SaveChangesAsync();


        application = new Application
        {
            Name = "Appb"
        };
        _db.Add(application);

        // Save Changes
        await _db.SaveChangesAsync();
    }


    /// <summary>
    ///     Seed Storage Nodes
    /// </summary>
    /// <returns></returns>
    public async Task SeedStorageNodesAsync()
    {
        StorageNode snode = new("AbsenceMgt Primary",
                                "Primary Storage for app",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                @"T:\ProgrammingTesting\absmgt_primary1");
        _db.Add(snode);

        snode = new StorageNode("AbsenceMgt Secondary",
                                "Secondary Storage for Some App",
                                false,
                                EnumStorageNodeLocation.HostedSMB,
                                EnumStorageNodeSpeed.Hot,
                                @"T:\ProgrammingTesting\absmgt_secondary1");
        _db.Add(snode);
    }
}