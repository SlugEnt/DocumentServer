using System.Diagnostics;
using Bogus;
using DocumentServer.Core;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using DocumentServer_Test.SupportObjects;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SlugEnt;
using SlugEnt.FluentResults;

namespace DocumentServer_Test;

[TestFixture]
public class Test_DocumentServerEngine
{
    private DatabaseSetup_Test databaseSetupTest = new DatabaseSetup_Test();


    [SetUp]
    public void Setup() { }



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
        string               nodePath               = @"x\y";

        // Create a random Storage
        StorageNode storageNode = new(sm.Faker.Music.Genre(),
                                      sm.Faker.Random.Words(3),
                                      true,
                                      EnumStorageNodeLocation.HostedSMB,
                                      EnumStorageNodeSpeed.Hot,
                                      nodePath);
        sm.DB.Add(storageNode);
        await sm.DB.SaveChangesAsync();

        // Create a random DocumentType WITH AN INVALID StorageNode
        int INVALID_STORAGE_MODE = 0;
        DocumentType randomDocType = new DocumentType()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = storageNode.Id,
            ApplicationId        = 1,
            StorageMode          = (EnumStorageMode)INVALID_STORAGE_MODE,
        };
        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Edit_C);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, invalid_storageNode_ID, fileName);

        Assert.That(result.IsFailed, Is.True, "Z10:");
    }



    [Test]
    [TestCase(EnumStorageMode.Editable)]
    [TestCase(EnumStorageMode.WriteOnceReadMany)]
    [TestCase(EnumStorageMode.Temporary)]
    [TestCase(EnumStorageMode.Versioned)]
    public async Task ComputeStorageFolder_CorrectPath_Generated(EnumStorageMode storageMode)
    {
        // A. Setup
        SupportMethods       sm         = new SupportMethods(databaseSetupTest);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        UniqueKeys           uniqueKeys = new("");
        string               filePart   = uniqueKeys.GetKey("fn");
        string               nodePath   = @"a\b\c\d";

        // Create a random StorageNode
        StorageNode storageNode = new(sm.Faker.Music.Genre(),
                                      sm.Faker.Random.Words(3),
                                      true,
                                      EnumStorageNodeLocation.HostedSMB,
                                      EnumStorageNodeSpeed.Hot,
                                      nodePath);
        sm.DB.Add(storageNode);
        await sm.DB.SaveChangesAsync();


        // Create a random DocumentType
        DocumentType randomDocType = new DocumentType()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = storageNode.Id,
            ApplicationId        = 1,
            StorageMode          = storageMode,
        };
        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        // B.
        string         fileName = sm.Faker.Random.Word();
        DocumentType   docType  = randomDocType;
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id, fileName);

        Assert.That(result.IsSuccess, Is.True, "B10: " + result.Errors);


        // Z. Validate - Now verify string is correct.
        string modeLetter = storageMode switch
        {
            EnumStorageMode.WriteOnceReadMany => "W",
            EnumStorageMode.Editable          => "E",
            EnumStorageMode.Temporary         => "T",
            EnumStorageMode.Versioned         => "V"
        };


        string ymdPath = DatePath();
        string expected = Path.Combine(storageNode.NodePath,
                                       modeLetter,
                                       ymdPath,
                                       fileName);
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }



    /// <summary>
    /// Confirms we can upload a document to the DocumentServer
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new SupportMethods(databaseSetupTest, EnumFolderCreation.Test);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        // A10. Create A Document
        string extension = "pdx";
        string fileName = sm.WriteRandomFile(sm.FileSystem,
                                             sm.Folder_Test,
                                             extension,
                                             3);
        string fullPath = Path.Combine(sm.Folder_Test, fileName);
        Assert.IsTrue(sm.FileSystem.FileExists(fullPath), "A10:");


        // A20. Read the File
        string file = Convert.ToBase64String(sm.FileSystem.File.ReadAllBytes(fullPath));


        // B.  Now Store it in the DocumentServer
        DocumentUploadDTO upload = new DocumentUploadDTO()
        {
            Description    = "Some Description",
            DocumentTypeId = sm.DocumentType_Test_Worm_A,
            FileExtension  = extension,
            FileBytes      = file,
        };

        Result<StoredDocument> result = await documentServerEngine.StoreDocumentFirstTimeAsync(upload);


        // Z. Validate
    }



    private string DatePath()
    {
        DateTime currentUtc = DateTime.UtcNow;
        string   year       = currentUtc.ToString("yyyy");
        string   month      = currentUtc.ToString("MM");
        string   day        = currentUtc.ToString("dd");
        return Path.Combine(year, month, day);
    }
}