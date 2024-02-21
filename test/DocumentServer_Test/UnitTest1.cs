using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using DocumentServer_Test.SupportObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

//using ILogger = Castle.Core.Logging.ILogger;

namespace DocumentServer_Test
{
    public class Tests
    {
        [SetUp]
        public void Setup() { }



        // Tests that CreatedAT timestamp field is automatically saved on all entity saves.
        [Test]
        public async Task CreatedAtUTC_SetOnSave()
        {
            SupportMethods sm = new SupportMethods();


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
            SupportMethods sm = new SupportMethods();

            Application app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");

            app.Name = "app_ad";

            await sm.DB.SaveChangesAsync();

            Assert.IsNotNull(app.CreatedAtUTC, "A10:");
            Assert.IsNotNull(app.ModifiedAtUTC, "A20");
        }
    }
}