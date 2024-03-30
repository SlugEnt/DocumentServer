using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;


namespace SlugEnt.DocumentServer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class NodeController : ControllerBase
{
    private readonly DocumentServerEngine _docEngine;


    /// <summary>
    ///     Public constructor
    /// </summary>
    /// <param name="db"></param>
    public NodeController(DocumentServerEngine documentServerEngine) { _docEngine = documentServerEngine; }



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
    /// <param name="transferDocumentDto">
    ///     The TransferDocumentDto that contains a document and the documents information to be
    ///     stored.
    /// </param>
    /// <returns>On Success:  Returns Document ID.  On Failure returns error message</returns>
    [HttpPost(Name = "StoreReplicateDocument")]
    [Authorize(Policy = "NodeKeyPolicy")]
    public async Task<ActionResult<long>> StoreReplicateDocument([FromForm] DocumentReplicationDto documentReplicationDto)
    {
        try
        {
            Result<StoredDocument> result = await _docEngine.StoreDocumentReplica(documentReplicationDto);
            if (result.IsSuccess)
                return Ok(result.Value.Id);

            return Problem(result.ToString());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}