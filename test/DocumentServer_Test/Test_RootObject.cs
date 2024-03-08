using DocumentServer.Core;
using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Models.Entities;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

[TestFixture]
public class Test_RootObject
{
    [OneTimeSetUp]
    public void Setup() { }



    // Confirms that The ApplicationID cannot be changed after initial save. 
    [Test]
    public async Task RootObjectUpdate_IgnoresWORMFields()
    {
        // A. Setup
        SupportMethods       sm             = new();
        DocumentServerEngine dse            = sm.DocumentServerEngine;
        int                  expAppId       = 1;
        string               expDescription = "Some Desc";
        bool                 expIsActive    = true;
        string               expName        = "The Name is";

        await sm.Initialize;


        RootObject rootObject = new()
        {
            ApplicationId = expAppId,
            Description   = expDescription,
            IsActive      = expIsActive,
            Name          = expName
        };
        sm.DB.RootObjects.Add(rootObject);
        await sm.DB.SaveChangesAsync();


        //***  B.  Make Changes
        string newDescription = sm.Faker.Random.String2(18);
        string newName        = sm.Faker.Random.String2(9);
        bool   newIsActive    = false;
        int    newAppId       = 2;
        rootObject.ApplicationId = newAppId;
        rootObject.Description   = newDescription;
        rootObject.IsActive      = newIsActive;
        rootObject.Name          = newName;
        await sm.DB.SaveChangesAsync();
        int newId = rootObject.Id;


        // Now Read Back
        sm.DB.ChangeTracker.Clear();


        //***  Z.  Validate
        RootObject? r2 = await sm.DB.RootObjects.SingleOrDefaultAsync(ro => ro.Id == newId);
        Assert.That(r2, Is.Not.Null, "Z10:");
        Assert.That(r2.Name, Is.EqualTo(newName), "Z20:");
        Assert.That(r2.Description, Is.EqualTo(newDescription), "Z30:");
        Assert.That(r2.IsActive, Is.EqualTo(newIsActive), "Z40:");
        Assert.That(r2.ApplicationId, Is.EqualTo(expAppId), "Z50:");
    }
}