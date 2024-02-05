using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConsoleTesting
{
    public partial class MainMenu
    {
        /// <summary>
        /// Seeds the database with some preliminary values
        /// </summary>
        /// <returns></returns>
        public async Task SeedDataAsync()
        {
            await SeedStorageNodesAsync();

            await SeedAbsenceMgtAsync();

            // Load Unity Data
            Application application = new Application
            {
                Name = "Unity"
            };
            _db.Add<Application>(application);
            await _db.SaveChangesAsync();


            application = new Application
            {
                Name = "Phoenix"
            };
            _db.Add<Application>(application);

            // Save Changes
            await _db.SaveChangesAsync();
        }


        /// <summary>
        /// Seed Absence Mgt
        /// </summary>
        /// <returns></returns>
        public async Task SeedAbsenceMgtAsync()
        {
            try
            {
                // Load MDOS Data
                Application application = new Application
                {
                    Name = "Modified Duty Off Site"
                };
                _db.Add<Application>(application);
                await _db.SaveChangesAsync();


                // Create Document Types
                DocumentType docType = new()
                {
                    Name        = "Referral Acceptance Form",
                    Description = "Signed Referral Acceptance Form",
                    StorageMode = EnumStorageMode.WriteOnceReadMany,
                    Application = application
                };
                _db.Add<DocumentType>(docType);


                docType = new()
                {
                    Name        = "Drug Results",
                    Description = "Official Drug Test Results",
                    StorageMode = EnumStorageMode.WriteOnceReadMany,
                    Application = application
                };
                _db.Add<DocumentType>(docType);


                docType = new()
                {
                    Name        = "Draft Work Plan",
                    Description = "Draft of a work plan",
                    StorageMode = EnumStorageMode.Editable,
                    Application = application
                };
                _db.Add<DocumentType>(docType);


                docType = new()
                {
                    Name        = "Temporary Notes",
                    Description = "Notes taken during a meeting",
                    StorageMode = EnumStorageMode.Temporary,
                    Application = application
                };
                _db.Add<DocumentType>(docType);

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("SeedAbsenceMgtAsync:  Error: {Error}  InnerError: {InnerError}", ex.Message,
                                 ex.InnerException != null ? ex.InnerException.Message : "N/A");
            }
        }


        /// <summary>
        /// Seed Storage Nodes
        /// </summary>
        /// <returns></returns>
        public async Task SeedStorageNodesAsync()
        {
            StorageNode snode = new StorageNode("AbsenceMgt Primary", "Primary Storage for Absence Mgt", false, EnumStorageNodeLocation.HostedSMB,
                                                EnumStorageNodeSpeed.Hot, nodePath: @"T:\ProgrammingTesting\absmgt_primary1");
            _db.Add<StorageNode>(snode);

            snode = new StorageNode("AbsenceMgt Secondary", "Secondary Storage for Absence Mgt", false, EnumStorageNodeLocation.HostedSMB,
                                    EnumStorageNodeSpeed.Hot, nodePath: @"T:\ProgrammingTesting\absmgt_secondary1");
            _db.Add<StorageNode>(snode);
        }
    }
}