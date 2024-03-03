using DocumentServer.Core;
using Microsoft.EntityFrameworkCore;
using SlugEnt;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;
using System.IO;
using Microsoft.AspNetCore.Http;
using SlugEnt.DocumentServer.ClientLibrary;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

[TestFixture]
public class Test_DocumentServerEngine
{
    [OneTimeSetUp]
    public void Setup() { }


    /// <summary>
    ///     DocumentServerEngine uses transactions internally.  You cannot have nested transactions, so they must be disabled
    ///     for these tests.
    /// </summary>
    private readonly bool _useDatabaseTransactions = false;



    /// <summary>
    ///     Validate that the ComputeStorageFolder method throws if it cannot find the StorageNode
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_ReturnsFailure_On_InvalidNode()
    {
        // A. Setup
        SupportMethods       sm                     = new();
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

        // Need to get RootObject's Application ID.
        RootObject rootObject = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == 1);

        DocumentType randomDocType = new()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = storageNode.Id,
            RootObjectId         = rootObject.Id,
            ApplicationId        = rootObject.ApplicationId,
            StorageMode          = (EnumStorageMode)INVALID_STORAGE_MODE
        };
        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        string         fileName = uniqueKeys.GetKey();
        DocumentType   docType  = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == sm.DocumentType_Test_Edit_C);
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, invalid_storageNode_ID);

        Assert.That(result.IsFailed, Is.True, "Z10:");
    }



    /// <summary>
    ///     Validates that the system creates the valid storage path for a file.
    /// </summary>
    /// <param name="storageMode"></param>
    /// <returns></returns>
    [Test]
    [TestCase(EnumStorageMode.Replaceable)]
    [TestCase(EnumStorageMode.Editable)]
    [TestCase(EnumStorageMode.WriteOnceReadMany)]
    [TestCase(EnumStorageMode.Temporary)]
    [TestCase(EnumStorageMode.Versioned)]
    public async Task ComputeStorageFolder_CorrectPath_Generated(EnumStorageMode storageMode)
    {
        // A. Setup
        SupportMethods       sm         = new();
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
        // Need to get RootObject's Application ID.
        RootObject rootObject = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == 1);

        DocumentType randomDocType = new()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = storageNode.Id,
            RootObjectId         = rootObject.Id,
            ApplicationId        = rootObject.ApplicationId,
            StorageMode          = storageMode,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7)
        };
        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        // B.
        string         fileName = sm.Faker.Random.Word();
        DocumentType   docType  = randomDocType;
        Result<string> result   = await dse.ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id);

        Assert.That(result.IsSuccess, Is.True, "B10: " + result);


        // Z. Validate - Now verify string is correct.
        string modeLetter = storageMode switch
        {
            EnumStorageMode.WriteOnceReadMany => "W",
            EnumStorageMode.Editable          => "E",
            EnumStorageMode.Temporary         => "T",
            EnumStorageMode.Replaceable       => "R",
            EnumStorageMode.Versioned         => "V",
            _                                 => throw new Exception("Unknown StorageMode value of [ " + storageMode + " ]")
        };


        // Paths should be:  NodePath\ModeLetter\DocTypeFolder\ymd\filename
        string ymdPath = DatePath(storageMode, randomDocType.InActiveLifeTime);
        string expected = Path.Combine(storageNode.NodePath,
                                       modeLetter,
                                       randomDocType.StorageFolderName,
                                       ymdPath);
        Assert.That(result.Value, Is.EqualTo(expected), "Z10:");
    }



    /// <summary>
    ///     Confirms we can upload a document to the DocumentServer
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        int                  expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);
        string               expectedRootObjectId = sm.Faker.Random.String2(10);
        string?              expectedExternalId   = null;


        // TODO - for test only.  DElete please now!
