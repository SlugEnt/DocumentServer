using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;


namespace SlugEnt.DocumentServer.API.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class NodeController : ControllerBase
{
    private readonly DocumentServerEngine _docEngine;


    /// <summary>
    ///     Public constructor
    /// </summary>
    /// <param name="db"></param>
    public NodeController(DocumentServerEngine documentServerEngine)
    {
        _docEngine = documentServerEngine;
        Console.WriteLine("Started NodeController");
    }



    [HttpGet]
    [Authorize(Policy = "NodeKeyPolicy")]
    public async Task<ActionResult> Alive() { return Ok(); }


    [HttpGet]
    public IActionResult Alive2() { return Ok(); }



    // GET api/<NodeController>/5
    [HttpGet("{id}")]
    [Authorize(Policy = "NodeKeyPolicy")]
    public async Task<ActionResult<ReturnedDocumentInfo>> GetStoredDocumentAndInfo(long id,
                                                                                   [FromHeader] string appToken)
    {
        return BadRequest("Not implemented Yet");
    }



    // POST api/<NodesController>
    /// <summary>
    ///     Stores a copy of the document on this node
    /// </summary>
    /// <returns>On Success:  Returns Document ID.  On Failure returns error message</returns>
    [HttpPost(Name = "StoreDocument")]
    [Authorize(Policy = "NodeKeyPolicy")]
    public async Task<ActionResult> StoreDocument([FromForm] RemoteDocumentStorageDto remoteDocumentStorageDto)
    {
        try
        {
            Result<StoredDocument> result = await _docEngine.StoreFileFromRemoteNode(remoteDocumentStorageDto);
            if (result.IsSuccess)
                return Ok();

            Console.WriteLine("Error in StoreDocument:  " + result.ToString());
            return Problem(result.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in StoreDocument:  " + ex.ToString());
            return BadRequest(ex.Message);
        }
    }
}