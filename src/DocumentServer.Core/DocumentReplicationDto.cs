using Microsoft.AspNetCore.Http;

namespace SlugEnt.DocumentServer.Core;

public class DocumentReplicationDto
{
    public IFormFile File;

    public long StoredDocumentID { get; set; }

    public string FullFileName { get; set; }

    public int StorageNodeId { get; set; }
}

// StoreFileOnStorageMediaAsync
// - We need to know:
//   FileName:  storedDocument.Filename  
//   StoragePath:  From ComputeStorageFullNameAsync  OR BETTER storedDocument.StorageFolder
//   formFile:   File Bytes
//   