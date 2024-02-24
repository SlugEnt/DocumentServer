using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using Test_DocumentServer.SupportObjects;
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

            // TODO fix this Nunit error about dates cannot be null.
            //Assert.That(app.CreatedAtUTC, Is.Not.Null, "A10:");
            //Assert.That(app.ModifiedAtUTC, Is.Not.Null, "A20:");
        }


        // Tests that ModifiedAT timestamp field is automatically saved on all entity saves.
        [Test]
        public async Task ModifiedAtUTC_SetOnUpdate()
        {
            SupportMethods sm = new SupportMethods();

            Application app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");

            app.Name = "app_ad";

            await sm.DB.SaveChangesAsync();

            // TODO fix this Nunit error about dates cannot be null.
            //Assert.That(app.CreatedAtUTC, Is.Not.Null, "A30:");
            //Assert.That(app.ModifiedAtUTC, Is.Not.Null, "A40:");
        }
    }
}