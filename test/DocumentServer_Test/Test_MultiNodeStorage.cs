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
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using SlugEnt.DocumentServer.EntityManager;
using Test_DocumentServer.SupportObjects;
using SlugEnt.DocumentServer.Models.Enums;
using static Test_DocumentServer.Test_MultiNodeStorage;

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
    ///  1. One Node is local to this host
    ///  2. 2nd Node is remote.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SmallFile2Nodes_COPY_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 1;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode1Id, sm);


        // Make Sure Node 2 has file stored
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        ValidateRemoteFileStored(buildAndTestResult,
                                 storageNode_C2,
                                 hostB,
                                 sm);

        // Validate there is no Replication Task
        ValidateNoReplicationTask(sm, buildAndTestResult.StoredDocument.Id);

        // Use when replication task should be created.
        /*
         ValidateReplicationTask(sm,
                                buildAndTestResult.StoredDocument.Id,
                                storageNode_C1.Id,
                                storageNode_C2.Id);
        */
    }


    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 2 storage node locations defined.
    ///  1. One Node is local to this host
    ///  2. 2nd Node is remote.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SmallFile2Nodes_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 1;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode1Id, sm);


        // Make Sure Node 2 has file stored
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        ValidateRemoteFileStored(buildAndTestResult,
                                 storageNode_C2,
                                 hostB,
                                 sm);

        // Validate there is no Replication Task
        ValidateNoReplicationTask(sm, buildAndTestResult.StoredDocument.Id);

        // Use when replication task should be created.
        /*
         ValidateReplicationTask(sm,
                                buildAndTestResult.StoredDocument.Id,
                                storageNode_C1.Id,
                                storageNode_C2.Id);
        */
    }



    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 2 storage node locations defined.
    ///  Scenario:
    ///   Two nodes defined, one primary and one secondary
    ///   One of the nodes is local and one remote
    ///   File Size is large
    ///  The local node should store the file.  The remote will be queued for later.
    ///  In this test the local node is actually indicated as Secondary on the DocumentType
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task LargeFileWith2NodesDefinedPrimaryIsRemote_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 4000;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode2Id, sm);


        // Validate the Replication Task is correct.
        ValidateReplicationTask(sm,
                                buildAndTestResult.StoredDocument.Id,
                                storageNode_C2.Id,
                                storageNode_C1.Id);
    }



    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 2 storage node locations defined.
    ///  Scenario:
    ///   Two nodes defined, one primary and one secondary
    ///   One of the nodes is local and one remote
    ///   File Size is large
    ///  The local node should store the file.  The remote will be queued for later
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task LargeFileWith2NodesDefined_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 4000;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode1Id, sm);


        // Validate the Replication Task is correct.
        ValidateReplicationTask(sm,
                                buildAndTestResult.StoredDocument.Id,
                                storageNode_C1.Id,
                                storageNode_C2.Id);
    }


    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 1 storage node location defined.
    ///  Scenario:
    ///   Only one node defined.  Is defined as primary.
    ///   Node is remote
    ///   File Size is large, thus not stored on 1st pass
    ///  
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task LargeFileOnlyRemoteNodeStoresFileRemotely_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C2 = null;

        // Note, this is the remote node
        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 4000;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        // If we only have 1 node to store on, then it will always be primary
        BuildAndTestResult buildAndTestResult = testResult.Value;
        Assert.That(buildAndTestResult.StoredDocument.PrimaryStorageNodeId, Is.EqualTo(storageNode_C1.Id), "Z100:");
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        ValidateRemoteFileStored(buildAndTestResult,
                                 storageNode_C1,
                                 hostB,
                                 sm);

        // Should be no validation task as only one node defined
        ValidateNoReplicationTask(sm,
                                  buildAndTestResult.StoredDocument.Id);
    }



    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 1 storage node location defined.
    ///  Scenario:
    ///   Only one node defined.  Is defined as secondary.
    ///   Node is remote
    ///   File Size is large, thus not stored on first try.
    ///   This should still store the file on the remote node and sets its Node1 value to the remote node.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task LargeFileOnlyRemoteNodeStoresFileRemotelyNode2_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = null;

        // Note, this is the remote node
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 4000;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        // If we only have 1 node to store on, then it will always be primary
        BuildAndTestResult buildAndTestResult = testResult.Value;
        Assert.That(buildAndTestResult.StoredDocument.PrimaryStorageNodeId, Is.EqualTo(storageNode_C2.Id), "Z100:");
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        ValidateRemoteFileStored(buildAndTestResult,
                                 storageNode_C2,
                                 hostB,
                                 sm);

        // Validate the Replication Task is correct.
        ValidateNoReplicationTask(sm,
                                  buildAndTestResult.StoredDocument.Id);
    }



    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 2 storage node location defined.
    ///  Scenario:
    ///   Two nodes.
    ///   First is local, 2nd is remote.
    ///   File Size is large (Outside of limit to store to 2nd node on initial save.)
    /// Expected Result:
    ///   Local file stored.  Remote file is not stored, but queued for later replication
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task LargeFileDoesNotImmediatelyStoreOnNode2_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");
        int         fileSize       = 4000;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode1Id, sm);


        // Make Sure Node 2 has file stored
        Assert.That(testResult.Value.StoredDocument.SecondaryStorageNodeId, Is.Null, "Z100:  There should be no value for secondary node.");


        // Validate the Replication Task is correct.
        ValidateReplicationTask(sm,
                                buildAndTestResult.StoredDocument.Id,
                                storageNode_C1.Id,
                                storageNode_C2.Id);
    }


    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 1 storage node location defined.
    ///  Scenario:
    ///   Only one node defined.  Is defined as primary.
    ///   Node is local.
    ///   File Size is small (within limit to store to 2nd node on initial save.
    ///  1. One Node is local to this host.  It is set as primary
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SmallDocumentOneNodePrimary_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C1");
        StorageNode storageNode_C2 = null;

        int fileSize = 1;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  null,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());


        BuildAndTestResult buildAndTestResult = testResult.Value;
        await ValidateLocalFileStored(buildAndTestResult, (int)buildAndTestResult.DocumentType.ActiveStorageNode1Id, sm);

        // Make Sure Node 2 has no file stored
        Assert.That(testResult.Value.StoredDocument.SecondaryStorageNodeId, Is.Null, "Z100:  There should be no value for secondary node.");

        ValidateNoReplicationTask(sm, buildAndTestResult.StoredDocument.Id);
    }



    /// <summary>
    /// This test actually runs the entire cycle for storing a document that has 1 storage node location defined.
    ///  Scenario:
    ///   Only one node defined.  Is defined as secondary
    ///   Node is remote.
    ///   File Size is small (within limit to store to 2nd node on initial save.
    ///   
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SmallDocumentOneNodeSecondary_Success()
    {
        //***  A. Setup
        SupportMethodsConfiguration smConfiguration = new SupportMethodsConfiguration()
        {
            FolderCreationSetting  = EnumFolderCreation.Test,
            UseDatabase            = true,
            UseTransactions        = _useDatabaseTransactions,
            StartSecondAPIInstance = true,
        };
        SupportMethods sm = new(smConfiguration);

        await sm.Initialize;
        if (smConfiguration.StartSecondAPIInstance)
            Assert.That(SecondAPI.StartUpResult.IsSuccess, Is.True, "A100: the startup of the SecondApi failed with | " + SecondAPI.StartUpResult.ToString());

        StorageNode storageNode_C1 = null;
        StorageNode storageNode_C2 = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_C2");

        int fileSize = 1;

        // Test
        Result<BuildAndTestResult> testResult = await BuildTestDocumentAndStoreIt(sm,
                                                                                  storageNode_C1,
                                                                                  storageNode_C2,
                                                                                  fileSize);
        Assert.That(testResult.IsSuccess, Is.True, "T100:  Build and Test Failed.  " + testResult.ToString());

        BuildAndTestResult buildAndTestResult = testResult.Value;


        // Make Sure Node 2 has file stored
        ServerHost hostB = (ServerHost)sm.IDLookupDictionary.GetValueOrDefault("ServerHost_B");
        ValidateRemoteFileStored(buildAndTestResult,
                                 storageNode_C2,
                                 hostB,
                                 sm);

        ValidateNoReplicationTask(sm, buildAndTestResult.StoredDocument.Id);
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



    /// <summary>
    /// Builds a Test Document based upon the parameters given.  It then attempts to store it and returns the results.
    /// </summary>
    /// <param name="sm"></param>
    /// <param name="primaryNode"></param>
    /// <param name="secondaryNode"></param>
    /// <param name="fileSize">In Kilo Bytes</param>
    /// <returns></returns>
    internal async Task<BuildAndTestResult> BuildTestDocumentAndStoreIt(SupportMethods sm,
                                                                        StorageNode primaryNode,
                                                                        StorageNode secondaryNode,
                                                                        int fileSize)
    {
        BuildAndTestResult buildAndTestResult   = new();
        Result             final                = new();
        string             assertPrefix         = "BTDASI:  ";
        string             expectedRootObjectId = sm.Faker.Random.String2(10);
        string?            expectedExternalId   = null;

        Application application = (Application)sm.IDLookupDictionary.GetValueOrDefault("App_A");
        RootObject  rootObject  = (RootObject)sm.IDLookupDictionary.GetValueOrDefault("Root_A");

        int? primaryNodeId   = primaryNode == null ? null : primaryNode.Id;
        int? secondaryNodeId = secondaryNode == null ? null : secondaryNode.Id;

        DocumentType docMultiNode = new()
        {
            Name                 = "MultiNode - " + sm.Faker.Random.String2(2),
            Description          = "Multinode on node: ",
            ApplicationId        = application.Id,
            RootObjectId         = rootObject.Id,
            StorageFolderName    = "MultiNode",
            StorageMode          = EnumStorageMode.WriteOnceReadMany,
            ActiveStorageNode1Id = primaryNodeId,
            ActiveStorageNode2Id = secondaryNodeId,
            IsActive             = true
        };
        EntityRules entityRules   = new(sm.DB);
        Result      saveDocResult = await entityRules.SaveDocumentTypeAsync(docMultiNode);
        Assert.That(saveDocResult.IsSuccess, Is.True, "BuildDocumentAndStoreIt: Save Document Should succeed.  Error Was: " + saveDocResult.ToStringWithLineFeeds());
        buildAndTestResult.DocumentType = docMultiNode;


        // Force Cache Reset so new Document Type gets loaded
        sm.DocumentServerInformation.ForceCacheRefresh(sm.DB);

        // Generate File
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(docMultiNode.Id,
                                                                              expectedRootObjectId,
                                                                              expectedExternalId,
                                                                              fileSize);
        buildAndTestResult.TransferDocumentDto = genFileResult.Value;

        // Store the document
        Result<StoredDocument> result = await sm.DocumentServerEngine.StoreDocumentNew(genFileResult.Value, TestConstants.APPA_TOKEN);
        buildAndTestResult.StoredDocument = result.Value;


        // Validate some common stuff
        StoredDocument      storedDocument      = result.Value;
        TransferDocumentDto transferDocumentDto = genFileResult.Value;

        Assert.That(storedDocument.FileName, Does.EndWith(transferDocumentDto.FileExtension), assertPrefix + "100: File Extensions do not match");
        Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(docMultiNode.Id), assertPrefix + "200:");
        Assert.That(storedDocument.Description, Is.EqualTo(transferDocumentDto.Description), assertPrefix + "300");


        return (buildAndTestResult);
    }


    /// <summary>
    /// Validates that a Document was correctly stored on the local host storage
    /// </summary>
    /// <param name="buildAndTestResult"></param>
    /// <param name="localStorageNodeId"></param>
    /// <param name="sm"></param>
    /// <returns></returns>
    internal async Task ValidateLocalFileStored(BuildAndTestResult buildAndTestResult,
                                                int localStorageNodeId,
                                                SupportMethods sm)
    {
        string assertPrefix = "ValidateLocalFileStored:  ";

        // Calculate the full storage node path that the file should have been written at.
        Result<string> storageFolderResult = sm.DocumentServerEngine.ComputeStoredDocumentStoragePath(buildAndTestResult.DocumentType);
        Assert.That(storageFolderResult.IsSuccess, Is.True, "A100: " + storageFolderResult);
        Result<string> actualPathResult =
            await sm.DocumentServerEngine.ComputeStorageFullNameAsync(buildAndTestResult.DocumentType, localStorageNodeId, storageFolderResult.Value);

        StoredDocument storedDocument = buildAndTestResult.StoredDocument;
        Assert.That(storedDocument.StorageFolder, Is.EqualTo(storageFolderResult.Value), assertPrefix + "100:");
        string fullPath = Path.Join(actualPathResult.Value, storedDocument.FileName);

        Console.WriteLine("Node 1 Full Path: {0}", fullPath);
        Assert.That(sm.FileSystem.FileExists(fullPath), Is.True, assertPrefix + "200");
    }


    /// <summary>
    /// Validates that a document was correctly stored on the remote host storage.
    /// </summary>
    /// <param name="buildAndTestResult"></param>
    /// <param name="remoteStorageNode"></param>
    /// <param name="remoteHost"></param>
    /// <param name="sm"></param>
    internal void ValidateRemoteFileStored(BuildAndTestResult buildAndTestResult,
                                           StorageNode remoteStorageNode,
                                           ServerHost remoteHost,
                                           SupportMethods sm)
    {
        string assertPrefix = "ValidateRemoteFileStored:  ";
        Result<string> tempCP = sm.DocumentServerEngine.ComputePhysicalStoragePath(remoteHost.Id,
                                                                                   remoteStorageNode,
                                                                                   buildAndTestResult.StoredDocument.StorageFolder,
                                                                                   false);
        string remotePath = Path.Join(tempCP.Value, buildAndTestResult.StoredDocument.FileName);
        string absPath    = remotePath.Replace("C:", @"T:\ProgrammingTesting\HostB");

        //        Assert.That(storedDocument.StorageFolder, Is.EqualTo(storagePathInfoB.Value.StoredDocumentPath), "200:");
        // fullPath = Path.Join(storagePathInfoB.Value.ActualPath, storagePathInfoB.Value.StoredDocumentPath, storedDocument.FileName);
        Console.WriteLine("Node 2 Full Path: {0}", absPath);
        Assert.That(File.Exists(remotePath), Is.True, assertPrefix + "100");
    }


    /// <summary>
    /// Validates that no Replication Task was created
    /// </summary>
    /// <param name="sm"></param>
    /// <param name="storedDocumentId"></param>
    internal void ValidateNoReplicationTask(SupportMethods sm,
                                            long storedDocumentId)
    {
        string assertPrefix = "ValidateReplicationTask:  ";

        // Validate the Replication Task is correct.
        sm.DB.ChangeTracker.Clear();
        List<ReplicationTask> replicationTasks = sm.DB.ReplicationTasks.Where(rt => rt.StoredDocumentId == storedDocumentId).ToList();
        Assert.That(replicationTasks.Count, Is.EqualTo(0), assertPrefix + "100:  There should be no replication tasks for this storeddocument");
        ;
    }



    /// <summary>
    /// Validates that a ReplicationTask was inserted into the database for the StoredDocument
    /// </summary>
    /// <param name="sm"></param>
    /// <param name="storedDocumentId"></param>
    /// <param name="replicationFromNode"></param>
    /// <param name="replicateToNode"></param>
    internal void ValidateReplicationTask(SupportMethods sm,
                                          long storedDocumentId,
                                          int replicationFromNode,
                                          int replicateToNode)
    {
        string assertPrefix = "ValidateReplicationTask:  ";

        // Validate the Replication Task is correct.
        sm.DB.ChangeTracker.Clear();
        List<ReplicationTask> replicationTasks = sm.DB.ReplicationTasks.Where(rt => rt.StoredDocumentId == storedDocumentId).ToList();
        Assert.That(replicationTasks.Count, Is.EqualTo(1), assertPrefix + " 300: Should only be 1 Replication Task for this Stored Document");
        ReplicationTask replicationTask = replicationTasks.First();
        Assert.That(replicationTask.ReplicateFromStorageNodeId, Is.EqualTo(replicationFromNode), assertPrefix + " 301:");
        Assert.That(replicationTask.ReplicateToStorageNodeId, Is.EqualTo(replicateToNode), assertPrefix + " 302:");
    }


    /// <summary>
    /// The results from a Build and Test of a document
    /// </summary>
    internal class BuildAndTestResult
    {
        public TransferDocumentDto TransferDocumentDto { get; set; }
        public StoredDocument StoredDocument { get; set; }
        public DocumentType DocumentType { get; set; }
        public Result Result { get; set; }
    }
}