//        sm.DB.ChangeTracker.Clear();

        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    expectedDescription,
                                                                                    expectedExtension,
                                                                                    expectedDocTypeId,
                                                                                    expectedRootObjectId,
                                                                                    expectedExternalId);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;


        // Y.  CRITICAL ITEM:  Storage Path - This should be considered a critical test.  If this fails after initial deployment to production
        //     you need to carefully consider why it failed.  
        // Calculate the full storage node path that the file should have been written at.
        DocumentType   a          = await sm.DB.DocumentTypes.SingleAsync(d => d.Id == expectedDocTypeId);
        StorageNode    b          = await sm.DB.StorageNodes.SingleAsync(s => s.Id == a.ActiveStorageNode1Id);
        Result<string> modeResult = documentServerEngine.GetModeLetter(a.StorageMode);
        Assert.That(modeResult.IsSuccess, Is.True, "Y10:");
        string modeLetter = modeResult.Value;

        string expectedPath = Path.Join(b.NodePath,
                                        a.StorageFolderName,
                                        modeLetter,
                                        DatePath(a.StorageMode, a.InActiveLifeTime));

        // Z. Validate
        // Z.10 - Validate the Database entry

        //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.FileName, Does.EndWith(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(expectedDocTypeId), "Z20:");
        Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30");
        Assert.That(storedDocument.StorageFolder, Is.EqualTo(expectedPath), "Z40:");

        // Make sure it was stored on the drive.
        string fullFileName = Path.Join(expectedPath, storedDocument.FileName);
        Assert.That(sm.FileSystem.FileExists(fullFileName), Is.True, "Z90");
    }



    /// <summary>
    ///     Validates we can read a document from the library
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ReadDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        int    expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string expectedExtension    = sm.Faker.Random.String2(3);
        string expectedDescription  = sm.Faker.Random.String2(32);
        string expectedRootObjectId = sm.Faker.Random.String2(10);
        string expectedExternalId   = sm.Faker.Random.String2(15);
        int    sizeInKB             = 3;

        // A.  Generate File and store it
        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    expectedDescription,
                                                                                    expectedExtension,
                                                                                    expectedDocTypeId,
                                                                                    expectedRootObjectId,
                                                                                    expectedExternalId);

        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;

        // B. Now lets read it.
        Result<TransferDocumentContainer> readResult = await documentServerEngine.GetStoredDocumentAsync(storedDocument.Id);
        TransferDocumentContainer         tdc        = readResult.Value;
        TransferDocumentDto               tdd        = readResult.Value.TransferDocument;
        if (!tdc.IsInFormFileMode)
        {
            Stream stream2 = genFileResult.Value.FileInFormFile.OpenReadStream();
            byte[] buffer2 = new byte[stream2.Length];
            stream2.ReadExactly(buffer2, 0, (int)stream2.Length);

            Assert.That(tdc.FileInBytes, Is.EqualTo(buffer2), "Z10A:");
        }
        else
        {
            Stream stream = tdc.FileInFormFile.OpenReadStream();
            Byte[] buffer = new Byte[stream.Length];
            stream.ReadExactly(buffer, 0, (int)stream.Length);

            Stream stream2 = genFileResult.Value.FileInFormFile.OpenReadStream();
            byte[] buffer2 = new byte[stream2.Length];
            stream.ReadExactly(buffer2, 0, (int)stream2.Length);
            Assert.That(buffer, Is.EqualTo(buffer2), "Z10B:");
        }

        // Validate other TransferDocument Info
        Assert.That(tdd.CurrentStoredDocumentId, Is.Not.EqualTo(0), "Z20:");
        Assert.That(tdd.Description, Is.EqualTo(expectedDescription), "Z30:");
        Assert.That(tdd.DocTypeExternalId, Is.EqualTo(expectedExternalId), "Z40:");
        Assert.That(tdd.DocumentTypeId, Is.EqualTo(expectedDocTypeId), "Z50:");
        Assert.That(tdd.RootObjectId, Is.EqualTo(expectedRootObjectId), "Z60:");
    }



    [Test]
    public async Task TemporaryDocumentsSaveInTempFolder() { }



    /// <summary>
    ///     Validates that temporary documents:
    ///     - Have an expiringDocuments entry
    ///     - That the entry is correct
    ///     - That the StoredDocuments IsAlive flag is set to False
    ///     - That the document is stored in the Tempoary folder space
    /// </summary>
    /// <returns></returns>
    [Test]
    [TestCase(EnumDocumentLifetimes.Never)]
    [TestCase(EnumDocumentLifetimes.HoursOne)]
    [TestCase(EnumDocumentLifetimes.HoursTwelve)]
    [TestCase(EnumDocumentLifetimes.HoursFour)]
    [TestCase(EnumDocumentLifetimes.DayOne)]
    [TestCase(EnumDocumentLifetimes.MonthOne)]
    [TestCase(EnumDocumentLifetimes.MonthsThree)]
    [TestCase(EnumDocumentLifetimes.MonthsSix)]
    [TestCase(EnumDocumentLifetimes.WeekOne)]
    [TestCase(EnumDocumentLifetimes.YearOne)]
    [TestCase(EnumDocumentLifetimes.YearsTwo)]
    [TestCase(EnumDocumentLifetimes.YearsThree)]
    [TestCase(EnumDocumentLifetimes.YearsFour)]
    [TestCase(EnumDocumentLifetimes.YearsSeven)]
    [TestCase(EnumDocumentLifetimes.YearsTen)]
    public async Task SaveTemporaryDocument(EnumDocumentLifetimes lifeTime)
    {
        //***  A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        string  expectedExtension    = sm.Faker.Random.String2(3);
        string  expectedDescription  = sm.Faker.Random.String2(32);
        string  expectedRootObjectId = sm.Faker.Random.String2(10);
        string? expectedExternalId   = null;


        //***  B. Create a random DocumentType.  These document types all will have a custom folder name
        // Need to get RootObject's Application ID.
        RootObject rootObject = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == 1);

        DocumentType randomDocType = new()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = 1,
            RootObjectId         = rootObject.Id,
            ApplicationId        = rootObject.ApplicationId,
            StorageMode          = EnumStorageMode.Temporary,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7),
            InActiveLifeTime     = lifeTime,
            IsActive             = true
        };
        Result r1 = randomDocType.IsValid();
        Console.WriteLine(r1.ToString());
        if (r1.IsFailed)
            throw new ApplicationException(r1.Errors.ToString());

        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        //***  C.  Generate File and store it
        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    expectedDescription,
                                                                                    expectedExtension,
                                                                                    randomDocType.Id,
                                                                                    expectedRootObjectId,
                                                                                    expectedExternalId);

        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;


        //***  W.  Validate document is stored in temporary folder path
        StorageNode storageNode = await sm.DB.StorageNodes.SingleOrDefaultAsync(s => s.Id == randomDocType.ActiveStorageNode1Id);
        Assert.That(storageNode, Is.Not.Null, "W10:");
        string expectedBeginPath = Path.Join(storageNode.NodePath, "T");
        Assert.That(storedDocument.StorageFolder.StartsWith(expectedBeginPath), Is.True, "W20:");

        // Verify it is actually stored where it is supposed to be.
        DateTime currentUtc = DateTime.UtcNow;
        string fileName = Path.Join(expectedBeginPath,
                                    randomDocType.StorageFolderName,
                                    DatePath(randomDocType.StorageMode, randomDocType.InActiveLifeTime),
                                    storedDocument.FileName);
        Assert.That(sm.FileSystem.File.Exists(fileName), "W30: Could not find file in filesystem");


        //***  Y. Validate
        Assert.That(storedDocument.IsAlive, Is.False, "Y10:");

        // Read the ExpiredDocument
        ExpiringDocument expired = await sm.DB.ExpiringDocuments.SingleOrDefaultAsync(e => e.StoredDocumentId == storedDocument.Id);
        Assert.That(expired, Is.Not.Null, "Y20:");

        //***  Z. Datetime Checks
        DateTime min = DateTime.MinValue;
        DateTime max = DateTime.MaxValue;

        bool wasAsserted = true;

        switch (lifeTime)
        {
            case EnumDocumentLifetimes.Never:
                Assert.That(expired.ExpirationDateUtcDateTime.IsInRange(min, max), "Z10:");

                break;
            case EnumDocumentLifetimes.HoursOne:
                min = DateTime.UtcNow.AddMinutes(59);
                max = DateTime.UtcNow.AddHours(1);
                break;
            case EnumDocumentLifetimes.HoursFour:
                min = DateTime.UtcNow.AddHours(3).AddMinutes(59);
                max = DateTime.UtcNow.AddHours(4);
                break;
            case EnumDocumentLifetimes.HoursTwelve:
                min = DateTime.UtcNow.AddHours(11).AddMinutes(59);
                max = DateTime.UtcNow.AddHours(12);
                break;

            case EnumDocumentLifetimes.DayOne:
                min = DateTime.UtcNow.AddDays(1).AddMinutes(-1);
                max = DateTime.UtcNow.AddDays(1);
                break;

            case EnumDocumentLifetimes.WeekOne:
                min = DateTime.UtcNow.AddDays(7).AddMinutes(-1);
                max = DateTime.UtcNow.AddDays(7);
                break;

            case EnumDocumentLifetimes.MonthOne:
                min = DateTime.UtcNow.AddDays(31).AddMinutes(-1);
                max = DateTime.UtcNow.AddDays(31);
                break;

            case EnumDocumentLifetimes.MonthsThree:
                min = DateTime.UtcNow.AddDays(91).AddMinutes(-1);
                max = DateTime.UtcNow.AddDays(91);
                break;

            case EnumDocumentLifetimes.MonthsSix:
                min = DateTime.UtcNow.AddDays(183).AddMinutes(-1);
                max = DateTime.UtcNow.AddDays(183);
                break;

            case EnumDocumentLifetimes.YearOne:
                min = DateTime.UtcNow.AddYears(1).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(1);
                break;

            case EnumDocumentLifetimes.YearsTwo:
                min = DateTime.UtcNow.AddYears(2).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(2);
                break;

            case EnumDocumentLifetimes.YearsThree:
                min = DateTime.UtcNow.AddYears(3).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(3);
                break;

            case EnumDocumentLifetimes.YearsFour:
                min = DateTime.UtcNow.AddYears(4).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(4);
                break;

            case EnumDocumentLifetimes.YearsSeven:
                min = DateTime.UtcNow.AddYears(7).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(7);
                break;

            case EnumDocumentLifetimes.YearsTen:
                min = DateTime.UtcNow.AddYears(10).AddMinutes(-1);
                max = DateTime.UtcNow.AddYears(10);
                break;

            case EnumDocumentLifetimes.ParentDetermined:
                // TODO fix this, but how?
                min = DateTime.UtcNow.AddHours(3).AddMinutes(59);
                max = DateTime.UtcNow.AddHours(4);
                break;
            default:
                wasAsserted = false;
                break;
        }


        Assert.That(wasAsserted, Is.True, "Z10: The Lifetime check never entered a valid Case statement.  Missing a case value?");
        Console.WriteLine("Asserted that the document with lifetime setting: {0} was in expected range ", lifeTime.ToString());
    }



    /// <summary>
    ///     Internal function to match the calculated date portion of a stored files path.
    /// </summary>
    /// <returns></returns>
    private string DatePath(EnumStorageMode storageMode,
                            EnumDocumentLifetimes inActiveLifeTime)
    {
        DateTime folderDatetime = DateTime.UtcNow;
        if (storageMode == EnumStorageMode.Temporary)
            folderDatetime = inActiveLifeTime switch
            {
                EnumDocumentLifetimes.HoursOne    => folderDatetime.AddHours(1),
                EnumDocumentLifetimes.HoursFour   => folderDatetime.AddHours(4),
                EnumDocumentLifetimes.HoursTwelve => folderDatetime.AddHours(12),
                EnumDocumentLifetimes.DayOne      => folderDatetime.AddDays(1),
                EnumDocumentLifetimes.MonthOne    => folderDatetime.AddMonths(1),
                EnumDocumentLifetimes.MonthsThree => folderDatetime.AddMonths(3),
                EnumDocumentLifetimes.MonthsSix   => folderDatetime.AddMonths(6),
                EnumDocumentLifetimes.WeekOne     => folderDatetime.AddDays(7),
                EnumDocumentLifetimes.YearOne     => folderDatetime.AddYears(1),
                EnumDocumentLifetimes.YearsTwo    => folderDatetime.AddYears(2),
                EnumDocumentLifetimes.YearsThree  => folderDatetime.AddYears(3),
                EnumDocumentLifetimes.YearsFour   => folderDatetime.AddYears(4),
                EnumDocumentLifetimes.YearsSeven  => folderDatetime.AddYears(7),
                EnumDocumentLifetimes.YearsTen    => folderDatetime.AddYears(10),
                EnumDocumentLifetimes.Never       => DateTime.MaxValue,
                _                                 => throw new Exception("Unknown DocumentLifetime value of [ " + inActiveLifeTime + " ]")
            };

        string year  = folderDatetime.ToString("yyyy");
        string month = folderDatetime.ToString("MM");

        //string   day        = currentUtc.ToString("dd");
        return Path.Combine(year, month);
    }


    [Test]
    [TestCase(EnumStorageMode.Versioned)]
    [TestCase(EnumStorageMode.Replaceable)]
    [TestCase(EnumStorageMode.Editable)]
    [TestCase(EnumStorageMode.Temporary)]
    [TestCase(EnumStorageMode.WriteOnceReadMany)]
    public void ModeLetter_Is_Correct(EnumStorageMode storageMode)
    {
        //***  A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;


        // Z. Validate - Now verify string is correct.
        string expectedModeLetter = storageMode switch
        {
            EnumStorageMode.WriteOnceReadMany => "W",
            EnumStorageMode.Editable          => "E",
            EnumStorageMode.Temporary         => "T",
            EnumStorageMode.Versioned         => "V",
            EnumStorageMode.Replaceable       => "R",
            _                                 => throw new Exception("Unknown StorageMode value of [ " + storageMode + " ]")
        };

        Result<string> actualResult = documentServerEngine.GetModeLetter(storageMode);
        Assert.That(actualResult.IsSuccess, Is.True, "Z10:");
        Assert.That(actualResult.Value, Is.EqualTo(expectedModeLetter), "Z20: Invalid mode letter returned");
    }


    [Test]
    public async Task StoreFileOnStorageMedia_Success()
    {
        //***  A. Setup
        SupportMethods       sm                    = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine  = sm.DocumentServerEngine;
        string               expectedExtension     = sm.Faker.Random.String2(3);
        string               expectedDescription   = sm.Faker.Random.String2(32);
        string               expectedStorageFolder = sm.Faker.Random.String2(6);
        string               expectedRootObjectId  = sm.Faker.Random.String2(9);
        string               exptectedDocTypeExtId = sm.Faker.Random.String2(5);
        byte[]               fileBytes             = sm.Faker.Random.Bytes(20);


        // Load a Document Type
        DocumentType documentType = await sm.DB.DocumentTypes.SingleOrDefaultAsync(dt => dt.Id == sm.DocumentType_Test_Worm_A);

        // Create a dummy StoredDocument
        StoredDocument storedDocument = new(expectedExtension,
                                            expectedDescription,
                                            storageFolder: expectedStorageFolder,
                                            rootObjectExternalKey: expectedRootObjectId,
                                            docTypeExternalKey: exptectedDocTypeExtId,
                                            sizeInKB: 0,
                                            documentTypeId: sm.DocumentType_Test_Worm_A,
                                            primaryStorageNodeId: sm.StorageNode_Test_A);

        FormFile formFile = sm.GetFormFile(fileBytes);

        //string   fileBytes64 = Convert.ToBase64String(fileBytes);
        Result<string> storeFileResult = await documentServerEngine.StoreFileOnStorageMediaAsync(storedDocument,
                                                                                                 documentType,
                                                                                                 sm.StorageNode_Test_A,
                                                                                                 formFile);

        //***  Z. Validate
        Assert.That(storeFileResult.IsSuccess, Is.True, "Z10: StoreFileResult returned false");

        // Make sure it was stored on the drive.
        string fullFileName = Path.Join(storedDocument.StorageFolder, storedDocument.FileName);
        Assert.That(sm.FileSystem.FileExists(fullFileName), Is.True, "Z90");
    }


    // If a Bad Storage Node is used, then the file can not be saved.
    [Test]
    public async Task StoreFileOnStorageMedia_BadStorageNode_Failure()
    {
        //***  A. Setup
        SupportMethods       sm                    = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine  = sm.DocumentServerEngine;
        string               expectedExtension     = sm.Faker.Random.String2(3);
        string               expectedDescription   = sm.Faker.Random.String2(32);
        string               expectedStorageFolder = sm.Faker.Random.String2(6);
        string               expectedRootObjectId  = sm.Faker.Random.String2(9);
        string               exptectedDocTypeExtId = sm.Faker.Random.String2(5);

        byte[] fileBytes = sm.Faker.Random.Bytes(20);


        // Load a Document Type
        DocumentType documentType = await sm.DB.DocumentTypes.SingleOrDefaultAsync(dt => dt.Id == sm.DocumentType_Test_Worm_A);

        // Create a dummy StoredDocument
        int badStorageNode = 999999;

        StoredDocument storedDocument = new(expectedExtension,
                                            expectedDescription,
                                            storageFolder: expectedStorageFolder,
                                            rootObjectExternalKey: expectedRootObjectId,
                                            docTypeExternalKey: exptectedDocTypeExtId,
                                            sizeInKB: 0,
                                            documentTypeId: sm.DocumentType_Test_Worm_A,
                                            primaryStorageNodeId: badStorageNode);

        //string   fileBytes64 = Convert.ToBase64String(fileBytes);
        FormFile formFile = sm.GetFormFile(fileBytes);
        Result<string> storeFileResult = await documentServerEngine.StoreFileOnStorageMediaAsync(storedDocument,
                                                                                                 documentType,
                                                                                                 badStorageNode,
                                                                                                 formFile);

        //***  Z. Validate
        Assert.That(storeFileResult.IsFailed, Is.True, "Z10: StoreFileResult should have been false");
    }


    // Confirms that a Replacement Document is successfully saved and the old document is deleted.
    [Test]
    public async Task ReplacementFileStorage_Success()
    {
        //***  A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        int                  expectedDocTypeId    = sm.DocumentType_Prod_Replaceable_A;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);
        string               expectedRootObjectId = sm.Faker.Random.String2(10);
        string?              expectedExternalId   = null;


        //***  B. Generate a file and store it
        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    expectedDescription,
                                                                                    expectedExtension,
                                                                                    expectedDocTypeId,
                                                                                    expectedRootObjectId,
                                                                                    expectedExternalId);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;

        string originalStoredFileName = storedDocument.FileNameAndPath;
        long   originalId             = storedDocument.Id;
        string originalFileName       = storedDocument.FileName;

        //***  C.  Confirm the file is stored.

        Assert.That(sm.FileSystem.File.Exists(originalStoredFileName), Is.True, "C10:  Original Stored File could not be found");
        Assert.That(sm.FileSystem.AllFiles.Count(), Is.EqualTo(2), "C20:");


        //***  D.  Generate and store a replacement file
        string replaceDescription = "Replaced Description";
        Result<TransferDocumentContainer> replacementDtoResult = sm.TFX_GenerateUploadFile(sm,
                                                                                           replaceDescription,
                                                                                           expectedExtension,
                                                                                           expectedDocTypeId,
                                                                                           expectedRootObjectId,
                                                                                           expectedExternalId);
        TransferDocumentContainer replacement = replacementDtoResult.Value;
        replacement.TransferDocument = new()
        {
            Description             = replaceDescription,
            FileExtension           = expectedExtension,
            CurrentStoredDocumentId = storedDocument.Id,
        };

        Result<StoredDocument> replaceResult       = await documentServerEngine.StoreReplacementDocumentAsync(replacement);
        StoredDocument         replacementDocument = replaceResult.Value;

        // Z. Validate

        //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(replacementDocument.FileName, Does.EndWith(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(replacementDocument.DocumentType.Id, Is.EqualTo(expectedDocTypeId), "Z20:");
        Assert.That(replacementDocument.Description, Is.EqualTo(replaceDescription), "Z30");

        // Make sure the StoredDocument and ReplacementDocument have different FileNames, but same Id
        Assert.That(replacementDocument.Id, Is.EqualTo(originalId), "Z40:");
        Assert.That(replacementDocument.FileName, Is.Not.EqualTo(originalFileName), "Z50:");

        // Make sure the replacement doc was stored on the drive.
        Assert.That(sm.FileSystem.FileExists(replacementDocument.FileNameAndPath), Is.True, "Z80:");

        // Make sure the original file is deleted.
        Assert.That(sm.FileSystem.File.Exists(originalStoredFileName), Is.False, "Z90:");

        // 2 generated files and the one new stored file.
        if (sm.FileSystem.AllFiles.Count() == 2)
        {
            int j = 3;
            j++;
        }

        Assert.That(sm.FileSystem.AllFiles.Count(), Is.EqualTo(3), "Z91:");
    }



    /// <summary>
    ///     Confirms we can store a document that has an ExternalDocumentId
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_HasExternalDocTypeKey_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        int                  expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);
        string               expectedRootObjectId = sm.Faker.Random.String2(10);
        string?              expectedExternalId   = sm.Faker.Random.String2(8);


        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    expectedDescription,
                                                                                    expectedExtension,
                                                                                    expectedDocTypeId,
                                                                                    expectedRootObjectId,
                                                                                    expectedExternalId);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;


        // Z. Validate
        // Z.10 - Validate the Database entry

        //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.FileName, Does.EndWith(expectedExtension), "Z10: File Extensions do not match");
        Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(expectedDocTypeId), "Z20:");
        Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30");
        Assert.That(storedDocument.RootObjectExternalKey, Is.EqualTo(expectedRootObjectId), "Z40:");
        Assert.That(storedDocument.DocTypeExternalKey, Is.EqualTo(expectedExternalId), "Z50:");
    }



    /// <summary>
    ///     Confirms we can store a document that has an ExternalDocumentId that is the same across multiple StoredDocuments
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_HasExternalDocTypeKey_MultipleEntries_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);
        string               expectedRootObjectId = sm.Faker.Random.String2(10);
        string?              expectedExternalId   = sm.Faker.Random.String2(8);


        //***  B. Create a random DocumentType.  These document types all will have a custom folder name
        // Need to get RootObject's Application ID.
        RootObject rootObject = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == 1);

        DocumentType randomDocType = new()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = 1,
            RootObjectId         = rootObject.Id,
            ApplicationId        = rootObject.ApplicationId,
            StorageMode          = EnumStorageMode.Temporary,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7),
            InActiveLifeTime     = EnumDocumentLifetimes.DayOne,
            IsActive             = true,
            AllowSameDTEKeys     = true
        };
        Result r1 = randomDocType.IsValid();
        Console.WriteLine(r1.ToString());
        if (r1.IsFailed)
            throw new ApplicationException(r1.Errors.ToString());

        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();

        for (int i = 0; i < 4; i++)
        {
            Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                        expectedDescription,
                                                                                        expectedExtension,
                                                                                        randomDocType.Id,
                                                                                        expectedRootObjectId,
                                                                                        expectedExternalId);
            Result<StoredDocument> result = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
            Assert.That(result.IsSuccess, Is.True, "Z05:");
            StoredDocument storedDocument = result.Value;


            // Z. Validate
            // Z.10 - Validate the Database entry

            //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
            Assert.That(storedDocument.FileName, Does.EndWith(expectedExtension), "Z10: Loop [ " + i + " ] File Extensions do not match");
            Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(randomDocType.Id), "Z20: Loop [ " + i + " ]");
            Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30Loop [ " + i + " ] ");
            Assert.That(storedDocument.RootObjectExternalKey, Is.EqualTo(expectedRootObjectId), "Z40:Loop [ " + i + " ] ");
            Assert.That(storedDocument.DocTypeExternalKey, Is.EqualTo(expectedExternalId), "Z50:Loop [ " + i + " ] ");
        }
    }



    /// <summary>
    ///     Confirms we can only store a single document With an External DocumentTypeId that is the same for a given DocType
    ///     We run thru the test cycle 2 times.  The first time we should be able to save the document.  The 2nd we should
    ///     get an error.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_HasExternalDocTypeKey_AllowsSingleEntries_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);
        string               expectedRootObjectId = sm.Faker.Random.String2(10);
        string?              expectedExternalId   = sm.Faker.Random.String2(8);

        // Need to get RootObject's Application ID.
        RootObject rootObject = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == 1);

        //***  B. Create a random DocumentType.  These document types all will have a custom folder name
        DocumentType randomDocType = new()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = 1,
            RootObjectId         = rootObject.Id,
            ApplicationId        = rootObject.ApplicationId,
            StorageMode          = EnumStorageMode.Temporary,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7),
            InActiveLifeTime     = EnumDocumentLifetimes.DayOne,
            IsActive             = true,
            AllowSameDTEKeys     = false
        };
        Result r1 = randomDocType.IsValid();
        Console.WriteLine(r1.ToString());
        if (r1.IsFailed)
            throw new ApplicationException(r1.Errors.ToString());

        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();

        for (int i = 0; i < 2; i++)
        {
            //***  Y
            Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                        expectedDescription,
                                                                                        expectedExtension,
                                                                                        randomDocType.Id,
                                                                                        expectedRootObjectId,
                                                                                        expectedExternalId);
            Result<StoredDocument> result = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);

            // First pass we let go thru, 2nd we should get a failure.
            if (i > 0)
            {
                Assert.That(result.IsFailed, Is.True, "Y10:");
                string searchMsg = "Duplicate Key not allowed.";
                Assert.That(result.ToString().Contains(searchMsg), "Y20:");
                return;
            }

            Assert.That(result.IsSuccess, Is.True, "Y30:");
            StoredDocument storedDocument = result.Value;


            // Z. Validate
            // Z.10 - Validate the Database entry

            //Assert.That(storedDocument.FileExtension, Is.EqualTo(expectedExtension), "Z10: File Extensions do not match");
            Assert.That(storedDocument.FileName, Does.EndWith(expectedExtension), "Z10: Loop [ " + i + " ] File Extensions do not match");
            Assert.That(storedDocument.DocumentType.Id, Is.EqualTo(randomDocType.Id), "Z20: Loop [ " + i + " ]");
            Assert.That(storedDocument.Description, Is.EqualTo(expectedDescription), "Z30Loop [ " + i + " ] ");
            Assert.That(storedDocument.RootObjectExternalKey, Is.EqualTo(expectedRootObjectId), "Z40:Loop [ " + i + " ] ");
            Assert.That(storedDocument.DocTypeExternalKey, Is.EqualTo(expectedExternalId), "Z50:Loop [ " + i + " ] ");
        }
    }
}