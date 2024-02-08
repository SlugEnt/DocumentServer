using System.Diagnostics;
using Bogus;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
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
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, invalid_storageNode_ID);

        Assert.That(result.IsFailed, Is.True, "Z10:");
    }



    /// <summary>
    /// Validates that the system creates the valid storage path for a file.
    /// </summary>
    /// <param name="storageMode"></param>
    /// <returns></returns>
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


        // Create a random DocumentType.  These document types all will have a custom folder name
        DocumentType randomDocType = new DocumentType()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = storageNode.Id,
            ApplicationId        = 1,
            StorageMode          = storageMode,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7),
        };
        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        // B.
        string         fileName = sm.Faker.Random.Word();
        DocumentType   docType  = randomDocType;
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id);

        Assert.That(result.IsSuccess, Is.True, "B10: " + result.Errors);


        // Z. Validate - Now verify string is correct.
        string modeLetter = storageMode switch
        {
            EnumStorageMode.WriteOnceReadMany => "W",
            EnumStorageMode.Editable          => "E",
            EnumStorageMode.Temporary         => "T",
            EnumStorageMode.Versioned         => "V"
        };


        // Paths should be:  NodePath\ModeLetter\DocTypeFolder\ymd\filename
        string ymdPath = DatePath();
        string expected = Path.Combine(storageNode.NodePath,
                                       modeLetter,
                                       randomDocType.StorageFolderName,
                                       ymdPath);
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }


    /// <summary>
    /// Generates a random upload file
    /// </summary>
    /// <param name="sm"></param>
    /// <param name="expectedDescription"></param>
    /// <param name="expectedExtension"></param>
    /// <param name="expectedDocTypeId"></param>
    /// <returns></returns>
    private Result<TransferDocumentDto> TFX_GenerateUploadFile(SupportMethods sm,
                                                               string expectedDescription,
                                                               string expectedExtension,
                                                               int expectedDocTypeId)
    {
        // A10. Create A Document

        string fileName = sm.WriteRandomFile(sm.FileSystem,
                                             sm.Folder_Test,
                                             expectedExtension,
                                             3);
        string fullPath = Path.Combine(sm.Folder_Test, fileName);
        Assert.IsTrue(sm.FileSystem.FileExists(fullPath), "TFX_GenerateUploadFile:");


        // A20. Read the File
        string file = Convert.ToBase64String(sm.FileSystem.File.ReadAllBytes(fullPath));


        // B.  Now Store it in the DocumentServer
        TransferDocumentDto upload = new TransferDocumentDto()
        {
            Description        = expectedDescription,
            DocumentTypeId     = expectedDocTypeId,
            FileExtension      = expectedExtension,
            FileInBase64Format = file,
        };
        return Result.Ok<TransferDocumentDto>(upload);
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
        int                  expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);

        // TODO - for test only.  DElete please now!
        sm.DB.ChangeTracker.Clear();

        Result<TransferDocumentDto> genFileResult = TFX_GenerateUploadFile(sm,
                                                                           expectedDescription,
                                                                           expectedExtension,
                                                                           expectedDocTypeId);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;


        // Y.  CRITICAL ITEM:  Storage Path - This should be considered a critical test.  If this fails after initial deployment to production
        //     you need to carefully consider why it failed.  
        // Calculate the full storage node path that the file should have been written at.
        DocumentType a          = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == expectedDocTypeId);
        StorageNode  b          = await sm.DB.StorageNodes.SingleAsync(s => s.Id == a.ActiveStorageNode1Id);
        string       modeLetter = ModeLetter(a.StorageMode);

        string expectedPath = Path.Join(b.NodePath,
                                        a.StorageFolderName,
                                        modeLetter,
                                        DatePath());

        // Z. Validate
        // Z.10 - Validate the Database entry

        Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(expectedDocTypeId), "Z20:");
        Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30");
        Assert.That(storedDocument.StorageFolder, Is.EqualTo(expectedPath), "Z40:");

        // Make sure it was stored on the drive.
        string fullFileName = Path.Join(expectedPath, storedDocument.Id.ToString() + "." + storedDocument.FileExtension);
        Assert.That(sm.FileSystem.FileExists(fullFileName), Is.True, "Z90");
    }



    /// <summary>
    /// Validates we can read a document from the library
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ReadDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new SupportMethods(databaseSetupTest, EnumFolderCreation.Test);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        int    expectedDocTypeId   = sm.DocumentType_Test_Worm_A;
        string expectedExtension   = sm.Faker.Random.String2(3);
        string expectedDescription = sm.Faker.Random.String2(32);

        // A.  Generate File and store it
        Result<TransferDocumentDto> genFileResult = TFX_GenerateUploadFile(sm,
                                                                           expectedDescription,
                                                                           expectedExtension,
                                                                           expectedDocTypeId);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;

        // B. Now lets read it.
        Result<string> readResult = await documentServerEngine.ReadStoredDocumentAsync(storedDocument.Id);
        Assert.That(readResult.Value, Is.EqualTo(genFileResult.Value.FileInBase64Format), "Z10:");
    }



    /// <summary>
    /// Internal function to match the calculated date portion of a stored files path.
    /// </summary>
    /// <returns></returns>
    private string DatePath()
    {
        DateTime currentUtc = DateTime.UtcNow;
        string   year       = currentUtc.ToString("yyyy");
        string   month      = currentUtc.ToString("MM");

        //string   day        = currentUtc.ToString("dd");
        return Path.Combine(year, month);
    }


    private string ModeLetter(EnumStorageMode storageMode)
    {
        // Z. Validate - Now verify string is correct.
        string modeLetter = storageMode switch
        {
            EnumStorageMode.WriteOnceReadMany => "W",
            EnumStorageMode.Editable          => "E",
            EnumStorageMode.Temporary         => "T",
            EnumStorageMode.Versioned         => "V"
        };
        return modeLetter;
    }
}