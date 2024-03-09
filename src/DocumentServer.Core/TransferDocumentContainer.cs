using Microsoft.AspNetCore.Http;
using SlugEnt.DocumentServer.ClientLibrary;

namespace SlugEnt.DocumentServer.Core;

/// <summary>
/// This is an internal container used by the Document Storage Engine to receive and send a file across the internet.  It also has a TransferDocumentDto to store file info.
/// </summary>
public class TransferDocumentContainer
{
    private IFormFile _fileInFormFile = null;
    private byte[]    _fileInBytes;
    private int       _fileSize;


    public TransferDocumentDto TransferDocument { get; set; }



    /// <summary>
    /// Returns True if the FormFile or FileInBase64Format have been loaded with a file.
    /// </summary>
    public bool FileBytesLoaded { get; private set; }

    public bool IsInFormFileMode { get; private set; }


    /// <summary>
    ///  The File in FormFile format.
    /// </summary>
    public IFormFile FileInFormFile
    {
        get { return _fileInFormFile; }
        set
        {
            _fileInFormFile  = value;
            FileBytesLoaded  = value != null ? true : false;
            IsInFormFileMode = true;
            _fileSize        = (int)value.Length;
        }
    }


    public Byte[] FileInBytes
    {
        get { return _fileInBytes; }
        set
        {
            _fileInBytes = value;
            _fileSize    = value.Length;
        }
    }

    /// <summary>
    /// Returns the size of the file.
    /// </summary>
    public int FileSize
    {
        get { return _fileSize; }
    }
}