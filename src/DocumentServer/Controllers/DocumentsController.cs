﻿using System.Text;
using DocumentServer.ClientLibrary;
using DocumentServer.Core;
using Microsoft.AspNetCore.Mvc;
using SlugEnt.DocumentServer.Db;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DocumentServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly DocServerDbContext   _db;
    private readonly DocumentServerEngine _docEngine;
    private          string               _storageDirectory = "";


    /// <summary>
    ///     Public constructor
    /// </summary>
    /// <param name="db"></param>
    public DocumentsController(DocServerDbContext db,
                               DocumentServerEngine documentServerEngine)
    {
        _db        = db;
        _docEngine = documentServerEngine;

        //_docEngine        = new DocumentServerEngine();

        _storageDirectory = @"T:\ProgrammingTesting";
    }


    // DELETE api/<DocumentsController>/5
    [HttpDelete("{id}")]
    public void Delete(int id) { }


    // GET api/<DocumentsController>/5
    [HttpGet("{id}/{name}")]
    public async Task<ActionResult<TransferDocumentDto>> GetStoredDocument(long id)
    {
        // For testing
        Result<TransferDocumentDto> result = await _docEngine.GetStoredDocumentAsync(id);

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
    [HttpPost]
    public async Task<ActionResult<string>> PostStoredDocument(TransferDocumentDto transferDocumentDto)
    {
        Result<StoredDocument> result = await _docEngine.StoreDocumentFirstTimeAsync(transferDocumentDto);

        if (result.IsSuccess)
        {
            long id = result.Value.Id;
            var val = new
            {
                Id = id
            };
            return Ok(val);
        }


        // TODO - return the errors from the Result
        StringBuilder sb = new();
        foreach (IError resultError in result.Errors)
            sb.Append(resultError + Environment.NewLine);
        return Problem(
                       sb.ToString(),
                       title: "Error Storing the Document"
                      );
    }


    //(************************************************************************
    /// <summary>
    ///     &&&&&&&
    /// </summary>
    /// <returns></returns>

    // GET: api/<DocumentsController>
    /*
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[]
        {
            "value1", "value2"
        };
    }
    */

    // PUT api/<DocumentsController>/5
    [HttpPut("{id}")]
    public void Put(int id,
                    [FromBody] string value) { }
}