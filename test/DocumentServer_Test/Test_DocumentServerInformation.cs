using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Resources;
using NUnit.Framework.Interfaces;
using SlugEnt;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer
{
    [TestFixture]
    [NonParallelizable]
    public class Test_DocumentServerInformation
    {
        [Test]
        public async Task AddDocumentType_Success()
        {
            // A. Setup
            SupportMethods sm = new();

            // Initialize
            await sm.Initialize;
            DocumentServerEngine dse = sm.DocumentServerEngine;

            //***  B: 
            int documentTypeCount = sm.DocumentServerInformation.CachedDocumentTypes.Count;
            int rootObjectCount   = sm.DocumentServerInformation.CachedRootObjects.Count;
            int applicationCount  = sm.DocumentServerInformation.CachedApplications.Count;
            int storageNodeCount  = sm.DocumentServerInformation.CachedStorageNodes.Count;

            // Read the current value for 
            VitalInfo vitalInfo     = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            DateTime  lastUpdateUtc = vitalInfo.LastUpdateUtc;

            // Insert a document Type
            DocumentType randomDocType = new()
            {
                Name                 = sm.Faker.Commerce.ProductName(),
                Description          = sm.Faker.Lorem.Sentence(),
                ActiveStorageNode1Id = sm.StorageNode_Test_A,
                RootObjectId         = 1,
                ApplicationId        = 1,
                StorageMode          = EnumStorageMode.WriteOnceReadMany,
                IsActive             = true,
            };
            EntityRules entityRules = new EntityRules(sm.DB);
            Result      resultSave  = await entityRules.SaveDocumentTypeAsync(randomDocType);

            // Z Validate
            Assert.That(resultSave.IsSuccess, Is.True, "Z100: " + resultSave.ToString());

            sm.DocumentServerInformation.CheckIfKeyEntitiesUpdated(sm.DB);
            Assert.That(sm.DocumentServerInformation.CachedDocumentTypes.Count, Is.GreaterThan(documentTypeCount), "Z200:");

            VitalInfo vitalInfo2 = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            Assert.That(vitalInfo2.LastUpdateUtc, Is.GreaterThan(lastUpdateUtc), "Z300:");
            sm.DB.Database.RollbackTransactionAsync();
        }


        [Test]
        public async Task AddApplication_Success()
        {
            // A. Setup
            SupportMethods sm = new();

            // Initialize
            await sm.Initialize;
            DocumentServerEngine dse = sm.DocumentServerEngine;

            //***  B: 
            int applicationCount = sm.DocumentServerInformation.CachedApplications.Count;
            int appTokenCount    = sm.DocumentServerInformation.CachedApplicationTokenLookup.Count;

            // Read the current value for 
            VitalInfo vitalInfo     = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            DateTime  lastUpdateUtc = vitalInfo.LastUpdateUtc;

            // Insert a document Type
            Application application = new()
            {
                Name     = sm.Faker.Random.String2(7),
                IsActive = true,
            };

            EntityRules entityRules = new EntityRules(sm.DB);
            Result      resultSave  = await entityRules.SaveApplicationAsync(application);


            // Z Validate
            Assert.That(resultSave.IsSuccess, Is.True, "Z100: " + resultSave.ToString());

            sm.DocumentServerInformation.CheckIfKeyEntitiesUpdated(sm.DB);
            Assert.That(sm.DocumentServerInformation.CachedApplications.Count, Is.GreaterThan(applicationCount), "Z200:");
            Assert.That(sm.DocumentServerInformation.CachedApplicationTokenLookup.Count, Is.GreaterThan(appTokenCount), "Z210:");

            VitalInfo vitalInfo2 = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            Assert.That(vitalInfo2.LastUpdateUtc, Is.GreaterThan(lastUpdateUtc), "Z300:");
            sm.DB.Database.RollbackTransactionAsync();
        }


        [Test]
        public async Task AddRootObject_Success()
        {
            // A. Setup
            SupportMethods sm = new();

            // Initialize
            await sm.Initialize;
            DocumentServerEngine dse = sm.DocumentServerEngine;

            //***  B: 
            int rootObjectCount = sm.DocumentServerInformation.CachedRootObjects.Count;

            // Read the current value for 
            VitalInfo vitalInfo     = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            DateTime  lastUpdateUtc = vitalInfo.LastUpdateUtc;

            // Insert a document Type
            RootObject rootObect = new()
            {
                ApplicationId = 1,
                Name          = sm.Faker.Random.String2(4, 5),
                Description   = sm.Faker.Random.String2(10, 15),
                IsActive      = true,
            };

            EntityRules entityRules = new EntityRules(sm.DB);
            Result      resultSave  = await entityRules.SaveRootObjectAsync(rootObect);

            // Z Validate
            Assert.That(resultSave.IsSuccess, Is.True, "Z100: " + resultSave.ToString());
            sm.DocumentServerInformation.CheckIfKeyEntitiesUpdated(sm.DB);
            Assert.That(sm.DocumentServerInformation.CachedRootObjects.Count, Is.GreaterThan(rootObjectCount), "Z200:");

            VitalInfo vitalInfo2 = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            Assert.That(vitalInfo2.LastUpdateUtc, Is.GreaterThan(lastUpdateUtc), "Z300:");
            sm.DB.Database.RollbackTransactionAsync();
        }


        [Test]
        public async Task AddStorageNode_Success()
        {
            // A. Setup
            SupportMethods sm = new();

            // Initialize
            await sm.Initialize;
            DocumentServerEngine dse = sm.DocumentServerEngine;

            //***  B: 
            int storageNodeCount = sm.DocumentServerInformation.CachedStorageNodes.Count;


            // Read the current value for 
            VitalInfo vitalInfo     = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            DateTime  lastUpdateUtc = vitalInfo.LastUpdateUtc;

            // Insert a storage node
            StorageNode storageNode = new(TestConstants.STORAGE_NODE_TEST_A,
                                          sm.Faker.Random.String2(7),
                                          true,
                                          EnumStorageNodeLocation.HostedSMB,
                                          EnumStorageNodeSpeed.Hot,
                                          TestConstants.FOLDER_TEST_PRIMARY,
                                          true);
            storageNode.ServerHostId = 1;

            EntityRules entityRules = new EntityRules(sm.DB);
            Result      resultSave  = await entityRules.SaveStorageNodeAsync(storageNode);

            // Z Validate
            Assert.That(resultSave.IsSuccess, Is.True, "Z100: " + resultSave.ToString());
            sm.DocumentServerInformation.CheckIfKeyEntitiesUpdated(sm.DB);
            Assert.That(sm.DocumentServerInformation.CachedStorageNodes.Count, Is.GreaterThan(storageNodeCount), "Z200:");

            VitalInfo vitalInfo2 = sm.DB.VitalInfos.SingleOrDefault(vi => vi.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            Assert.That(vitalInfo2.LastUpdateUtc, Is.GreaterThan(lastUpdateUtc), "Z300:");
            sm.DB.Database.RollbackTransactionAsync();
        }


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task OverrideDNSName_Success(bool overrideHost)
        {
            SupportMethods sm = new(EnumFolderCreation.None,
                                    true,
                                    true,
                                    overrideHost);
            await sm.Initialize;

            // Get Host name depending on override
            ServerHost? serverHost;
            if (!overrideHost)
                serverHost = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_A");
            else
                serverHost = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
            Assert.That(sm.DocumentServerInformation.ServerHostInfo.ServerFQDN, Is.EqualTo(serverHost.FQDN), "Z100: Host name did not match expected value");
        }
    }
}