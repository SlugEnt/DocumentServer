using DocumentServer.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SlugEnt.DocumentServer.API.Controllers;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using Test_DocumentServer.SupportObjects;

namespace Test_DocumentServer;

[TestFixture]
public class Test_NodesController
{
    /// <summary>
    ///     DocumentServerEngine uses transactions internally.  You cannot have nested transactions, so they must be disabled
    ///     for these tests.
    /// </summary>
    private readonly bool _useDatabaseTransactions = false;


    /// <summary>
    /// Test that we can store a document remotely.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task StoreRemoteDocumentSuccess()
    {
        //***  A) Initialize
        SupportMethods sm = new(EnumFolderCreation.Test, _useDatabaseTransactions);
        await sm.Initialize;
        DocumentServerEngine dse      = sm.DocumentServerEngine;
        string               fileName = "something.txt";
        string storagePath = Path.Join("remote",
                                       "month",
                                       "day");
        StorageNode storageNode = (StorageNode)sm.IDLookupDictionary.GetValueOrDefault("StorageNode_A");


        MockLogger<NodeController> _logger    = Substitute.For<MockLogger<NodeController>>();
        NodeController             controller = new NodeController(dse, _logger);


        //***  B) Setup
        Result<TransferDocumentDto> genFileResult = sm.TFX_GenerateUploadFile(sm,
                                                                              "ab",
                                                                              "pdf",
                                                                              sm.DocumentType_Test_Worm_A,
                                                                              "x",
                                                                              "extab",
                                                                              1);
        Assert.That(genFileResult.IsSuccess, Is.EqualTo(true), "A10:");
        RemoteDocumentStorageDto remoteDocumentStorageDto = new()
        {
            File          = genFileResult.Value.File,
            StorageNodeId = storageNode.Id,
            StoragePath   = storagePath,
            FileName      = fileName,
        };


        //*** T)  Test
        var      actionResult = await controller.StoreDocument(remoteDocumentStorageDto);
        OkResult ok           = actionResult as OkResult;

        //*** Y) Final Prep
        string completePath = Path.Join(sm.DocumentServerInformation.ServerHostInfo.Path,
                                        storageNode.NodePath,
                                        storagePath,
                                        fileName);


        //*** Z) Validate
        Assert.That(actionResult, Is.InstanceOf<OkResult>(), "Z100: Expected to receive an Ok response. But got: ");
        Assert.That(ok.StatusCode, Is.EqualTo(200), "Z200:");
        Assert.That(sm.FileSystem.AllFiles.Count(), Is.EqualTo(2), "Z210:  Expected 2 files.  Original and the stored one");
        Assert.That(sm.FileSystem.File.Exists(completePath), Is.True, "Z220:  File should exist");
    }


    [Test]
    public async Task IsAlive_Success()
    {
        //*** A) Setup
        SupportMethodsConfiguration smConfiguration = new()
        {
            UseTransactions = _useDatabaseTransactions
        };
        SupportMethods sm = new(smConfiguration);
        await sm.Initialize;
        DocumentServerEngine       dse        = sm.DocumentServerEngine;
        MockLogger<NodeController> _logger    = Substitute.For<MockLogger<NodeController>>();
        NodeController             controller = new NodeController(dse, _logger);


        //*** T - Test
        var actionResult = await controller.Alive();

        //*** V - Validate
        Assert.That(actionResult, Is.InstanceOf<OkResult>(), "Z100:  Expected an Ok Response");
    }



    private static T GetObjectResultContent<T>(ActionResult<T> result) { return (T)((ObjectResult)result.Result).Value; }


    private static int? GetStatusCode<T>(ActionResult<T?> actionResult)
    {
        IConvertToActionResult convertToActionResult      = actionResult; // ActionResult implicit implements IConvertToActionResult
        var                    actionResultWithStatusCode = convertToActionResult.Convert() as IStatusCodeActionResult;
        return actionResultWithStatusCode?.StatusCode;
    }
}