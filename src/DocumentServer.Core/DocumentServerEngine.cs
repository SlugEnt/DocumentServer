using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using DocumentServer.Db;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using Application = DocumentServer.Models.Entities.Application;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using SlugEnt.FluentResults;


[assembly: InternalsVisibleTo("DocumentServer_Test")]

namespace DocumentServer.Core;

/// <summary>
/// Performs all core logic operations of the DocumentServer including database access and file access methods.
/// </summary>
public class DocumentServerEngine
{
    private readonly IFileSystem                  _fileSystem;
    private          DocServerDbContext           _db;
    private readonly ILogger                      _logger;
    private          Dictionary<int, StorageNode> _storageNodes;


    /// <summary>
    /// Constructor with Dependency Injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileSystem"></param>
    public DocumentServerEngine(ILogger<DocumentServerEngine> logger,
                                DocServerDbContext dbContext,
                                IFileSystem fileSystem = null)
    {
        _logger = logger;
        _db     = dbContext;

        if (fileSystem != null)
            _fileSystem = fileSystem;

        // Use the real file system
        else
            _fileSystem = new FileSystem();
    }



    /// <summary>
    /// Sets the Database to use
    /// </summary>
    public DocServerDbContext DocumentServerDatabase
    {
        set { _db = value; }
    }



    /// <summary>
    /// Stores a document to the Storage Engine and database
    /// </summary>
    /// <param name="documentUploadDto">The file info to be stored</param>
    /// <param name="storageDirectory">Where to save it to.</param>
    /// <returns>StoredDocument if successful.  Returns Null if it failed.</returns>
    public async Task<Result<StoredDocument>> StoreDocumentFirstTimeAsync(DocumentUploadDTO documentUploadDto,
                                                                          string storageDirectory)
    {
        // TODO change return to Result
        //TODO is stoageDirectory parameter needed?


        bool                    fileSavedToStorage      = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = new();
        Result<StoredDocument>  opResults               = new();

        try
        {
            // Retrieve the DocumentType
            // TODO this db call needs to be replaced with in memory hashset or something
            DocumentType docType = _db.DocumentTypes.Single(d => d.Id == documentUploadDto.DocumentTypeId);

            // TODO Need to implement logic to try 2nd active node.
            if (docType.ActiveStorageNode1Id == null)
            {
                string msg = string.Format("The DocumentType requested does not have a storage node specified.  DocType = " + docType.Id);
                _logger.LogError("DocumentType has an ActiveStorageNodeId that is null.  Must have a value.  {DocTypeId}", docType.Id);

                opResults.WithError(msg);

//                documentOperationStatus.WithError(msg);
                //              return documentOperationStatus;
                return opResults;
            }


            // We always use the primary node for initial storage.
            int fileSize = documentUploadDto.FileBytes.Length > 1024 ? documentUploadDto.FileBytes.Length / 1024 : 1;
            StoredDocument storedDocument = new(documentUploadDto.Description,
                                                storageDirectory,
                                                fileSize,
                                                documentUploadDto.DocumentTypeId,
                                                (int)docType.ActiveStorageNode1Id);

            // Generate File Description
            string         fileName = storedDocument.Id.ToString() + documentUploadDto.FileExtension;
            Result<string> result   = await ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id, fileName);
            if (result.IsFailed)
            {
                Result merged = Result.Merge(opResults, result);
                return merged;
            }

            // TODO result has the path - Figure out where to store it.  
            string storeAtPath = result.Value;

            // Decode the file bytes
            byte[] binaryFile;

            binaryFile   = Convert.FromBase64String(documentUploadDto.FileBytes);
            fullFileName = Path.Combine(storageDirectory, fileName.ToString());
            File.WriteAllBytesAsync(fullFileName, binaryFile);
            fileSavedToStorage = true;

            // Save Database Entry
            _db.StoredDocuments.Add(storedDocument);
            await _db.SaveChangesAsync();
            return documentOperationStatus;
        }
        catch (Exception ex)
        {
            // Delete the file from storage if we successfully saved it, but failed afterward.
            if (fileSavedToStorage)
            {
                File.Delete(fullFileName);
            }

            string msg =
                String.Format($"StoreDocument:  Failed to store the document:  Description: {documentUploadDto.Description}, Extension: {documentUploadDto.FileExtension}.  Error Was: {ex.Message}.  ");

            StringBuilder sb = new StringBuilder();
            sb.Append(msg);
            if (ex.InnerException != null)
                sb.Append(ex.InnerException.Message);
            msg = sb.ToString();

            _logger.LogError("StoreDocument: Failed to store document.{Description}{Extension}Error:{Error}.  Inner Error: {Inner}",
                             documentUploadDto.Description,
                             documentUploadDto.FileExtension,
                             ex.Message,
                             ex.InnerException.Message);
            opResults.WithError(msg);
            return opResults;
        }
    }


    /// <summary>
    ///  Loads / Reloads the master data tables.
    /// </summary>
    /// <returns></returns>
    public async Task LoadMasterDataTables()
    {
        Dictionary<int, StorageNode> tempStorageNodes = new();
    }


    /// <summary>
    /// Computes the complete path including the actual file name.
    /// </summary>
    /// <param name="documentType">The DocumentType</param>
    /// <param name="storageNodeId">The StorageNode Id to store on</param>
    /// <param name="fileName">The complete filename including extension</param>
    /// <returns>Result</returns>
    internal async Task<Result<string>> ComputeStorageFullNameAsync(DocumentType documentType,
                                                                    int storageNodeId,
                                                                    string fileName)
    {
        try
        {
            // TODO this needs to be  replaced with in memory lookup
            StorageNode storageNode = await _db.StorageNodes.SingleAsync(n => n.Id == storageNodeId);


            string modePath = documentType.StorageMode switch
            {
                EnumStorageMode.WriteOnceReadMany => "W",
                EnumStorageMode.Temporary         => "T",
                EnumStorageMode.Editable          => "E",
                _ => throw new ArgumentOutOfRangeException(nameof(documentType.StorageMode),
                                                           $"Unknown StorageMode value: {documentType.StorageMode}"),
            };


            string path = Path.Combine(storageNode.NodePath, modePath, fileName);
            return Result.Ok(path);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("ComputeStorageFolder:  Invalid Storage Node Id:  {StorageNode} for DocumentType: {DocumentType}",
                             storageNodeId,
                             documentType.Id);
            string msg =
                String.Format("ComputeStorageFolder:  Invalid Storage Node Id:  {0} for DocumentType: {1}",
                              storageNodeId,
                              documentType.Id);
            return Result.Fail(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError("ComputeStorageFolder error for {DocumentType} - Storage Node Id: {StorageNode} -   {Error}",
                             documentType.Id,
                             storageNodeId,
                             ex.Message);

            string msg =
                String.Format("ComputeStorageFolder error for {0} - Storage Node Id: {1} -   {2}",
                              documentType.Id,
                              storageNodeId,
                              ex.Message);
            return Result.Fail(msg);
        }
    }
}