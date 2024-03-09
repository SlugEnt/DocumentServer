using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;

[assembly: InternalsVisibleTo("Test_DocumentServer")]

namespace DocumentServer.Core;

/// <summary>
///     Performs all core logic operations of the DocumentServer including database access and file access methods.
/// </summary>
public class DocumentServerEngine
{
    private readonly IFileSystem               _fileSystem;
    private readonly ILogger                   _logger;
    private          DocServerDbContext        _db;
    private          DocumentServerInformation _documentServerInformation;


    /// <summary>
    ///     Constructor with Dependency Injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileSystem"></param>
    public DocumentServerEngine(ILogger<DocumentServerEngine> logger,
                                DocServerDbContext dbContext,
                                DocumentServerInformation documentServerInformation,
                                IFileSystem? fileSystem = null)
    {
        _logger                    = logger;
        _db                        = dbContext;
        _documentServerInformation = documentServerInformation;

        if (fileSystem != null)
            _fileSystem = fileSystem;

        // Use the real file system
        else
            _fileSystem = new FileSystem();
    }



    /// <summary>
    ///     Loads the specified DocumentType and validates some values before returning.
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <returns>Result.Success if good, Result.Fail if bad</returns>
    internal async Task<Result<DocumentType>> LoadDocumentType_ForSavingStoredDocument(int documentTypeId)
    {
        // Retrieve the DocumentType
        DocumentType? docType = await _db.DocumentTypes
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
        ExpiringDocument? expiring = null;

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


    /// <summary>
    /// Retrieves a Stored Document
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Result<TransferDocumentContainer>> GetStoredDocumentAsync(long id,
                                                                                string appToken)
    {
        TransferDocumentContainer transferDocumentContainer = new();
        try
        {
            // Verify the Application Token is correct.
            if (!_documentServerInformation.ApplicationTokenLookup.TryGetValue(appToken, out Application application))
                return Result.Fail("Invalid Application Token provided.");


            //StoredDocument? storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(s => s.Id == id);
            var storedDocument = await _db.StoredDocuments.Where(sd => sd.Id == id).Select(s => new
            {
                StoredDocument = s,
                ApplicationId  = s.DocumentType.ApplicationId
            }).FirstOrDefaultAsync();

            if (storedDocument == null)
                return Result.Fail("Unable to find a Stored Document with that Id");

            if (storedDocument.ApplicationId != application.Id)
                return Result.Fail("Application Token provided does not match the application Id of the Stored Document requested.  Access Denied");


            // Now Load the Stored Document.
            string fileName     = storedDocument.StoredDocument.FileName;
            string fullFileName = Path.Join(storedDocument.StoredDocument.StorageFolder, fileName);


            transferDocumentContainer.FileInBytes = _fileSystem.File.ReadAllBytes(fullFileName);
            string extension = Path.GetExtension(fileName);
            if (extension == string.Empty)
                extension = MediaTypes.GetExtension(storedDocument.StoredDocument.MediaType);
            else

                // Strip the returned period.
                extension = extension.Substring(1);


            // Load the TransferDocument info
            transferDocumentContainer.TransferDocument = new TransferDocumentDto()
            {
                Description             = storedDocument.StoredDocument.Description,
                CurrentStoredDocumentId = storedDocument.StoredDocument.Id,
                DocumentTypeId          = storedDocument.StoredDocument.DocumentTypeId,
                DocTypeExternalId       = storedDocument.StoredDocument.DocTypeExternalKey,
                RootObjectId            = storedDocument.StoredDocument.RootObjectExternalKey,
                MediaType               = storedDocument.StoredDocument.MediaType,
                FileExtension           = extension,
            };


            // TODO update the number of times accessed

            return Result.Ok(transferDocumentContainer);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStoredDocumentFileBytesAsync:  DocumentId [{DocumentId} ]Exception:  {Error}", id, ex.Message);
            return Result.Fail(new Error("Unable to read document from library.").CausedBy(ex));
        }
    }



    /// <summary>
    ///     Replaces a Replaceable document with a new one.  Marks for deletion the old one.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> ReplaceDocument(TransferDocumentDto replacementDto)
    {
        // Lets read current stored document
        StoredDocument? storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentStoredDocumentId);
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
    public async Task<Result<StoredDocument>> StoreDocumentFirstTimeAsync(TransferDocumentContainer transferDocumentContainer,
                                                                          string applicationToken)
    {
        IDbContextTransaction?  transaction             = null;
        bool                    fileSavedToStorage      = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = new();
        Result<StoredDocument>  result                  = new();
        try
        {
            // Verify the Application Token is correct.
            if (!_documentServerInformation.ApplicationTokenLookup.TryGetValue(applicationToken, out Application application))
                return Result.Fail("Invalid Application Token provided.");

            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> docTypeResult = await LoadDocumentType_ForSavingStoredDocument(transferDocumentContainer.TransferDocument.DocumentTypeId);
            if (docTypeResult.IsFailed)
                return Result.Merge(result, docTypeResult);

            DocumentType docType = docTypeResult.Value;


            // Make sure the DocType application matches the application the token was for.
            if (docType.ApplicationId != application.Id)
                return Result.Fail("The document type requested is not a member of the application you provided a token for.  Access denied.");


            // We always use the primary node for initial storage.

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
    internal static void SetMediaType(EnumMediaTypes mediaType,
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
    /// <param name="formFile">Contents of the file</param>
    /// <returns>Result string where string is the full file name </returns>
    internal async Task<Result<string>> StoreFileOnStorageMediaAsync(StoredDocument storedDocument,
                                                                     DocumentType documentType,
                                                                     int storageNodeId,
                                                                     IFormFile formFile)
    {
        string fullFileName = "";

        try
        {
            Result<StoragePathInfo> resultB = await ComputeStorageFullNameAsync(documentType, storageNodeId);
            if (resultB.IsFailed)
                return Result.Fail(new Error("Failed to compute Where the document should be stored.").CausedBy(resultB.Errors));

            // Store the path and make sure all the paths exist.
            string storeAtPath = resultB.Value.ActualPath;
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

        IDbContextTransaction? transaction  = null;
        string                 fullFileName = "";
        bool                   docSaved     = false;
        string                 oldFileName  = "";
        Result<StoredDocument> result       = new();
        try
        {
            // We need to load the existing StoredDocument to retrieve some info.
            StoredDocument? storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.TransferDocument.CurrentStoredDocumentId);
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


    /// <summary>
    /// Saves the requested application object to the database.  It creates the Token prior to save and returns its value.
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    public async Task<string> ApplicationSave(Application application)
    {
        string guid = Guid.NewGuid().ToString();
        application.Token = guid;

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        return guid;
    }



#region "Support Functions"

    /// <summary>
    ///     Computes the complete path including the actual file name.  Note.  Does not include the HostName Path part.
    ///     All files are stored in a folder by
    ///     storageNodePath\StorageModeLetter\DocumentTypePath\year\month
    /// </summary>
    /// <param name="documentType">The DocumentType</param>
    /// <param name="storageNodeId">The StorageNode Id to store on</param>
    /// <returns>Result</returns>
    internal async Task<Result<StoragePathInfo>> ComputeStorageFullNameAsync(DocumentType documentType,
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
            //StorageNode storageNode = await _db.StorageNodes.SingleOrDefaultAsync(n => n.Id == storageNodeId);
            StorageNode? storageNode = await _db.StorageNodes
                                                .Include(sh => sh.ServerHost).SingleOrDefaultAsync(n => n.Id == storageNodeId);
            if (storageNode == null)
            {
                string nodeMsg = string.Format("{0} had a storage node [ {1} ] that could not be found.", documentType.ErrorMessage, storageNodeId);
                return Result.Fail(new Error(nodeMsg));
            }

            if (storageNode.ServerHost == null)
            {
                string nodeMsg = string.Format("{0} had a server host that was null.  It must exist.", documentType.ErrorMessage);
                return Result.Fail(new Error(nodeMsg));
            }


            // Get letter for Mode.
            Result<string> modeResult = GetModeLetter(documentType.StorageMode);
            if (modeResult.IsFailed)
                return Result.Fail(new Error("Failed to get Mode for StorageMode").CausedBy(modeResult.Errors));

            string modePath = modeResult.Value;


            StoragePathInfo storagePathInfo = new();
            storagePathInfo.StoredDocumentPath = Path.Combine(storageNode.NodePath,
                                                              modePath,
                                                              documentType.StorageFolderName,
                                                              year,
                                                              month);

            Result<string> resultCP = ComputePhysicalStoragePath(storageNode.ServerHost, storageNode, storagePathInfo.StoredDocumentPath);
            if (resultCP.IsFailed)
                return Result.Fail(new Error("Failed to determine host path").CausedBy(resultCP.Errors));

            storagePathInfo.ActualPath = resultCP.Value;
            return Result.Ok(storagePathInfo);
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
    /// Computes the physical location on the node that the document is stored at.
    /// </summary>
    /// <param name="serverHost"></param>
    /// <param name="documentPath"></param>
    /// <returns></returns>
    internal Result<string> ComputePhysicalStoragePath(ServerHost serverHost,
                                                       StorageNode node,
                                                       string documentPath)
    {
        if (_documentServerInformation.ServerHostInfo.ServerHostId != serverHost.Id)
        {
            string msg = String.Format("This host is not the host that can write for this StorageNode.  Host: [ " + _documentServerInformation.ServerHostInfo.ServerHostName +
                                       " ]  Requested StorageNode [ " + node.Name + " - " + node.Id + " ]");
            _logger.LogCritical(msg);
            return Result.Fail(msg);
        }

        return Result.Ok(Path.Join(serverHost.Path, documentPath));
    }



    /// <summary>
    ///     Returns the Mode Letter for the given StorageMode
    /// </summary>
    /// <param name="storageMode"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    internal static Result<string> GetModeLetter(EnumStorageMode storageMode)
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