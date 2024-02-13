using System.Diagnostics;
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
using Microsoft.EntityFrameworkCore.Storage;
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
    /// Loads the specified DocumentType and validates some values before returning.  
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <returns>Result.Success if good, Result.Fail if bad</returns>
    internal async Task<Result<DocumentType>> LoadDocumentType_ForSavingStoredDocument(int documentTypeId)
    {
        // Retrieve the DocumentType
        // TODO this db call needs to be replaced with in memory hashset or something
        DocumentType docType = await _db.DocumentTypes
                                        .Include(i => i.ActiveStorageNode1)
                                        .Include(i => i.ActiveStorageNode2)
                                        .SingleOrDefaultAsync(d => d.Id == documentTypeId);

        if (docType == null)
        {
            string msg = "Unable to locate a DocumentType with the Id [ " + documentTypeId + " ]";
            return Result.Fail(new Error(msg));
        }

        if (!docType.IsActive)
        {
            string msg = String.Format("Document type [ {0} ] - [ {1} ] is not Active.", docType.Id, docType.Name);
            return Result.Fail(new Error(msg));
        }


        // TODO Need to implement logic to try 2nd active node.
        if (docType.ActiveStorageNode1Id == null)
        {
            string msg = string.Format("The DocumentType requested does not have a storage node specified.  DocType [ " + docType.Id) + " ]";
            _logger.LogError("DocumentType has an ActiveStorageNodeId that is null.  Must have a value.  {DocTypeId}", docType.Id);

            return Result.Fail(new Error(msg));
        }

        return Result.Ok(docType);
    }



    /// <summary>
    /// Determines where to store the file and then stores it.
    /// </summary>
    /// <param name="storedDocument">The StoredDocument that will be updated with path info</param>
    /// <param name="documentType">DocumentType this document is</param>
    /// <param name="storageNodeId">Node Id to store file at</param>
    /// <param name="fileInBase64Format">Contents of the file</param>
    /// <returns>Result string where string is the full file name </returns>
    internal async Task<Result<string>> StoreFileOnStorageMediaAsync(StoredDocument storedDocument,
                                                                     DocumentType documentType,
                                                                     int storageNodeId,
                                                                     string fileInBase64Format)
    {
        string         fullFileName = "";
        Result<string> result       = new Result();

        try
        {
            Result<string> resultB = await ComputeStorageFullNameAsync(documentType, storageNodeId);
            if (resultB.IsFailed)
            {
                resultB.WithError("Failed to compute Where the document should be stored.");
                return resultB;
            }

            // Store the path and make sure all the paths exist.
            string storeAtPath = resultB.Value;
            _fileSystem.Directory.CreateDirectory(storeAtPath);


            // Decode the file bytes
            byte[] binaryFile;

            binaryFile   = Convert.FromBase64String(fileInBase64Format);
            fullFileName = Path.Combine(storeAtPath, storedDocument.FileName);
            _fileSystem.File.WriteAllBytesAsync(fullFileName, binaryFile);

//            fileSavedToStorage = true;

            // Save the path in the StoredDocument
            storedDocument.StorageFolder = storeAtPath;
            return Result.Ok(fullFileName);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to save file on permanent media. FullFileName [ " + fullFileName + " ] ").CausedBy(ex));
        }
    }



    /// <summary>
    /// Stores a document to the Storage Engine and database, if it is a new document
    /// </summary>
    /// <param name="transferDocumentDto">The file info to be stored</param>
    /// <param name="storageDirectory">Where to save it to.</param>
    /// <returns>StoredDocument if successful.  Returns Null if it failed.</returns>
    public async Task<Result<StoredDocument>> StoreDocumentFirstTimeAsync(TransferDocumentDto transferDocumentDto)
    {
        IDbContextTransaction   transaction             = null;
        bool                    priorDBTransaction      = false;
        bool                    fileSavedToStorage      = false;
        bool                    isInTransaction         = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = new();
        Result<StoredDocument>  result                  = new Result<StoredDocument>();
        try
        {
            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> docTypeResult = await LoadDocumentType_ForSavingStoredDocument(transferDocumentDto.DocumentTypeId);
            if (docTypeResult.IsFailed)
                return Result.Merge(result, docTypeResult);

            DocumentType docType = docTypeResult.Value;


            // We always use the primary node for initial storage.
            int fileSize = transferDocumentDto.FileInBase64Format.Length > 1024 ? transferDocumentDto.FileInBase64Format.Length / 1024 : 1;
            StoredDocument storedDocument = new(transferDocumentDto.FileExtension,
                                                transferDocumentDto.Description,
                                                "",
                                                fileSize,
                                                transferDocumentDto.DocumentTypeId,
                                                (int)docType.ActiveStorageNode1Id);

            // Store the document on the storage media
            Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                            docType,
                                                                            (int)docType.ActiveStorageNode1Id,
                                                                            transferDocumentDto.FileInBase64Format);
            if (storeResult.IsFailed)
                return Result.Merge(result, storeResult);

            fullFileName       = storeResult.Value;
            fileSavedToStorage = true;


            // Save StoredDocument
            transaction = _db.Database.BeginTransaction();
            _db.StoredDocuments.Add(storedDocument);
            await PreSaveEdits(docType, storedDocument);
            _db.Database.CommitTransaction();

            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            await _db.Database.RollbackTransactionAsync();


            // Delete the file from storage if we successfully saved it, but failed afterward.
            if (fileSavedToStorage)
            {
                _fileSystem.File.Delete(fullFileName);
            }

            string msg =
                String.Format($"StoreDocument:  Failed to store the document:  Description: {transferDocumentDto.Description}, Extension: {transferDocumentDto.FileExtension}.  Error Was: {ex.Message}.  ");

            StringBuilder sb = new StringBuilder();
            sb.Append(msg);
            if (ex.InnerException != null)
                sb.Append(ex.InnerException.Message);
            msg = sb.ToString();

            _logger.LogError("StoreDocument: Failed to store document.{Description}{Extension}Error:{Error}.",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             msg);
            Result errorResult = Result.Fail(new Error("Failed to store the document due to errors.").CausedBy(ex));
            return errorResult;
        }
    }



    /// <summary>
    /// Stores a new document that is a replacement for an existing document.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> StoreReplacementDocumentAsync(ReplacementDto replacementDto)
    {
        // The replacement process is:
        // - Store new file
        // - Update StoredDocument
        // - Delete Old File

        IDbContextTransaction  transaction        = null;
        bool                   fileSavedToStorage = false;
        string                 fullFileName       = "";
        bool                   docSaved           = false;
        string                 oldFileName        = "";
        Result<StoredDocument> result             = new Result<StoredDocument>();
        try
        {
            // We need to load the existing StoredDocument to retrieve some info.
            StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentId);
            if (storedDocument == null)
            {
                string msg = String.Format("Unable to find existing StoredDocument with Id [ {0} ]", replacementDto.CurrentId);
                return Result.Fail(new Error(msg));
            }

            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> docTypeResult = await LoadDocumentType_ForSavingStoredDocument(storedDocument.DocumentTypeId);
            if (docTypeResult.IsFailed)
                return Result.Merge(result, docTypeResult);

            DocumentType docType = docTypeResult.Value;


            // Save the current Document FileName so we can delete it in a moment.
            oldFileName = storedDocument.FileNameAndPath;

            // Store the new document on the storage media
            storedDocument.ReplaceFileName(replacementDto.FileExtension);
            Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                            docType,
                                                                            (int)docType.ActiveStorageNode1Id,
                                                                            replacementDto.FileInBase64Format);
            if (storeResult.IsFailed)
                return Result.Merge(result, storeResult);

            fullFileName       = storeResult.Value;
            fileSavedToStorage = true;


            // Save StoredDocument
            //  - Update description
            if (!string.IsNullOrEmpty(replacementDto.Description))
                storedDocument.Description = replacementDto.Description;

            transaction = _db.Database.BeginTransaction();
            _db.StoredDocuments.Update(storedDocument);
            await PreSaveEdits(docType, storedDocument);
            _db.Database.CommitTransaction();
            docSaved = true;

            // Delete the old document
            _fileSystem.File.Delete(oldFileName);

            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            if (docSaved)
            {
                // Then error happened during file delete of old document
                // TODO add to error table.
                string msg = string.Format("Error during cleanup of old replaceable file [ {0} ].  File remains on storagemedia and will need to be deleted manually.",
                                           oldFileName);
                return Result.Fail(new Error(msg).CausedBy(ex));
            }

            _logger.LogError("StoreReplacementDocumentAsync:  {Exception}", ex.Message);
            return Result.Fail(new Error("StoreReplacementDocumentTypeAsync:  " + ex.Message).CausedBy(ex));
        }
    }



    /// <summary>
    /// This is logic that is called before saving a StoredDocument, either first save or subsequent update saves.
    ///  The StoredDocument is saved in this method.
    /// </summary>
    /// <param name="documentType"></param>
    /// <param name="storedDocument"></param>
    /// <param name="isFirstSave"></param>
    /// <returns></returns>
    private async Task PreSaveEdits(DocumentType documentType,
                                    StoredDocument storedDocument,
                                    bool isFirstSave = true)
    {
        ExpiringDocument expiring = null;

        if (isFirstSave)
        {
            if (documentType.StorageMode == EnumStorageMode.Temporary)
            {
                // Since this is a temporary document we immediately mark it as inactive to start its lifetime counter.
                storedDocument.IsAlive = false;

                // Need to add an entry to the ExpiringDocuments table
                expiring = new ExpiringDocument(documentType.InActiveLifeTime);
            }
        }

        // Save the StoredDocument so we can get id to set for Expiring
        await _db.SaveChangesAsync();

        // Set Expiring Id to the storeddocuments Id.
        if (expiring != null)
        {
            expiring.StoredDocumentId = storedDocument.Id;
            _db.Add(expiring);
            await _db.SaveChangesAsync();
        }
    }



    /// <summary>
    /// Reads a document from the library and returns it to the caller.
    /// </summary>
    /// <param name="Id"></param>
    /// <returns>Result.Success and the file in Base64  or returns Result.Fail with error message</returns>
    public async Task<Result<string>> ReadStoredDocumentAsync(long Id)
    {
        try
        {
            StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(s => s.Id == Id);
            if (storedDocument == null)
                return Result.Fail("Unable to find a Stored Document with that Id");


            // Now Load the Stored Document.
            string fileName     = storedDocument.FileName;
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
    /// Replaces a Replaceable document with a new one.  Marks for deletion the old one.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> ReplaceDocument(ReplacementDto replacementDto)
    {
        // Lets read current stored document
        StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentId);
        if (storedDocument == null)
            return Result.Fail(new Error("Unable to find existing StoredDocument with Id [ " + replacementDto.CurrentId + " ]"));


        return Result.Ok(storedDocument);
    }



    /// <summary>
    /// Changes the IsAlive status of a DocumentType
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <param name="isAliveValue"></param>
    /// <returns></returns>
    public async Task<Result> SetDocumentTypeAliveStatus(int documentTypeId,
                                                         bool isAliveValue)
    {
        try
        {
            DocumentType documentType = await _db.DocumentTypes.SingleAsync(d => d.Id == documentTypeId);
            if (documentType == null)
            {
                return Result.Fail("Unable to find a DocumentType with the Id of " + documentTypeId);
            }

            if (documentType.IsActive == isAliveValue)
            {
                return Result.Ok();
            }

            // Change it 
            documentType.IsActive = isAliveValue;
            await _db.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError("FAiled to change DocumentType [ {DocumentType} Active Status.  Error: {Error}", documentTypeId, ex);
            return Result.Fail(new Error("Failed to change DocumentType IsAlive Status due to error.").CausedBy(ex));
        }
    }


#region "Support Functions"

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
    /// All files are stored in a folder by
    ///   storageNodePath\StorageModeLetter\DocumentTypePath\year\month
    /// </summary>
    /// <param name="documentType">The DocumentType</param>
    /// <param name="storageNodeId">The StorageNode Id to store on</param>
    /// <returns>Result</returns>
    internal async Task<Result<string>> ComputeStorageFullNameAsync(DocumentType documentType,
                                                                    int storageNodeId)
    {
        try
        {
            DateTime folderDatetime = DateTime.UtcNow;

            // For most of the documents we store them in a folder based upon the date we write them.
            // But for Temporary we write based upon the date they expire.
            if (documentType.StorageMode == EnumStorageMode.Temporary)
            {
                folderDatetime = documentType.InActiveLifeTime switch
                {
                    EnumDocumentLifetimes.HoursOne    => folderDatetime.AddHours(1),
                    EnumDocumentLifetimes.HoursFour   => folderDatetime.AddHours(4),
                    EnumDocumentLifetimes.HoursTwelve => folderDatetime.AddHours(12),
                    EnumDocumentLifetimes.DayOne      => folderDatetime.AddDays(1),
                    EnumDocumentLifetimes.MonthOne    => folderDatetime.AddMonths(1),
                    EnumDocumentLifetimes.MonthsThree => folderDatetime.AddMonths(3),
                    EnumDocumentLifetimes.MonthsSix   => folderDatetime.AddMonths(6),
                    EnumDocumentLifetimes.WeekOne     => folderDatetime.AddDays(7),
                    EnumDocumentLifetimes.YearOne     => folderDatetime.AddYears(1),
                    EnumDocumentLifetimes.YearsTwo    => folderDatetime.AddYears(2),
                    EnumDocumentLifetimes.YearsThree  => folderDatetime.AddYears(3),
                    EnumDocumentLifetimes.YearsFour   => folderDatetime.AddYears(4),
                    EnumDocumentLifetimes.YearsSeven  => folderDatetime.AddYears(7),
                    EnumDocumentLifetimes.YearsTen    => folderDatetime.AddYears(10),
                    EnumDocumentLifetimes.Never       => DateTime.MaxValue,
                };
            }

            string year  = folderDatetime.ToString("yyyy");
            string month = folderDatetime.ToString("MM");


            // Retrieve Storage Node
            StorageNode storageNode = await _db.StorageNodes.SingleOrDefaultAsync(n => n.Id == storageNodeId);
            if (storageNode == null)
            {
                string nodeMsg = String.Format("{0} had a storage node [ {1} ] that could not be found.", documentType.ErrorMessage, storageNodeId);
                return Result.Fail(new Error(nodeMsg));
            }

            // Get letter for Mode.
            Result<string> modeResult = GetModeLetter(documentType.StorageMode);
            if (modeResult.IsFailed)
                return modeResult;

            string modePath = modeResult.Value;

            string path = Path.Combine(storageNode.NodePath,
                                       modePath,
                                       documentType.StorageFolderName,
                                       year,
                                       month);
            return Result.Ok(path);
        }
        catch (InvalidOperationException ex)
        {
            string msg = string.Format("ComputeStorageFolder Error for: {0}  -  had exception:{1}", documentType.ErrorMessage, ex);
            _logger.LogError("ComputeStorageFolder Error for {DocumentType}  - had exception:  {Exception}", documentType.ErrorMessage, ex);
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



    /// <summary>
    /// Returns the Mode Letter for the given StorageMode
    /// </summary>
    /// <param name="storageMode"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    internal Result<string> GetModeLetter(EnumStorageMode storageMode)
    {
        try
        {
            string modeLetter = storageMode switch
            {
                EnumStorageMode.WriteOnceReadMany => "W",
                EnumStorageMode.Editable          => "E",
                EnumStorageMode.Temporary         => "T",
                EnumStorageMode.Replaceable       => "R",
                EnumStorageMode.Versioned         => "V",
                _                                 => throw new NotSupportedException("Invalid StorageMode provided - " + storageMode.ToString())
            };
            return Result.Ok(modeLetter);
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError(ex));
        }
    }

#endregion
}