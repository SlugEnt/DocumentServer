using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.ClientLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using SlugEnt.FluentResults;

namespace SlugEnt.DocumentServer.Core;

public class TransferDocumentContainer
{
    private string   _fileInBase64Format;
    private FormFile _fileInFormFile = null;
    private byte[]   _fileInBytes;
    private int      _fileSize;


    public TransferDocumentDto TransferDocument { get; set; }

    /// <summary>
    /// Alias for the TransferDocumentDto method
    /// </summary>
    public TransferDocumentDto TDD
    {
        get { return TransferDocument; }
        set { TransferDocument = value; }
    }


    /// <summary>
    /// Returns True if the FormFile or FileInBase64Format have been loaded with a file.
    /// </summary>
    public bool FileBytesLoaded { get; private set; }

    public bool IsInFormFileMode { get; private set; }


    /// <summary>
    ///     The File in Base64 format
    /// </summary>
    public string FileInBase64Format
    {
        get { return _fileInBase64Format; }
        set
        {
            _fileInBase64Format = value;
            FileBytesLoaded     = _fileInBase64Format.Length > 0 ? true : false;
            IsInFormFileMode    = false;
            _fileSize           = value.Length;
        }
    }


    /// <summary>
    ///  The File in FormFile format.
    /// </summary>
    public FormFile FileInFormFile
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



    /*
    /// <summary>
    ///     Reads the specified file into the FileInBase64Format property.
    /// </summary>
    /// <param name="fileName">Full path and name of file to read in</param>
    /// <param name="fileSystem">Should be set to Mock File System in Unit Testing.  Leave null or empty for real use scenarios</param>
    /// <returns></returns>
    public Result ReadFileIn(string fileName,
                             IFileSystem fileSystem = null)
    {
        try
        {
            FileInBase64Format = Convert.ToBase64String(File.ReadAllBytes(fileName));
            return Result.Ok();
        }
        catch (Exception ex)
        {
            string msg = string.Format("Error reading File: {0}.", fileName);
            return Result.Fail(new Error(msg).CausedBy(ex));
        }
    }


    /// <summary>
    ///     Saves the file.  The Extension is automatically added
    /// </summary>
    /// <param name="pathToSaveTo"></param>
    /// <param name="fileName"></param>
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public bool SaveFile(string pathToSaveTo,
                         string fileName,
                         IFileSystem fileSystem)
    {
        try
        {
            byte[] binaryFile = Convert.FromBase64String(FileInBase64Format);
            if (TransferDocument.FileExtension != string.Empty)
                fileName = fileName + "." + TransferDocument.FileExtension;
            string fullFileName = Path.Combine(pathToSaveTo, fileName);
            fileSystem.File.WriteAllBytesAsync(fullFileName, binaryFile);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error Saving file - {0}", ex.Message);
            return false;
        }
    }
    */
}