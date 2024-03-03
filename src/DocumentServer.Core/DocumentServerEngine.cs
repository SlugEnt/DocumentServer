using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;
using FileInfo = SlugEnt.DocumentServer.ClientLibrary.FileInfo;

[assembly: InternalsVisibleTo("Test_DocumentServer")]

namespace DocumentServer.Core;

/// <summary>
///     Performs all core logic operations of the DocumentServer including database access and file access methods.
/// </summary>
public class DocumentServerEngine
{
    private readonly IFileSystem                  _fileSystem;
    private readonly ILogger                      _logger;
    private          DocServerDbContext           _db;
    private          Dictionary<int, StorageNode> _storageNodes;


    /// <summary>
    ///     Constructor with Dependency Injection
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
    ///     Sets the Database to use
    /// </summary>
    public DocServerDbContext DocumentServerDatabase
    {
        set => _db = value;
    }



    /// <summary>
    ///     Loads the specified DocumentType and validates some values before returning.
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <returns>Result.Success if good, Result.Fail if bad</returns>
    internal async Task<Result<DocumentType>> LoadDocumentType_ForSavingStoredDocument(int documentTypeId)
    {
        // Retrieve the DocumentType
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
            string msg = string.Format("Document type [ {0} ] - [ {1} ] is not Active.", docType.Id, docType.Name);
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
    ///     This is logic that is called before saving a StoredDocument, either first save or subsequent update saves.
    ///     The StoredDocument is saved in this method.
    /// </summary>
    /// <param name="documentType"></param>
    /// <param name="storedDocument"></param>
    /// <param name="isFirstSave"></param>
    /// <returns></returns>
    private async Task<Result> PreSaveEdits(DocumentType documentType,
                                            StoredDocument storedDocument,
                                            bool isFirstSave = true)
    {
        ExpiringDocument expiring = null;

        if (isFirstSave)
            if (documentType.StorageMode == EnumStorageMode.Temporary)
            {
                // Since this is a temporary document we immediately mark it as inactive to start its lifetime counter.
                storedDocument.IsAlive = false;

                // Need to add an entry to the ExpiringDocuments table
                Result<ExpiringDocument> expResult = ExpiringDocument.Create(documentType.InActiveLifeTime);
                if (expResult.IsFailed)
                    return Result.Fail(expResult.Errors);

                expiring = expResult.Value;
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

        return Result.Ok();
    }



    public async Task<Result<TransferDocumentContainer>> GetStoredDocumentAsync(long id)
    {
        TransferDocumentContainer transferDocument = new();
        try
        {
            StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(s => s.Id == id);
            if (storedDocument == null)
                return Result.Fail("Unable to find a Stored Document with that Id");


            // Now Load the Stored Document.
            string fileName     = storedDocument.FileName;
            string fullFileName = Path.Join(storedDocument.StorageFolder, fileName);

            transferDocument.FileInBytes = _fileSystem.File.ReadAllBytes(fullFileName);
            string extension = Path.GetExtension(fileName);
            if (extension == string.Empty)
                extension = MediaTypes.GetExtension(storedDocument.MediaType);


            // Load the TransferDocument info
            transferDocument.TransferDocument = new TransferDocumentDto()
            {
                Description             = storedDocument.Description,
                CurrentStoredDocumentId = storedDocument.Id,
                DocumentTypeId          = storedDocument.DocumentTypeId,
                DocTypeExternalId       = storedDocument.DocTypeExternalKey,
                RootObjectId            = storedDocument.RootObjectExternalKey,
                MediaType               = storedDocument.MediaType,
                FileExtension           = extension,
            };


            // TODO update the number of times accessed

            return Result.Ok(transferDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStoredDocumentFileBytesAsync:  DocumentId [{DocumentId} ]Exception:  {Error}", id, ex.Message);
            return Result.Fail(new Error("Unable to read document from library.").CausedBy(ex));
        }
    }



    /// <summary>
    ///     Reads a document from the library and returns it to the caller.  Returns the File Bytes Only
    /// </summary>
    /// <param name="Id"></param>
    /// <returns>Result.Success and the file in Base64  or returns Result.Fail with error message</returns>
    [Obsolete]
    public async Task<Result<string>> GetStoredDocumentFileBytesAsync(long Id)
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
            _logger.LogError("GetStoredDocumentFileBytesAsync:  DocumentId [{DocumentId} ]Exception:  {Error}", Id, ex.Message);
            return Result.Fail(new Error("Unable to read document from library.").CausedBy(ex));
        }
    }



    /// <summary>
    ///     Replaces a Replaceable document with a new one.  Marks for deletion the old one.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> ReplaceDocument(ReplacementDto replacementDto)
    {
        // Lets read current stored document
        StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentStoredDocumentId);
        if (storedDocument == null)
            return Result.Fail(new Error("Unable to find existing StoredDocument with Id [ " + replacementDto.CurrentStoredDocumentId + " ]"));


        return Result.Ok(storedDocument);
    }


#region "RootObject Code"

