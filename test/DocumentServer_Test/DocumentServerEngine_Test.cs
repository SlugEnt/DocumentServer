﻿using DocumentServer.Core;
using DocumentServer.Models.Entities;
using DocumentServer_Test.SupportObjects;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SlugEnt;
using SlugEnt.FluentResults;

namespace DocumentServer_Test;

[TestFixture]
public class DocumentServerEngine_Test
{
    private DatabaseSetup_Test databaseSetupTest = new DatabaseSetup_Test();


    [SetUp]
    public void Setup() { }


    /// <summary>
    /// Test that Folder is correctly computed for a Worm style storage node path
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_Worm_Success()
    {
        // A. Setup
        SupportMethods       sm         = new SupportMethods(databaseSetupTest);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        UniqueKeys           uniqueKeys = new("");
        string               filePart   = uniqueKeys.GetKey("fn");


        // B.
        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Worm_A);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id, fileName);
        Assert.That(result.IsSuccess, Is.True, "B10:");


        // F.  Build Expected Value
        // This is a Worm container
        StorageNode snode    = await sm.DB.StorageNodes.SingleAsync(n => n.Id == docType.ActiveStorageNode1Id);
        string      expected = Path.Combine(snode.NodePath, "W", fileName);

        // Z. Validate
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }



    /// <summary>
    /// Test that Folder is correctly computed for a Worm style storage node path
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_Tempoary_Success()
    {
        // A. Setup
        SupportMethods       sm         = new SupportMethods(databaseSetupTest);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        UniqueKeys           uniqueKeys = new("");
        string               filePart   = uniqueKeys.GetKey("fn");


        // B.
        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Temp_B);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id, fileName);
        Assert.That(result.IsSuccess, Is.True, "B10:");


        // F.  Build Expected Value
        // This is a Temporary container
        StorageNode snode    = await sm.DB.StorageNodes.SingleAsync(n => n.Id == docType.ActiveStorageNode1Id);
        string      expected = Path.Combine(snode.NodePath, "T", fileName);

        // Z. Validate
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }


    /// <summary>
    /// Test that Folder is correctly computed for a Worm style storage node path
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_Editable_Success()
    {
        // A. Setup
        SupportMethods       sm         = new SupportMethods(databaseSetupTest);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        UniqueKeys           uniqueKeys = new("");
        string               filePart   = uniqueKeys.GetKey("fn");


        // B.
        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Edit_C);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id, fileName);
        Assert.That(result.IsSuccess, Is.True, "B10:");


        // F.  Build Expected Value
        // This is a Worm container
        StorageNode snode    = await sm.DB.StorageNodes.SingleAsync(n => n.Id == docType.ActiveStorageNode1Id);
        string      expected = Path.Combine(snode.NodePath, "E", fileName);

        // Z. Validate
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }


    /// <summary>
    /// Validate that the ComputeStorageFolder method throws if it cannot find the StorageNode
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_ReturnsFailure_On_InvalidNode()
    {
        // A. Setup
        SupportMethods       sm                     = new SupportMethods(databaseSetupTest);
        DocumentServerEngine dse                    = sm.DocumentServerEngine;
        UniqueKeys           uniqueKeys             = new("");
        string               filePart               = uniqueKeys.GetKey("fn");
        int                  invalid_storageNode_ID = 999999999;

        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Edit_C);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, invalid_storageNode_ID, fileName);


        Assert.That(result.IsFailed, Is.True, "Z10:");

//        Assert.That(result.MapErrors());


        // This is a Worm container
        //                                                  Assert.ThrowsAsync<InvalidOperationException>(async () =>
        //                                                    await sm.DB.StorageNodes.SingleAsync(n => n.Id == 9999999));
    }
}