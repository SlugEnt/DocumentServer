using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using DocumentServer_Test.SupportObjects;
using SlugEnt;
using SlugEnt.FluentResults;

namespace Test_DocumentServer
{
    [TestFixture]
    public class Test_DocumentType
    {
        private DatabaseSetup_Test databaseSetupTest = new DatabaseSetup_Test();


        [SetUp]
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
            SupportMethods       sm  = new SupportMethods(databaseSetupTest);
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
        public async Task DocumentServerMustBeActive_ToStoreADocument(bool markDocumentTypeActive)
        {
            // A. Setup
            SupportMethods       sm         = new SupportMethods(databaseSetupTest, EnumFolderCreation.Test);
            DocumentServerEngine dse        = sm.DocumentServerEngine;
            string               folderName = sm.Faker.Random.AlphaNumeric(6);

            string expectedExtension   = sm.Faker.Random.String2(3);
            string expectedDescription = sm.Faker.Random.String2(32);


            // B. Createa A new Document Type.
            DocumentType documentType = new DocumentType(sm.Faker.Random.AlphaNumeric(7),
                                                         sm.Faker.Name.FullName(),
                                                         folderName,
                                                         EnumStorageMode.WriteOnceReadMany,
                                                         1,
                                                         1);
            documentType.IsActive = markDocumentTypeActive;
            Assert.That(documentType.IsActive, Is.EqualTo(markDocumentTypeActive), "B20:");
            sm.DB.DocumentTypes.Add(documentType);
            await sm.DB.SaveChangesAsync();


            // C. Create a document
            Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                  expectedDescription,
                                                                                  expectedExtension,
                                                                                  documentType.Id);
            Result<StoredDocument> result = await dse.StoreDocumentFirstTimeAsync(genFileResult.Value);


            // Z. Validate
            Assert.That(result.IsSuccess, Is.EqualTo(markDocumentTypeActive), "Z10:");

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
    }
}