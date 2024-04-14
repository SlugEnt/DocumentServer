using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

[SetUpFixture]
public class OneTime
{
    public OneTime() { }


    [OneTimeTearDown]
    public void TearDown()
    {
        if (SecondAPI.IsInitialized)
            SecondAPI.StopSecondAPI();
    }


    [OneTimeSetUp]
    public void Startup() { }
}