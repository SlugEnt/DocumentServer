using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

[TestFixture]
public class Test_DocumentType
{
    [OneTimeSetUp]
    public void Setup() { }



    /// <summary>
    ///     Validate that the ComputeStorageFolder method throws if it cannot find the StorageNode
    /// </summary>
    /// <returns></returns>
    [Test]
    [TestCase("abc", true)]
    [TestCase("abc xyz", false)]
    [TestCase("a1", true)]
    [TestCase("ab1 xy", false)]
    [TestCase("0123456789AB", false)]
    [TestCase("abc$", false)]
    [TestCase("ab_cd", false)]
    public void DocumentType_StorageFolder_Validation(string folderName,
                                                      bool shouldPass)
    {
        //***  A. Setup
        SupportMethods       sm    = new();
        DocumentServerEngine dse   = sm.DocumentServerEngine;
        string               eName = sm.Faker.Random.Word();

        //***  Y. Create DocumentType
        Result<DocumentType> docResult = DocumentType.CreateDocumentType(eName,
                                                                         sm.Faker.Random.Words(4),
                                                                         folderName,
                                                                         EnumStorageMode.WriteOnceReadMany,
                                                                         1,
                                                                         1,
                                                                         1);
        if (shouldPass)
        {
            Assert.That(docResult.IsSuccess, Is.True, "Z10");
            Assert.That(docResult.Value, Is.Not.Null, "Z20:");
            Assert.That(docResult.Value.Name, Is.EqualTo(eName), "Z30:");
        }
        else
        {
            Assert.That(docResult.IsFailed, Is.True, "Z50");

            Assert.That(docResult.Errors.Count, Is.GreaterThan(0));
        }
    }



    /// <summary>
    ///     Validates that a document cannot be stored that has a DocumentType that is inactive.
    /// </summary>
    /// <returns></returns>
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task DocumentTypeMustBeActive_ToStoreADocument(bool markDocumentTypeActive)
    {
        //***  A. Setup
        SupportMethods       sm         = new(EnumFolderCreation.Test, false);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        string               folderName = sm.Faker.Random.AlphaNumeric(6);

        string  expectedExtension    = sm.Faker.Random.String2(3);
        string  expectedDescription  = sm.Faker.Random.String2(32);
        string  expectedRootObjectId = sm.Faker.Random.String2(10);
        string? expectedExternalId   = null;


        //***  B. Createa A new Document Type.
        Result<DocumentType> docResult = DocumentType.CreateDocumentType(sm.Faker.Random.Word(),
                                                                         sm.Faker.Random.Words(4),
                                                                         folderName,
                                                                         EnumStorageMode.WriteOnceReadMany,
                                                                         1,
                                                                         1,
                                                                         1);
        Assert.That(docResult.IsSuccess, Is.True, "B10");
        Assert.That(docResult.Value, Is.Not.Null, "B20:");
        DocumentType documentType = docResult.Value;

        documentType.IsActive = markDocumentTypeActive;
        Assert.That(documentType.IsActive, Is.EqualTo(markDocumentTypeActive), "B20:");
        sm.DB.DocumentTypes.Add(documentType);
        Console.WriteLine("Saving Step 1..." + DateTime.Now);
#if DEBUG
        Console.WriteLine("DB Context ID is {0}", sm.DB.ContextId);

        // Displays the Change Tracker Cache
        Console.WriteLine("Displaying ChangeTracker Cache");
        foreach (EntityEntry entityEntry in sm.DB.ChangeTracker.Entries())
            Console.WriteLine($"Found {entityEntry.Metadata.Name} entity with ID {entityEntry.Property("Id").CurrentValue}");
#endif
        await sm.DB.SaveChangesAsync();
        Console.WriteLine("Saved Step 1!");

        //***  C. Create a document
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              expectedDescription,
                                                                              expectedExtension,
                                                                              documentType.Id,
                                                                              expectedRootObjectId,
                                                                              expectedExternalId);
        Result<StoredDocument> result = await dse.StoreDocumentFirstTimeAsync(genFileResult.Value);


        //***  Z. Validate
        Console.WriteLine("1st Line" + Environment.NewLine + "2nd Line" + Environment.NewLine + "3rd Line");

        Console.WriteLine(result.ToStringWithLineFeeds());

