﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
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
    public async Task<ActionResult<TransferDocumentDto>> GetStoredDocumentAsStream(long id,
                                                                                   [FromHeader] string appToken)

        //public async Task<ActionResult<TransferDocumentDto>> GetStoredDocumentAsStream(long id)
    {
        // Get the App Token from the header

        Result<ReturnedDocumentInfo> result = await _docEngine.GetStoredDocumentAsync(id, appToken);
        if (result.IsFailed)
            return BadRequest(result.ToString());


        // Send the file.
        //string contentType = MediaTypes.GetContentType(result.Value.TransferDocument.MediaType);
        ReturnedDocumentInfo returnedDocumentInfo = result.Value;
        return new FileContentResult(returnedDocumentInfo.FileInBytes, returnedDocumentInfo.ContentType);

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
    public async Task<ActionResult<ReturnedDocumentInfo>> GetStoredDocumentAndInfo(long id,
                                                                                   [FromHeader] string appToken)
    {
        Result<ReturnedDocumentInfo> result = await _docEngine.GetStoredDocumentAsync(id,
                                                                                      appToken);
        if (result.IsFailed)
            return BadRequest(result.ToString());


        // Build Return Object
        /*
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
        */
        // Send the file.
        return Ok(result.Value);
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
/*    [HttpPost(Name = "PostStoredDocument")]
    [Authorize(Policy = "ApiKeyPolicy")]
    public async Task<ActionResult<long>> PostStoredDocument([FromForm] DocumentContainer documentContainer,
                                                             [FromHeader] string appToken)
    {
        try
        {
            TransferDocumentContainer txfDocumentContainer = new()
            {
                TransferDocument = documentContainer.Info,
                FileInFormFile   = documentContainer.File,
            };

            Result<StoredDocument> result = await _docEngine.StoreDocumentFirstTimeAsync(txfDocumentContainer, appToken);
            if (result.IsSuccess)
                return Ok(result.Value.Id);


            return Problem(result.ToString());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
*/



    // POST api/<DocumentsController>
    /// <summary>
    ///     Places a document to be stored in to the DocumentServer
    /// </summary>
    /// <param name="transferDocumentDto">
    ///     The TransferDocumentDto that contains a document and the documents information to be
    ///     stored.
    /// </param>
    /// <returns>On Success:  Returns Document ID.  On Failure returns error message</returns>
    [HttpPost(Name = "PostStoredDocument2")]
    [Authorize(Policy = "ApiKeyPolicy")]
    public async Task<ActionResult<long>> PostStoredDocument2([FromForm] TransferDocumentDto docDTO,
                                                              [FromHeader] string appToken)
    {
        try
        {
            /*TransferDocumentContainer txfDocumentContainer = new()
            {
                TransferDocument = documentContainer.Info,
                FileInFormFile   = documentContainer.File,
            };

            Result<StoredDocument> result = await _docEngine.StoreDocumentFirstTimeAsync(txfDocumentContainer, appToken);
            */
            Result<StoredDocument> result = await _docEngine.StoreDocumentNew(docDTO, appToken);
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