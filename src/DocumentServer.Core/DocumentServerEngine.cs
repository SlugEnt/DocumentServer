using System.IO.Abstractions;
using System.Text;
using System.Xml.Linq;
using DocumentServer.Db;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using DocumentServer.Models.Enums;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using Application = DocumentServer.Models.Entities.Application;


namespace DocumentServer.Core;

/// <summary>
/// Performs all core logic operations of the DocumentServer including database access and file access methods.
/// </summary>
public class DocumentServerEngine
{
    private readonly IFileSystem        _fileSystem;
    private          DocServerDbContext _db;
    private readonly ILogger            _logger;


    /// <summary>
    /// Constructor with Dependency Injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileSystem"></param>
    public DocumentServerEngine(ILogger<DocumentServerEngine> logger, DocServerDbContext dbContext, IFileSystem fileSystem = null)
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
    public async Task<DocumentOperationStatus> StoreDocumentFirstTimeAsync(DocumentUploadDTO documentUploadDto, string storageDirectory)
    {
        bool                    fileSavedToStorage      = false;
        string                  fullFileName            = "";
        DocumentOperationStatus documentOperationStatus = null;

        try
        {
            // Generate File Description
            Guid   fileGuid = Guid.NewGuid();
            string fileName = fileGuid.ToString() + documentUploadDto.FileExtension;

            StoredDocument storedDocument = new()
            {
                Id             = fileGuid,
                Description    = documentUploadDto.Description,
                StorageFolder  = storageDirectory,
                CreatedAtUTC   = DateTime.UtcNow,
                sizeInKB       = documentUploadDto.FileBytes.Length > 1024 ? documentUploadDto.FileBytes.Length / 1024 : 1,
                DocumentTypeId = documentUploadDto.DocumentTypeId,
                Status         = EnumDocumentStatus.InitialSave
            };

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
                             documentUploadDto.FileExtension, ex.Message, ex.InnerException.Message);
            documentOperationStatus.SetError(msg);
            return documentOperationStatus;
        }
    }
}