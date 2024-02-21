using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using DocumentServer_Test.SupportObjects;
using SlugEnt;
using SlugEnt.FluentResults;

namespace DocumentServer_Test;

[TestFixture]
public class Test_DocumentType
{
    [OneTimeSetUp]
    public void Setup() { }



    /// <summary>
    /// Validate that the ComputeStorageFolder method throws if it cannot find the StorageNode
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
    public async Task DocumentType_StorageFolder_Validation(string folderName,
                                                            bool shouldPass)
    {
        // A. Setup
        SupportMethods       sm  = new SupportMethods();
        DocumentServerEngine dse = sm.DocumentServerEngine;

        // B. Create DocumentType
        if (!shouldPass)
            Assert.Throws<ArgumentException>(() => new DocumentType(sm.Faker.Random.Word(),
                                                                    sm.Faker.Name.FullName(),
                                                                    folderName,
                                                                    EnumStorageMode.WriteOnceReadMany,
                                                                    1,
                                                                    1));
        else
        {
            Assert.That(() => new DocumentType(sm.Faker.Random.Word(),
                                               sm.Faker.Name.FullName(),
                                               folderName,
                                               EnumStorageMode.WriteOnceReadMany,
                                               1,
                                               1),
                        Is.Not.Null);
        }
    }



    /// <summary>
    /// Validates that a document cannot be stored that has a DocumentType that is inactive.
    /// </summary>
    /// <returns></returns>
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task DocumentTypeMustBeActive_ToStoreADocument(bool markDocumentTypeActive)
    {
        //***  A. Setup
        SupportMethods       sm         = new SupportMethods(EnumFolderCreation.Test, false);
        DocumentServerEngine dse        = sm.DocumentServerEngine;
        string               folderName = sm.Faker.Random.AlphaNumeric(6);

        string  expectedExtension    = sm.Faker.Random.String2(3);
        string  expectedDescription  = sm.Faker.Random.String2(32);
        string  expectedRootObjectId = sm.Faker.Random.String2(10);
        string? expectedExternalId   = null;


        //***  B. Createa A new Document Type.
        DocumentType documentType = new DocumentType(sm.Faker.Random.AlphaNumeric(7),
                                                     sm.Faker.Name.FullName(),
                                                     folderName,
                                                     EnumStorageMode.WriteOnceReadMany,
                                                     1,
                                                     1);
        documentType.IsActive = markDocumentTypeActive;
        Assert.That(documentType.IsActive, Is.EqualTo(markDocumentTypeActive), "B20:");
        sm.DB.DocumentTypes.Add(documentType);
        Console.WriteLine("Saving Step 1..." + DateTime.Now);
#if DEBUG
        Console.WriteLine("DB Context ID is {0}", sm.DB.ContextId);

        // Displays the Change Tracker Cache
        Console.WriteLine("Displaying ChangeTracker Cache");
        foreach (var entityEntry in sm.DB.ChangeTracker.Entries())
        {
            Console.WriteLine($"Found {entityEntry.Metadata.Name} entity with ID {entityEntry.Property("Id").CurrentValue}");
        }
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
        foreach (var error in result.Errors)
        {
            if (error.Message.Contains("DocumentType") && error.Message.Contains("inactive"))
                foundMsg = true;
        }

        Assert.That(foundMsg, Is.False, "Z30:");
    }



    /// <summary>
    /// Runs thru the DocumentType IsAlive change process.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task SetDocumentTypeIsActiveStatus_Success()
    {
        // A. Setup
        SupportMethods       sm                  = new SupportMethods(EnumFolderCreation.None);
        DocumentServerEngine dse                 = sm.DocumentServerEngine;
        string               folderName          = sm.Faker.Random.AlphaNumeric(6);
        string               expectedExtension   = sm.Faker.Random.String2(3);
        string               expectedDescription = sm.Faker.Random.String2(32);


        // B. Createa A new Document Type.
        DocumentType documentType = new DocumentType(sm.Faker.Random.AlphaNumeric(7),
                                                     sm.Faker.Name.FullName(),
                                                     folderName,
                                                     EnumStorageMode.WriteOnceReadMany,
                                                     1,
                                                     1);
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
    /// Validates the default settings of a DocumentType Object are correct.  If these ever fail, very carefully consider the
    /// changes you made, it could affect existing data.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task Validate_DocumentTypeDefaultValues()
    {
        // A. Setup
        SupportMethods       sm                  = new SupportMethods(EnumFolderCreation.None);
        DocumentServerEngine dse                 = sm.DocumentServerEngine;
        string               folderName          = sm.Faker.Random.AlphaNumeric(6);
        string               expectedExtension   = sm.Faker.Random.String2(3);
        string               expectedDescription = sm.Faker.Random.String2(32);


        // B. Createa A new Document Type.
        DocumentType documentType = new DocumentType(sm.Faker.Random.AlphaNumeric(7),
                                                     sm.Faker.Name.FullName(),
                                                     folderName,
                                                     EnumStorageMode.WriteOnceReadMany,
                                                     1,
                                                     1);
        sm.DB.Add(documentType);
        await sm.DB.SaveChangesAsync();


        // Z.  Validate
        Assert.That(documentType.IsActive, Is.False, "Z10:  Document Type ActiveStatus should always be False upon creation");
        Assert.That(documentType.ActiveStorageNode2Id, Is.Null, "Z20:");
        Assert.That(documentType.ArchivalStorageNode1Id, Is.Null, "Z20:");
        Assert.That(documentType.ArchivalStorageNode2Id, Is.Null, "Z30:");
        Assert.That(documentType.InActiveLifeTime, Is.EqualTo(EnumDocumentLifetimes.Never), "Z40:");
    }
}