using DocumentServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.FluentResults;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

public class Test_DocumentsController
{
    /// <summary>
    ///     DocumentServerEngine uses transactions internally.  You cannot have nested transactions, so they must be disabled
    ///     for these tests.
    /// </summary>
    private readonly bool _useDatabaseTransactions = false;



    [Test]
    public async Task StoreDocumentSuccess()
    {
        SupportMethods sm = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        await sm.Initialize;
        DocumentServerEngine dse = sm.DocumentServerEngine;

        int    expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string expectedRootObjectId = "x";

        DocumentsController controller = new DocumentsController(dse);

        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              "ab",
                                                                              "pdf",
                                                                              expectedDocTypeId,
                                                                              expectedRootObjectId,
                                                                              "extab");
        Assert.That(genFileResult.IsSuccess, Is.EqualTo(true), "A10:");


        //***  Z - Call the controller method.
        var actionResult = await controller.PostStoredDocument2(genFileResult.Value, TestConstants.APPA_TOKEN);
        Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>(), "Z200: Expected to receive an Ok response with the stored document Id.");
        OkObjectResult ok = actionResult.Result as OkObjectResult;


        // The expected Stored Document ID
        long obj = GetObjectResultContent<long>(actionResult);
        Assert.That(ok.StatusCode, Is.EqualTo(200), "Z300:");
        Assert.That(obj, Is.EqualTo(1), "Z400:");


        // Only if the controller method is returning a straight object and not wrapped in an Ok or Problem type return object
        //var x = actionResult.Value;
    }


/* Can't really not pass those parameters. The object is enforcing them
    [Test]
    [TestCase(false,
                 "desc",
                 "ext",
                 1,
                 "xyz",
                 "extkey")]
    [TestCase(true,
                 "desc",
                 null,
                 1,
                 "xyz",
                 "extkey")]
    public async Task StoreDocumentMissingRequiredInfo_Errors(bool shouldError,
                                                              string desc,
                                                              string? extension,
                                                              int? docTypeId,
                                                              string extRootObjId,
                                                              string extKey)
    {
        SupportMethods sm = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        await sm.Initialize;
        DocumentServerEngine dse = sm.DocumentServerEngine;

        int    expectedDocTypeId    = sm.DocumentType_Test_Worm_A;
        string expectedRootObjectId = "x";

        DocumentsController controller = new DocumentsController(dse);

        Result<TransferDocumentContainer> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                                    desc,
                                                                                    extension,
                                                                                    1,
                                                                                    extRootObjId,
                                                                                    extKey);
        Assert.That(genFileResult.IsSuccess, Is.EqualTo(true), "A10:");

        DocumentContainer documentContainer = new DocumentContainer();
        documentContainer.Info = genFileResult.Value.TransferDocument;
        documentContainer.File = genFileResult.Value.FileInFormFile;



        //***  Z - Call the controller method.
        var actionResult = await controller.PostStoredDocument(documentContainer, TestConstants.APPA_TOKEN);
        if (!shouldError)
        {
            Assert.That(actionResult.Result, Is.InstanceOf<OkObjectResult>(), "Z200: Expected to receive an Ok response with the stored document Id.");
            OkObjectResult ok = actionResult.Result as OkObjectResult;

            // The expected Stored Document ID
            long obj = GetObjectResultContent<long>(actionResult);
            Assert.That(ok.StatusCode, Is.EqualTo(200), "Z300:");
            Assert.That(obj, Is.EqualTo(1), "Z400:");
        }
        else
        {
            Assert.That(actionResult.Result, Is.InstanceOf<BadRequestObjectResult>());
            BadRequestObjectResult bad = actionResult.Result as BadRequestObjectResult;

            long msg = GetObjectResultContent<long>(actionResult);
        }


        // Only if the controller method is returning a straight object and not wrapped in an Ok or Problem type return object
        //var x = actionResult.Value;
    }
*/


    private static T GetObjectResultContent<T>(ActionResult<T> result) { return (T)((ObjectResult)result.Result).Value; }
}