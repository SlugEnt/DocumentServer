using System.IO.Abstractions.TestingHelpers;
using DocumentServer.Core;
using DocumentServer.Db;
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



        [Test]
        public async Task Test1()
        {
            SupportMethods sm = new SupportMethods(databaseSetupTest);


            Application app = await sm.DB.Applications.SingleOrDefaultAsync(s => s.Name == "App_A");


            IEnumerable<string> paths = sm.FileSystem.AllPaths;

            Assert.Pass();
        }
    }
}