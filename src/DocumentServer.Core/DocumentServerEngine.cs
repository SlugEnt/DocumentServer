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
                string msg = String.Format("Document type [ {0} ] - [ {1} ] is not Active.", docType.Id, docType.Name);
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


            Result<string> resultB = await ComputeStorageFullNameAsync(docType, (int)docType.ActiveStorageNode1Id);
            if (resultB.IsFailed)
            {
                Result<StoredDocument> resultC = Result.Fail("Cannot save Document.");
                Result                 merged  = Result.Merge(resultB, resultC);
                return merged;
            }

            // Store the path and make sure all the paths exist.
            string storeAtPath = resultB.Value;
            _fileSystem.Directory.CreateDirectory(storeAtPath);


            // Decode the file bytes
            byte[] binaryFile;

            binaryFile   = Convert.FromBase64String(transferDocumentDto.FileInBase64Format);
            fullFileName = Path.Combine(storeAtPath, storedDocument.FileName);
            _fileSystem.File.WriteAllBytesAsync(fullFileName, binaryFile);
            fileSavedToStorage = true;


            // Save Database Entry
            storedDocument.StorageFolder = storeAtPath;

            transaction = _db.Database.BeginTransaction();

            _db.StoredDocuments.Add(storedDocument);

            //await transaction.CreateSavepointAsync("SDOC2");
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
    /// Changes the IsAlive status of a DocumentType
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <param name="isActiveValue"></param>
    /// <returns></returns>
    public async Task<Result> SetDocumentTypeActiveStatus(int documentTypeId,
                                                          bool isActiveValue)
    {
        try
        {
            DocumentType documentType = await _db.DocumentTypes.SingleAsync(d => d.Id == documentTypeId);
            if (documentType == null)
            {
                return Result.Fail("Unable to find a DocumentType with the Id of " + documentTypeId);
            }

            if (documentType.IsActive == isActiveValue)
            {
                return Result.Ok();
            }

            // Change it 
            documentType.IsActive = isActiveValue;
            await _db.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError("FAiled to change DocumentType [ {DocumentType} Active Status.  Error: {Error}", documentTypeId, ex);
            return Result.Fail(new Error("Failed to change DocumentType IsAlive Status due to error.").CausedBy(ex));
        }
    }
}