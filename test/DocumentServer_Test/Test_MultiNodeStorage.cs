using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using SlugEnt.DocumentServer.EntityManager;
using Test_DocumentServer.SupportObjects;
using SlugEnt.DocumentServer.Models.Enums;

namespace Test_DocumentServer;

/// <summary>
/// This set of tests validates that the multi-node storage methods work.
/// </summary>
[TestFixture]
public class Test_MultiNodeStorage
{
    /// <summary>
    ///     DocumentServerEngine uses transactions internally.  You cannot have nested transactions, so they must be disabled
    ///     for these tests.
    /// </summary>
    private readonly bool _useDatabaseTransactions = false;



    /// <summary>
    /// Validates that the StoreFileFromRemoteNode method is able to succecssfully store a document.  
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreFileRemote_Success()
    {
        //*** A) Setup
        SupportMethods sm       = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        string         fileName = "somefile.txt";
        string storagePath = Path.Join("remote",
                                       "month",
                                       "day");

        await sm.Initialize;
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        int storageNodeId = sm.StorageNode_Test_A;

        // Create a test file.
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              sm.Faker.Random.String2(32),
                                                                              sm.Faker.Random.String2(3),
                                                                              1,
                                                                              sm.Faker.Random.String2(10),
                                                                              null,
                                                                              1);

        RemoteDocumentStorageDto remoteDocumentStorageDto = new()
        {
            File          = genFileResult.Value.File,
            StorageNodeId = storageNodeId,
            StoragePath   = storagePath,
            FileName      = fileName,
        };


        //*** C) Test
        Result result = await documentServerEngine.StoreFileFromRemoteNode(remoteDocumentStorageDto);

        //*** Y) Final Prep
        StorageNode storageNode = await sm.DB.StorageNodes.SingleOrDefaultAsync(sn => sn.Id == storageNodeId);
        string completePath = Path.Join(sm.DocumentServerInformation.ServerHostInfo.Path,
                                        storageNode.NodePath,
                                        storagePath,
                                        fileName);


        //*** Z) Validate
        Assert.That(sm.FileSystem.File.Exists(completePath), Is.True, "Z20:  File should exist");
    }


    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 2 storage node locations defined.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SendingToSecondNode_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,

            //StartSecondAPIInstance = false,

            //DocumentEngineCacheTTL = 1, // For this test we need it to pick up on added Document Types
        };
        SupportMethods sm = new(smConfiguration);

        int     expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string  expectedExtension    = sm.Faker.Random.String2(3);
        string  expectedDescription  = sm.Faker.Random.String2(32);
        string  expectedRootObjectId = sm.Faker.Random.String2(10);
        string? expectedExternalId   = null;

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;


        //***  B: Create Document Type
        Application application    = (Application)sm.IDLookupDictionary.GetValueOrDefault("App_A");
        RootObject  rootObject     = (RootObject)sm.IDLookupDictionary.GetValueOrDefault("Root_A");
        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");

        DocumentType docMultiNode = new()
        {
            Name                 = "MultiNode",
            Description          = "Multinode Node C1, C2 WORM",
            ApplicationId        = application.Id,
            RootObjectId         = rootObject.Id,
            StorageFolderName    = "MultiNode",
            StorageMode          = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1Id = storageNode_C1.Id,
            ActiveStorageNode2Id = storageNode_C2.Id,
            IsActive             = true
        };
        EntityRules entityRules   = new(sm.DB);
        Result      saveDocResult = await entityRules.SaveDocumentTypeAsync(docMultiNode);
        Assert.That(saveDocResult.IsSuccess, Is.True, "B10: Save Document Should succeed.  Error Was: " + saveDocResult.ToStringWithLineFeeds());


        //***  C. Generate file to upload
        // Force Cache Reset so new Document Type gets loaded
        sm.DocumentServerInformation.ForceCacheRefresh(sm.DB);
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              expectedDescription,
                                                                              expectedExtension,
                                                                              docMultiNode.Id,
                                                                              expectedRootObjectId,
                                                                              expectedExternalId);

        //***  T. Test
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentNew(genFileResult.Value, TestConstants.APPA_TOKEN);
        StoredDocument         storedDocument = result.Value;


        // Y.  CRITICAL ITEM:  Storage Path - This should be considered a critical test.  If this fails after initial deployment to production
        //     you need to carefully consider why it failed.  
        // Calculate the full storage node path that the file should have been written at.
        Result<StoragePathInfo> storagePathInfoA = await sm.DocumentServerEngine.ComputeStorageFullNameAsync(docMultiNode, (int)docMultiNode.ActiveStorageNode1Id);

        // Z. Validate

        //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.FileName, Does.EndWith(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(docMultiNode.Id), "Z20:");
        Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30");

        // Make Sure Node 1 - StorageInfo Document path is correct and that the document is stored there,
        Assert.That(storedDocument.StorageFolder, Is.EqualTo(storagePathInfoA.Value.StoredDocumentPath), "100:");
        string fullPath = Path.Join(storagePathInfoA.Value.ActualPath, storedDocument.FileName);
        Console.WriteLine("Node 1 Full Path: {0}", fullPath);
        Assert.That(sm.FileSystem.FileExists(fullPath), Is.True, "110");

        // Make Sure Node 2 - StorageInfo Document path is correct and that the document is stored there,
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        Result<string> tempCP = sm.DocumentServerEngine.ComputePhysicalStoragePath(hostB.Id,
                                                                                   storageNode_C2,
                                                                                   storagePathInfoA.Value.StoredDocumentPath,
                                                                                   false);
        string remotePath = Path.Join(tempCP.Value, storedDocument.FileName);
        string absPath    = remotePath.Replace("C:", @"T:\ProgrammingTesting\HostB");

//        Assert.That(storedDocument.StorageFolder, Is.EqualTo(storagePathInfoB.Value.StoredDocumentPath), "200:");
        // fullPath = Path.Join(storagePathInfoB.Value.ActualPath, storagePathInfoB.Value.StoredDocumentPath, storedDocument.FileName);
        Console.WriteLine("Node 2 Full Path: {0}", absPath);
        Assert.That(File.Exists(remotePath), Is.True, "210");
    }


    [Test]
    public async Task IsAlive_Success()
    {
        //*** A. Setup
        SupportMethodsConfiguration smConfiguration = new()
        {
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);
        await sm.Initialize;
        Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        DocumentServerEngine dse = sm.DocumentServerEngine;

        HttpClient           httpClient     = new HttpClient();
        NodeToNodeHttpClient nodeHttpClient = new(httpClient);
        nodeHttpClient.NodeKey = sm.NodeKey;


        //*** T. Test
        string nodeAddress   = "localhost:" + SecondAPI.Port;
        Result isAliveResult = await nodeHttpClient.AskIfAlive(nodeAddress);

        //*** Z. Validate
        Assert.That(isAliveResult.IsSuccess, Is.True, "Z100: IsAlive http call did not succeed.  |  " + isAliveResult.ToString());
    }


    [Test]
    public async Task something()
    {
        //var x = new 
    }
}