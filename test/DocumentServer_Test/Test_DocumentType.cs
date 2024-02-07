using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using DocumentServer.Core;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using DocumentServer_Test.SupportObjects;
using SlugEnt;

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
    }
}