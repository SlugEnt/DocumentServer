using Microsoft.AspNetCore.Http;

namespace SlugEnt.DocumentServer.Core;

public class RemoteDocumentStorageDto
{
    //public long StoredDocumentID { get; set; }

    /// <summary>
    /// This is the StoragePath component of the StoredDocument
    /// </summary>
    public string StoragePath { get; set; }

    /// <summary>
    /// The file name and extension of the file to be stored remotely.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// The node this should be stored on.
    /// </summary>
    public int StorageNodeId { get; set; }

    public IFormFile File { get; set; }
}