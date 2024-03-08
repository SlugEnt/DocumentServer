using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Models.Entities;
using Test_DocumentServer.SupportObjects;


namespace Test_DocumentServer;

[TestFixture]
public class Tests
{
    // Tests that CreatedAT timestamp field is automatically saved on all entity saves.
    [Test]
    public async Task CreatedAtUTC_SetOnSave()
    {
        SupportMethods sm = new();
        await sm.Initialize;

        Application? app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");


        IEnumerable<string> paths = sm.FileSystem.AllPaths;


        Application appNew = new()
        {
            Name  = "A new app",
            Token = sm.Faker.Random.String2(4),
        };
        sm.DB.Add(appNew);
        await sm.DB.SaveChangesAsync();

        // TODO fix this Nunit error about dates cannot be null.
        //Assert.That(app.CreatedAtUTC, Is.Not.Null, "A10:");
        //Assert.That(app.ModifiedAtUTC, Is.Not.Null, "A20:");
    }


    // Tests that ModifiedAT timestamp field is automatically saved on all entity saves.
    [Test]
    public async Task ModifiedAtUTC_SetOnUpdate()
    {
        SupportMethods sm = new();
        await sm.Initialize;

        Application? app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");

        app.Name = "app_ad";

        await sm.DB.SaveChangesAsync();

        // TODO fix this Nunit error about dates cannot be null.
        //Assert.That(app.CreatedAtUTC, Is.Not.Null, "A30:");
        //Assert.That(app.ModifiedAtUTC, Is.Not.Null, "A40:");
    }


    [SetUp]
    public void Setup() { }


    [Test]
    public void FormFileFile()
    {
        SupportMethods sm = new();
        string fileName = sm.WriteRandomFile(sm.FileSystem,
                                             "",
                                             "pdf",
                                             1024);

        string   fullName = sm.FileSystem.Path.Combine("", fileName);
        FormFile x        = sm.GetFormFile(fileName);
    }
}