    /// <summary>
    ///     Marks a RootObject as InActive.
    /// </summary>
    /// <param name="rootObjectId"></param>
    /// <returns></returns>
    public async Task<bool> RootObjectDeleteAsync(int rootObjectId)
    {
        int rowsAffected = await _db.RootObjects.Where(ro => ro.Id == rootObjectId).ExecuteUpdateAsync(s => s.SetProperty(ro => ro.IsActive, false));
        if (rowsAffected > 0)
            return true;

        return false;
    }

#endregion



    /// <summary>
    /// Changes the Alive Status for a Document Type
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
                return Result.Fail("Unable to find a DocumentType with the Id of " + documentTypeId);

            if (documentType.IsActive == isAliveValue)
                return Result.Ok();

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



    /// <summary>
    ///     Stores a document to the Storage Engine and database, if it is a new document
    /// </summary>
    /// <param name="transferDocumentDto">The file info to be stored</param>
    /// <param name="storageDirectory">Where to save it to.</param>
    /// <returns>StoredDocument if successful.  Returns Null if it failed.</returns>
    public async Task<Result<StoredDocument>> StoreDocumentFirstTimeAsync(TransferDocumentContainer transferDocumentContainer)
    {
        IDbContextTransaction   transaction             = null;
        bool                    fileSavedToStorage      = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = new();
        Result<StoredDocument>  result                  = new();
        try
        {
            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> docTypeResult = await LoadDocumentType_ForSavingStoredDocument(transferDocumentContainer.TransferDocument.DocumentTypeId);
            if (docTypeResult.IsFailed)
                return Result.Merge(result, docTypeResult);

            DocumentType docType = docTypeResult.Value;


            // We always use the primary node for initial storage.
            // TODO This is going to need to be fixed now that we are not passing file bytes around...
            int tmpFileSize = transferDocumentContainer.FileSize;
            int fileSize    = tmpFileSize > 1024 ? tmpFileSize / 1024 : 1;

            if (string.IsNullOrWhiteSpace(transferDocumentContainer.TransferDocument.RootObjectId))
                return Result.Fail(new Error("No RootObjectId was specified on the transferDocumentContainer.  It is required."));

            if (string.IsNullOrWhiteSpace(transferDocumentContainer.TransferDocument.DocTypeExternalId))
            {
                // Ensure it is null.
                transferDocumentContainer.TransferDocument.DocTypeExternalId = null;
            }
            else
            {
                // Make sure we are following the rule for not allowing duplicates if it is set.
                if (!docType.AllowSameDTEKeys)
                {
                    // Make sure the external key does not already exist.
                    bool exists = _db.StoredDocuments.Any(sd => sd.RootObjectExternalKey == transferDocumentContainer.TransferDocument.RootObjectId &&
                                                                sd.DocTypeExternalKey == transferDocumentContainer.TransferDocument.DocTypeExternalId);
                    if (exists)
                    {
                        string msg =
                            string.Format("Duplicate Key not allowed.  RootObject Id [ {0} ] already has a DocumentType [ {1} ]  with an External Id of [ {2} ].  This DocumentType does not allow duplicate entries.",
                                          transferDocumentContainer.TransferDocument.RootObjectId,
                                          transferDocumentContainer.TransferDocument.DocumentTypeId,
                                          transferDocumentContainer.TransferDocument.DocTypeExternalId);
                        return Result.Fail(new Error(msg));
                    }
                }
            }

            StoredDocument storedDocument = new(transferDocumentContainer.TransferDocument.FileExtension,
                                                transferDocumentContainer.TransferDocument.Description,
                                                transferDocumentContainer.TransferDocument.RootObjectId,
                                                transferDocumentContainer.TransferDocument.DocTypeExternalId,
                                                "",
                                                fileSize,
                                                transferDocumentContainer.TransferDocument.DocumentTypeId,
                                                (int)docType.ActiveStorageNode1Id);
            SetMediaType(transferDocumentContainer.TransferDocument.MediaType, transferDocumentContainer.TransferDocument.FileExtension, storedDocument);


            // Store the document on the storage media
            Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                            docType,
                                                                            (int)docType.ActiveStorageNode1Id,
                                                                            transferDocumentContainer.FileInFormFile);
            if (storeResult.IsFailed)
                return Result.Merge(result, storeResult);

            fullFileName       = storeResult.Value;
            fileSavedToStorage = true;


            // Save StoredDocument
            transaction = _db.Database.BeginTransaction();
            _db.StoredDocuments.Add(storedDocument);

            // TODO PreSaveEdits should return a Result
            Result preSaveResult = await PreSaveEdits(docType, storedDocument);
            if (preSaveResult.IsFailed)
                return preSaveResult;

            _db.Database.CommitTransaction();

            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            await _db.Database.RollbackTransactionAsync();


            // Delete the file from storage if we successfully saved it, but failed afterward.
            if (fileSavedToStorage)
                _fileSystem.File.Delete(fullFileName);

            string msg =
                string.Format($"StoreDocument:  Failed to store the document:  Description: {transferDocumentContainer.TransferDocument.Description}, Extension: {transferDocumentContainer.TransferDocument.FileExtension}.  Error Was: {ex.Message}.  ");

            StringBuilder sb = new();
            sb.Append(msg);
            if (ex.InnerException != null)
                sb.Append(ex.InnerException.Message);
            msg = sb.ToString();

            _logger.LogError("StoreDocument: Failed to store document.{Description}{Extension}Error:{Error}.",
                             transferDocumentContainer.TransferDocument.Description,
                             transferDocumentContainer.TransferDocument.FileExtension,
                             msg);
            Result errorResult = Result.Fail(new Error("Failed to store the document due to errors.").CausedBy(ex));
            return errorResult;
        }
    }


    /// <summary>
    /// Determines and sets the Media Type based upon the passed MediaType or Extension.  
    /// </summary>
    /// <param name="storedDocument"></param>
    internal void SetMediaType(EnumMediaTypes mediaType,
                               string fileExtension,
                               StoredDocument storedDocument)
    {
        // Use user specified Media Type if available.
        if (mediaType != EnumMediaTypes.NotSpecified)
        {
            storedDocument.MediaType = mediaType;
            return;
        }

        // Figure out based upon extension
        storedDocument.MediaType = MediaTypes.GetMediaType(fileExtension.ToLower());
    }


    /// <summary>
    ///     Determines where to store the file and then stores it.
    /// </summary>
    /// <param name="storedDocument">The StoredDocument that will be updated with path info</param>
    /// <param name="documentType">DocumentType this document is</param>
    /// <param name="storageNodeId">Node Id to store file at</param>
    /// <param name="fileInBase64Format">Contents of the file</param>
    /// <returns>Result string where string is the full file name </returns>
    [Obsolete]
    internal async Task<Result<string>> StoreFileOnStorageMediaAsync(StoredDocument storedDocument,
                                                                     DocumentType documentType,
                                                                     int storageNodeId,
                                                                     string fileInBase64Format)
    {
        throw new NotImplementedException();

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

            // Only if file is in bytes.
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
    ///     Determines where to store the file and then stores it.
    /// </summary>
    /// <param name="storedDocument">The StoredDocument that will be updated with path info</param>
    /// <param name="documentType">DocumentType this document is</param>
    /// <param name="storageNodeId">Node Id to store file at</param>
    /// <param name="formFile">Contents of the file</param>
    /// <returns>Result string where string is the full file name </returns>
    internal async Task<Result<string>> StoreFileOnStorageMediaAsync(StoredDocument storedDocument,
                                                                     DocumentType documentType,
                                                                     int storageNodeId,
                                                                     IFormFile formFile)
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
            fullFileName = Path.Combine(storeAtPath, storedDocument.FileName);
            using (Stream fs = _fileSystem.File.Create(fullFileName))
            {
                await formFile.CopyToAsync(fs);
            }

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
    ///     Stores a new document that is a replacement for an existing document.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> StoreReplacementDocumentAsync(TransferDocumentContainer replacementDto)
    {
        // The replacement process is:
        // - Store new file
        // - Update StoredDocument
        // - Delete Old File

        IDbContextTransaction  transaction  = null;
        string                 fullFileName = "";
        bool                   docSaved     = false;
        string                 oldFileName  = "";
        Result<StoredDocument> result       = new();
        try
        {
            // We need to load the existing StoredDocument to retrieve some info.
            StoredDocument storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.TransferDocument.CurrentStoredDocumentId);
            if (storedDocument == null)
            {
                string msg = string.Format("Unable to find existing StoredDocument with Id [ {0} ]", replacementDto.TransferDocument.CurrentStoredDocumentId);
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
            storedDocument.ReplaceFileName(replacementDto.TransferDocument.FileExtension);
            Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                            docType,
                                                                            (int)docType.ActiveStorageNode1Id,
                                                                            replacementDto.FileInFormFile);
            if (storeResult.IsFailed)
                return Result.Merge(result, storeResult);

            fullFileName = storeResult.Value;

            // Save StoredDocument
            //  - Update description
            if (!string.IsNullOrEmpty(replacementDto.TransferDocument.Description))
                storedDocument.Description = replacementDto.TransferDocument.Description;

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


#region "Support Functions"

    /// <summary>
    ///     Loads / Reloads the master data tables.
    /// </summary>
    /// <returns></returns>
    public async Task LoadMasterDataTables()
    {
        Dictionary<int, StorageNode> tempStorageNodes = new();
    }


    /// <summary>
    ///     Computes the complete path including the actual file name.
    ///     All files are stored in a folder by
    ///     storageNodePath\StorageModeLetter\DocumentTypePath\year\month
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
                    _                                 => throw new Exception("Unknown DocumentLifetime value of [ " + documentType.InActiveLifeTime + " ]")
                };

            string year  = folderDatetime.ToString("yyyy");
            string month = folderDatetime.ToString("MM");


            // Retrieve Storage Node
            StorageNode storageNode = await _db.StorageNodes.SingleOrDefaultAsync(n => n.Id == storageNodeId);
            if (storageNode == null)
            {
                string nodeMsg = string.Format("{0} had a storage node [ {1} ] that could not be found.", documentType.ErrorMessage, storageNodeId);
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
                string.Format("ComputeStorageFolder error for {0} - Storage Node Id: {1} -   {2}",
                              documentType.Id,
                              storageNodeId,
                              ex.Message);
            return Result.Fail(msg);
        }
    }



    /// <summary>
    ///     Returns the Mode Letter for the given StorageMode
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
                _                                 => throw new NotSupportedException("Invalid StorageMode provided - " + storageMode)
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