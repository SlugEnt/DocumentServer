using System.Net;
using System.Net.Mime;
using DocumentServer.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.Extensions.Options;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DocumentServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly DocumentServerEngine _docEngine;


    /// <summary>
    ///     Public constructor
    /// </summary>
    /// <param name="db"></param>
    public DocumentsController(DocumentServerEngine documentServerEngine) { _docEngine = documentServerEngine; }


    // DELETE api/<DocumentsController>/5
    //[HttpDelete("{id}")]
    //public void Delete(int id) { }


    /// <summary>
    /// Returns a streamed file content
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>

    // GET api/<DocumentsController>/5
    [HttpGet("{id}/stream")]
    [Authorize(Policy = "ApiKeyPolicy")]
    public async Task<ActionResult<TransferDocumentDto>> GetStoredDocumentAsStream(long id)
    {
        Result<TransferDocumentContainer> result = await _docEngine.GetStoredDocumentAsync(id);
        if (result.IsFailed)
            return BadRequest(result.ToString());


        // Send the file.
        string contentType = MediaTypes.GetContentType(result.Value.TransferDocument.MediaType);
        return new FileContentResult(result.Value.FileInBytes, contentType);

        // Another way to return a file do it via stream???
        //return File(result.Result, "image/png", "test.jpg");
    }


    /// <summary>
    /// Returns the stored document along with some metadata
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>

    // GET api/<DocumentsController>/5
    [HttpGet("{id}")]
    [Authorize(Policy = "ApiKeyPolicy")]
    public async Task<ActionResult<DocumentContainer>> GetStoredDocumentAndInfo(long id)
    {
        Result<TransferDocumentContainer> result = await _docEngine.GetStoredDocumentAsync(id);
        if (result.IsFailed)
            return BadRequest(result.ToString());


        // Build Return Object
        TransferDocumentContainer tdc = result.Value;
        DocumentContainer documentContainer = new()
        {
            DocumentInfo = new ReturnedDocumentInfo()
            {
                Extension   = tdc.TransferDocument.FileExtension,
                Size        = tdc.FileSize,
                FileInBytes = tdc.FileInBytes,
                Description = tdc.TransferDocument.Description,
            },
        };

        // Send the file.
        return Ok(documentContainer);
    }



    // POST api/<DocumentsController>
    /// <summary>
    ///     Places a document to be stored in to the DocumentServer
    /// </summary>
    /// <param name="transferDocumentDto">
    ///     The TransferDocumentDto that contains a document and the documents information to be
    ///     stored.
    /// </param>
    /// <returns>On Success:  Returns Document ID.  On Failure returns error message</returns>
    [HttpPost(Name = "PostStoredDocument")]
    [Authorize(Policy = "ApiKeyPolicy")]
    public async Task<ActionResult<string>> PostStoredDocument([FromForm] DocumentContainer documentContainer)
    {
        try
        {
            TransferDocumentContainer txfDocumentContainer = new()
            {
                TransferDocument = documentContainer.Info,
                FileInFormFile   = documentContainer.File,
            };

            Result<StoredDocument> result = await _docEngine.StoreDocumentFirstTimeAsync(txfDocumentContainer);
            if (result.IsSuccess)
                return Ok(result.Value.Id);


            return Problem(result.ToString());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }



    // PUT api/<DocumentsController>/5
    /*[HttpPut("{id}")]
    public void Put(int id,
                    [FromBody] string value) { }
    */
}