using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using DocumentServer.Db;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using DocumentServer_Test.SupportObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

//using ILogger = Castle.Core.Logging.ILogger;

namespace DocumentServer_Test
{
    public class Tests
    {
        private DatabaseSetup_Test databaseSetupTest = new DatabaseSetup_Test();


        [SetUp]
        public void Setup() { }



        // Tests that CreatedAT timestamp field is automatically saved on all entity saves.
        [Test]
        public async Task CreatedAtUTC_SetOnSave()
        {
            SupportMethods sm = new SupportMethods(databaseSetupTest);


            Application app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");


            IEnumerable<string> paths = sm.FileSystem.AllPaths;


            Application appNew = new()
            {
                Name = "A new app"
            };
            sm.DB.Add<Application>(appNew);
            await sm.DB.SaveChangesAsync();

            Assert.IsNotNull(appNew.CreatedAtUTC, "A10:");
            Assert.IsNull(appNew.ModifiedAtUTC, "A20");
        }


        // Tests that ModifiedAT timestamp field is automatically saved on all entity saves.
        [Test]
        public async Task ModifiedAtUTC_SetOnUpdate()
        {
            SupportMethods sm = new SupportMethods(databaseSetupTest);

            Application app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");

            app.Name = "app_ad";

            await sm.DB.SaveChangesAsync();

            Assert.IsNotNull(app.CreatedAtUTC, "A10:");
            Assert.IsNotNull(app.ModifiedAtUTC, "A20");
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
            string fileName = sm.WriteRandomFile(sm.FileSystem, sm.Folder_Test, extension,
                                                 3);
            string fullPath = Path.Combine(sm.Folder_Test, fileName);
            Assert.IsTrue(sm.FileSystem.FileExists(fullPath), "A10:");


            // A20. Read the File
            string file = Convert.ToBase64String(File.ReadAllBytes(fullPath));


            // B.  Now Store it in the DocumentServer
            DocumentUploadDTO upload = new DocumentUploadDTO()
            {
                Description    = "Some Description",
                DocumentTypeId = sm.DocumentType_Test_Worm_A,
                FileExtension  = extension,
                FileBytes      = file,
            };
            documentServerEngine.StoreDocumentFirstTimeAsync(upload, "");


            // Z. Validate
            sm.DB.ChangeTracker.Clear();
            Application app2 = sm.DB.Applications.Single(a => a.Name == "A new app");
            Assert.AreEqual(app2.Name, app2.Name, "Z10:");
        }
    }
}