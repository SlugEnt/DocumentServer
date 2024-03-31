using Microsoft.AspNetCore.Http;

namespace SlugEnt.DocumentServer.Core;

public class RemoteDocumentStorageDto
{
    public IFormFile File;

    //public long StoredDocumentID { get; set; }

    /// <summary>
    /// This is the StoragePath component of the StoredDocument plus the filename and extension.
    /// </summary>
    public string StoragePathAndFileName { get; set; }

    /// <summary>
    /// The node this should be stored on.
    /// </summary>
    public int StorageNodeId { get; set; }
}

// StoreFileOnStorageMediaAsync
// - We need to know:
//   FileName:  storedDocument.Filename  
//   StoragePath:  From ComputeStorageFullNameAsync  OR BETTER storedDocument.StorageFolder
//   formFile:   File Bytes
//   