using System.Net;
using DocumentServer.Core;
using DocumentServer.Db;
using DocumentServer.Models;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DocumentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly DocServerDbContext   _db;
        private          DocumentServerEngine _docEngine;
        private          string               _storageDirectory = "";


        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="db"></param>
        public DocumentsController(DocServerDbContext db, DocumentServerEngine documentServerEngine)
        {
            _db        = db;
            _docEngine = documentServerEngine;

            //_docEngine        = new DocumentServerEngine();

            _storageDirectory = @"T:\ProgrammingTesting";
        }


        // GET api/<DocumentsController>/5
        [HttpGet("{id}/{name}")]
        public async Task<ActionResult<StoredDocument>> GetStoredDocument(string id, string name)
        {
            // For testing
            StoredDocument storedDocument = new StoredDocument();
            return Ok(storedDocument);
        }



        // POST api/<DocumentsController>
        /// <summary>
        /// Stores a passed in document to the storage engine
        /// </summary>
        /// <param name="documentUploadDto">The DocumentUploadDTO that contains a document and the documents information to be stored.</param>
        /// <returns>On Success:  Returns Document ID.  On Failure returns error message</returns>
        [HttpPost]
        public async Task<ActionResult<string>> PostStoredDocument(DocumentUploadDTO documentUploadDto)
        {
            DocumentOperationStatus documentOperationStatus = await _docEngine.StoreDocumentFirstTimeAsync(documentUploadDto, _storageDirectory);
            if (!documentOperationStatus.IsErrored)
            {
                return Ok(documentOperationStatus.StoredDocument.Id);
            }


            return Problem(
                           detail: documentOperationStatus.ErrorMessage,
                           title: "Error Storing the Document"
                          );
        }



        //(************************************************************************
        //(************************************************************************
        /*
        [HttpPost]
        public async Task<ActionResult> SaveDocumentToFilePath([FromQuery] string fullFilePath, IFormFile file)
        {
            string linuxFilePath = WindowsPathToLinuxPath(fullFilePath);

            FileInfo fileInfo = new(linuxFilePath);
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);

            Exception latestException = null;

            byte fileLockedTries = 0;
            while (fileLockedTries < 10)
            {
                try
                {
                    await using FileStream fileStream = new(linuxFilePath, FileMode.OpenOrCreate, FileAccess.Write);

                    fileStream.SetLength(0); // Set to 0 so we don't leave extra data on an overwrite

                    await file.CopyToAsync(fileStream);

                    return Ok();
                }
                catch (Exception e)
                {
                    latestException = e;

                    if (e is UnauthorizedAccessException or IOException)
                    {
                        await Task.Delay(50); // Wait 50 milliseconds
                        fileLockedTries++;
                        continue;
                    }

                    _logger.LogSystemException(LogLevel.Error, e, e.Message);

                    return BadRequest();
                }
            }

            if (latestException != null)
            {
                _logger.LogSystemException(LogLevel.Error, latestException, latestException.Message);
            }

            return BadRequest();
        }
        */


        //(************************************************************************
        /// <summary>
        /// &&&&&&&
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
        public void Put(int id, [FromBody] string value) { }


        // DELETE api/<DocumentsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id) { }
    }
}