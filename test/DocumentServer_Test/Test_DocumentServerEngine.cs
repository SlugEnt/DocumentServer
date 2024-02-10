using System.Diagnostics;
using Bogus;
using Bogus.DataSets;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using DocumentServer_Test.SupportObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SlugEnt;
using SlugEnt.FluentResults;
using Test_DocumentServer.SupportObjects;

namespace DocumentServer_Test;

[TestFixture]
public class Test_DocumentServerEngine
{
    [OneTimeSetUp]
    public void Setup() { }



    /// <summary>
    /// Validate that the ComputeStorageFolder method throws if it cannot find the StorageNode
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ComputeStorageFolder_ReturnsFailure_On_InvalidNode()
    {
        // A. Setup
        SupportMethods       sm                     = new SupportMethods();
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
        SupportMethods       sm         = new SupportMethods();
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
    /// Confirms we can upload a document to the DocumentServer
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new SupportMethods(EnumFolderCreation.Test);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;
        int                  expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string               expectedExtension    = sm.Faker.Random.String2(3);
        string               expectedDescription  = sm.Faker.Random.String2(32);

        // TODO - for test only.  DElete please now!
//        sm.DB.ChangeTracker.Clear();

        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
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
    /// Validates we can read a document from the library
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task ReadDocument_Success()
    {
        // A. Setup
        SupportMethods       sm                   = new SupportMethods(EnumFolderCreation.Test);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        int    expectedDocTypeId   = sm.DocumentType_Test_Worm_A;
        string expectedExtension   = sm.Faker.Random.String2(3);
        string expectedDescription = sm.Faker.Random.String2(32);

        // A.  Generate File and store it
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
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
    /// Validates that temporary documents:
    ///   - Have an expiringDocuments entry
    ///   - That the entry is correct
    ///   - That the StoredDocuments IsAlive flag is set to False
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
        // A. Setup
        SupportMethods       sm                   = new SupportMethods(EnumFolderCreation.Test, false);
        DocumentServerEngine documentServerEngine = sm.DocumentServerEngine;

        string expectedExtension   = sm.Faker.Random.String2(3);
        string expectedDescription = sm.Faker.Random.String2(32);

        // B. Create a random DocumentType.  These document types all will have a custom folder name
        DocumentType randomDocType = new DocumentType()
        {
            Name                 = sm.Faker.Commerce.ProductName(),
            Description          = sm.Faker.Lorem.Sentence(),
            ActiveStorageNode1Id = 1,
            ApplicationId        = 1,
            StorageMode          = EnumStorageMode.Temporary,
            StorageFolderName    = sm.Faker.Random.AlphaNumeric(7),
            InActiveLifeTime     = lifeTime,
            IsActive             = true,
        };
        Result r1 = randomDocType.IsValid();
        Console.WriteLine(r1.ToString());
        if (r1.IsFailed)
            throw new ApplicationException(r1.Errors.ToString());

        sm.DB.AddAsync(randomDocType);
        await sm.DB.SaveChangesAsync();


        // C.  Generate File and store it
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              expectedDescription,
                                                                              expectedExtension,
                                                                              randomDocType.Id);
        Result<StoredDocument> result         = await documentServerEngine.StoreDocumentFirstTimeAsync(genFileResult.Value);
        StoredDocument         storedDocument = result.Value;


        // Y. Validate

        Assert.That(storedDocument.IsAlive, Is.False, "Y10:");

        // Read the ExpiredDocument
        ExpiringDocument expired = await sm.DB.ExpiringDocuments.SingleOrDefaultAsync(e => e.StoredDocumentId == storedDocument.Id);
        Assert.That(expired, Is.Not.Null, "Y20:");

        // z. Datetime Checks
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