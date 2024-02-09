using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using DocumentServer.Db;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using Application = DocumentServer.Models.Entities.Application;
using System.Runtime.CompilerServices;
using DocumentServer.ClientLibrary;
using Microsoft.EntityFrameworkCore;
using SlugEnt.FluentResults;


[assembly: InternalsVisibleTo("Test_DocumentServer")]

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
    /// <param name="transferDocumentDto">The file info to be stored</param>
    /// <param name="storageDirectory">Where to save it to.</param>
    /// <returns>StoredDocument if successful.  Returns Null if it failed.</returns>
    public async Task<Result<StoredDocument>> StoreDocumentFirstTimeAsync(TransferDocumentDto transferDocumentDto)
    {
        bool                    fileSavedToStorage      = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = new();

        try
        {
            // Retrieve the DocumentType
            // TODO this db call needs to be replaced with in memory hashset or something
            DocumentType docType = await _db.DocumentTypes
                                            .Include(i => i.ActiveStorageNode1)
                                            .Include(i => i.ActiveStorageNode2)
                                            .SingleOrDefaultAsync(d => d.Id == transferDocumentDto.DocumentTypeId);

            if (docType == null)
            {
                string msg = "Unable to locate a DocumentType with the Id [ " + transferDocumentDto.DocumentTypeId + " ]";
                return Result.Fail(msg);
            }

            if (!docType.IsActive)
            {
                string msg = String.Format("Document type [ {0} ] - [ {1} ] is not Active.", docType.Id, docType.Description);
                return Result.Fail(msg);
            }


            // TODO Need to implement logic to try 2nd active node.
            if (docType.ActiveStorageNode1Id == null)
            {
                string msg = string.Format("The DocumentType requested does not have a storage node specified.  DocType = " + docType.Id);
                _logger.LogError("DocumentType has an ActiveStorageNodeId that is null.  Must have a value.  {DocTypeId}", docType.Id);

                Result<StoredDocument> resultA = Result.Fail(new Error(msg));
                return resultA;
            }


            // We always use the primary node for initial storage.
            int fileSize = transferDocumentDto.FileInBase64Format.Length > 1024 ? transferDocumentDto.FileInBase64Format.Length / 1024 : 1;
            StoredDocument storedDocument = new(transferDocumentDto.FileExtension,
                                                transferDocumentDto.Description,
                                                "",
                                                fileSize,
                                                transferDocumentDto.DocumentTypeId,
                                                (int)docType.ActiveStorageNode1Id);

            // Generate File Description
            string fileName = storedDocument.ComputedStoredFileName;

            Result<string>         resultB = await ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id);
            Result<StoredDocument> resultC = Result.Fail("Cannot save Document.");
            Result                 merged  = Result.Merge(resultB, resultC);


            // Store the path and make sure all the paths exist.
            string storeAtPath = resultB.Value;
            _fileSystem.Directory.CreateDirectory(storeAtPath);


            // Decode the file bytes
            byte[] binaryFile;

            binaryFile   = Convert.FromBase64String(transferDocumentDto.FileInBase64Format);
            fullFileName = Path.Combine(storeAtPath, fileName);
            _fileSystem.File.WriteAllBytesAsync(fullFileName, binaryFile);
            fileSavedToStorage = true;

            // Save Database Entry
            storedDocument.StorageFolder = storeAtPath;

            _db.StoredDocuments.Add(storedDocument);
            await _db.SaveChangesAsync();
            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            // Delete the file from storage if we successfully saved it, but failed afterward.
            if (fileSavedToStorage)
            {
                File.Delete(fullFileName);
            }

            string msg =
                String.Format($"StoreDocument:  Failed to store the document:  Description: {transferDocumentDto.Description}, Extension: {transferDocumentDto.FileExtension}.  Error Was: {ex.Message}.  ");

            StringBuilder sb = new StringBuilder();
            sb.Append(msg);
            if (ex.InnerException != null)
                sb.Append(ex.InnerException.Message);
            msg = sb.ToString();

            _logger.LogError("StoreDocument: Failed to store document.{Description}{Extension}Error:{Error}.  Inner Error: {Inner}",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             ex.Message);
            Result errorResult = Result.Fail(new Error("Failed to store the document due to errors.").CausedBy(ex));
            return errorResult;
        }
    }



    /// <summary>
    /// Reads a document from the library and returns it to the caller.
    /// </summary>
    /// <param name="Id"></param>
    /// <returns>Result.Success and the file in Base64  or returns Result.Fail with error message</returns>
    public async Task<Result<string>> ReadStoredDocumentAsync(Guid Id)
    {
        try
        {
            StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(s => s.Id == Id);
            if (storedDocument == null)
                return Result.Fail("Unable to find a Stored Document with that Id");


            // Now Load the Stored Document.
            string fileName     = storedDocument.ComputedStoredFileName;
            string fullFileName = Path.Join(storedDocument.StorageFolder, fileName);
            string file         = Convert.ToBase64String(_fileSystem.File.ReadAllBytes(fullFileName));

            // TODO update the number of times accessed
            return Result.Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError("ReadStoredDocumentAsync:  DocumentId [{DocumentId} ]Exception:  {Error}", Id, ex.Message);
            return Result.Fail(new Error("Unable to read document from library.").CausedBy(ex));
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
    /// All files are stored in a folder by yyyy/mm/dd
    /// </summary>
    /// <param name="documentType">The DocumentType</param>
    /// <param name="storageNodeId">The StorageNode Id to store on</param>
    /// <param name="fileName">The complete filename including extension</param>
    /// <returns>Result</returns>
    internal async Task<Result<string>> ComputeStorageFullNameAsync(DocumentType documentType,
                                                                    int storageNodeId)
    {
        try
        {
            DateTime currentUtc = DateTime.UtcNow;
            string   year       = currentUtc.ToString("yyyy");
            string   month      = currentUtc.ToString("MM");


#if DEBUG

            // Displays the Change Tracker Cache
            Console.WriteLine("Displaying ChangeTracker Cache");
            foreach (var entityEntry in _db.ChangeTracker.Entries())
            {
                Console.WriteLine($"Found {entityEntry.Metadata.Name} entity with ID {entityEntry.Property("Id").CurrentValue}");
            }
#endif

            // Retrieve Storage Node
            StorageNode storageNode = await _db.StorageNodes.SingleAsync(n => n.Id == storageNodeId);


            string modePath = documentType.StorageMode switch
            {
                EnumStorageMode.WriteOnceReadMany => "W",
                EnumStorageMode.Temporary         => "T",
                EnumStorageMode.Editable          => "E",
                EnumStorageMode.Versioned         => "V",
                _                                 => ""
            };
            if (modePath == string.Empty)
            {
                return Result.Fail("Unknown StorageMode Value [ " + documentType.StorageMode.ToString() + " ] for DocumentType: " + documentType.Id);
            }


            string path = Path.Combine(storageNode.NodePath,
                                       modePath,
                                       documentType.StorageFolderName,
                                       year,
                                       month);
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