        Assert.That(result.IsSuccess, Is.EqualTo(markDocumentTypeActive), "Z10: " + result.ToStringWithLineFeeds());

        // If we marked the document type active (true) then nothing else to test.
        if (markDocumentTypeActive)
            return;

        Assert.That(result.Errors.Count, Is.GreaterThan(0), "Z20:");

        bool foundMsg = false;
        foreach (IError? error in result.Errors)
        {
            if (error.Message.Contains("DocumentType") && error.Message.Contains("inactive"))
                foundMsg = true;
        }

        Assert.That(foundMsg, Is.False, "Z30:");
    }



    /// <summary>
    ///     Runs thru the DocumentType IsAlive change process.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SetDocumentTypeIsActiveStatus_Success()
    {
        //***  A. Setup
        SupportMethods       sm                  = new();
        DocumentServerEngine dse                 = sm.DocumentServerEngine;
        string               folderName          = sm.Faker.Random.AlphaNumeric(6);
        string               expectedExtension   = sm.Faker.Random.String2(3);
        string               expectedDescription = sm.Faker.Random.String2(32);


        //***  B. Createa A new Document Type.
        Result<DocumentType> docResult = DocumentType.CreateDocumentType(sm.Faker.Random.Word(),
                                                                         sm.Faker.Random.Words(4),
                                                                         folderName,
                                                                         EnumStorageMode.WriteOnceReadMany,
                                                                         1,
                                                                         1,
                                                                         1);
        Assert.That(docResult.IsSuccess, Is.True, "B10");
        Assert.That(docResult.Value, Is.Not.Null, "B20:");
        DocumentType documentType = docResult.Value;

        Assert.That(documentType.IsActive, Is.False, "B10:  Document Type ActiveStatus should always be False upon creation");
        sm.DB.Add(documentType);
        await sm.DB.SaveChangesAsync();

        // Change the Status to true.
        documentType.IsActive = true;
        await sm.DB.SaveChangesAsync();

        Assert.That(documentType.IsActive, Is.True, "Z10:");

        // Change the Status to false
        documentType.IsActive = false;
        await sm.DB.SaveChangesAsync();
        Assert.That(documentType.IsActive, Is.False, "Z20:");
    }



    /// <summary>
    ///     Validates the default settings of a DocumentType Object are correct.  If these ever fail, very carefully consider
    ///     the
    ///     changes you made, it could affect existing data.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task Validate_DocumentTypeDefaultValues()
    {
        //***  A. Setup
        SupportMethods       sm                  = new();
        DocumentServerEngine dse                 = sm.DocumentServerEngine;
        string               folderName          = sm.Faker.Random.AlphaNumeric(6);
        string               expectedExtension   = sm.Faker.Random.String2(3);
        string               expectedDescription = sm.Faker.Random.String2(32);


        //***  B. Createa A new Document Type.
        Result<DocumentType> docResult = DocumentType.CreateDocumentType(sm.Faker.Random.Word(),
                                                                         sm.Faker.Random.Words(4),
                                                                         folderName,
                                                                         EnumStorageMode.WriteOnceReadMany,
                                                                         1,
                                                                         1,
                                                                         1);
        Assert.That(docResult.IsSuccess, Is.True, "B10");
        Assert.That(docResult.Value, Is.Not.Null, "B20:");
        DocumentType documentType = docResult.Value;
        sm.DB.Add(documentType);
        await sm.DB.SaveChangesAsync();


        //***  Z.  Validate
        Assert.That(documentType.IsActive, Is.False, "Z10:  Document Type ActiveStatus should always be False upon creation");
        Assert.That(documentType.ActiveStorageNode2Id, Is.Null, "Z20:");
        Assert.That(documentType.ArchivalStorageNode1Id, Is.Null, "Z20:");
        Assert.That(documentType.ArchivalStorageNode2Id, Is.Null, "Z30:");
        Assert.That(documentType.InActiveLifeTime, Is.EqualTo(EnumDocumentLifetimes.Never), "Z40:");
    }



    /// <summary>
    ///     Confirms that WORM fields are unable to be updated after initial save.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task Ignore_Worm_Fields_Success()
    {
        // A. Setup
        SupportMethods        sm             = new(EnumFolderCreation.None, false);
        DocumentServerEngine  dse            = sm.DocumentServerEngine;
        string                sName          = sm.Faker.Random.String2(12);
        string                sDesc          = sm.Faker.Random.String2(22);
        int                   sNodes         = 1;
        bool                  sSameDTE       = false;
        int                   sRootObjId     = 1;
        string                sStorageFolder = sm.Faker.Random.String2(5);
        EnumStorageMode       sStorageMode   = EnumStorageMode.Versioned;
        EnumDocumentLifetimes sDocLifetime   = EnumDocumentLifetimes.HoursTwelve;


        // B. Createa A new Document Type.
        DocumentType docType = new()
        {
            Name                   = sName,
            Description            = sDesc,
            ActiveStorageNode1Id   = sNodes,
            ActiveStorageNode2Id   = sNodes,
            AllowSameDTEKeys       = sSameDTE,
            ArchivalStorageNode1Id = sNodes,
            ArchivalStorageNode2Id = sNodes,
            InActiveLifeTime       = sDocLifetime,
            RootObjectId           = sRootObjId,
            StorageFolderName      = sStorageFolder,
            StorageMode            = sStorageMode
        };

        sm.DB.Add(docType);
        await sm.DB.SaveChangesAsync();
        int eId = docType.Id;
        sm.ResetContext();


        //***  C. Now make changes
        string                eName          = sm.Faker.Random.String2(12);
        string                eDesc          = sm.Faker.Random.String2(22);
        int                   eNodes         = 2;
        bool                  eSameDTE       = true;
        int                   eRootObjId     = 2;
        bool                  eIsActive      = false;
        string                eStorageFolder = sm.Faker.Random.String2(5);
        EnumDocumentLifetimes eDocLifetime   = EnumDocumentLifetimes.YearOne;
        EnumStorageMode       eStorageMode   = EnumStorageMode.Temporary;
        DocumentType          docType2       = await sm.DB.DocumentTypes.SingleOrDefaultAsync(dt => dt.Id == eId);

        docType2.Name                   = eName;
        docType2.Description            = eDesc;
        docType2.AllowSameDTEKeys       = eSameDTE;
        docType2.ArchivalStorageNode1Id = eNodes;
        docType2.ArchivalStorageNode2Id = eNodes;
        docType2.ActiveStorageNode1Id   = eNodes;
        docType2.ActiveStorageNode2Id   = eNodes;
        docType2.InActiveLifeTime       = eDocLifetime;
        docType2.RootObjectId           = eRootObjId;
        docType2.StorageFolderName      = eStorageFolder;
        docType2.StorageMode            = eStorageMode;
        docType2.IsActive               = eIsActive;
        await sm.DB.SaveChangesAsync();
        sm.ResetContext();

        DocumentType docType3 = await sm.DB.DocumentTypes.SingleOrDefaultAsync(dt => dt.Id == eId);


        // Z.  Validate
        Assert.That(docType3, Is.Not.Null, "Z10:");
        Assert.That(docType3.IsActive, Is.EqualTo(eIsActive), "Z20:");
        Assert.That(docType3.ActiveStorageNode2Id, Is.EqualTo(eNodes), "Z30:");
        Assert.That(docType3.ActiveStorageNode1Id, Is.EqualTo(eNodes), "Z31:");
        Assert.That(docType3.ArchivalStorageNode1Id, Is.EqualTo(eNodes), "Z32:");
        Assert.That(docType3.ArchivalStorageNode2Id, Is.EqualTo(eNodes), "Z33:");
        Assert.That(docType3.InActiveLifeTime, Is.EqualTo(eDocLifetime), "Z40:");
        Assert.That(docType3.Description, Is.EqualTo(eDesc), "Z50:");
        Assert.That(docType3.Name, Is.EqualTo(eName), "Z60:");
        Assert.That(docType3.StorageFolderName, Is.EqualTo(eStorageFolder), "Z70:");

        // These should hAVE remained same as original and not have been changed.
        Assert.That(docType3.RootObjectId, Is.EqualTo(sRootObjId), "Z100:");
        Assert.That(docType3.StorageMode, Is.EqualTo(sStorageMode), "Z120:");
        Assert.That(docType3.AllowSameDTEKeys, Is.EqualTo(sSameDTE));
    }
}