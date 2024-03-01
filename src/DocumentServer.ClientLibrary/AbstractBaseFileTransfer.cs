using System.IO.Abstractions;
using SlugEnt.FluentResults;

namespace DocumentServer.ClientLibrary;

public abstract class AbstractBaseFileTransfer
{
    /// <summary>
    /// If this is a versioned or replacable StoredDocument and this is the latest iteration you must provide the Current StoredDocument's ID this is replacing.
    ///   - Is returned when retrieving a document.
    ///   - Is only sent when document is possibly replacing an existing one with same DocTypeExternalId
    /// </summary>
    public long CurrentStoredDocumentId { get; set; }


    /// <summary>
    ///     Type of Document This is
    ///   - Is returned when retrieving a document.
    ///   - Must be sent when saving a document.
    /// </summary>
    public int DocumentTypeId { get; set; }


    /// <summary>
    ///     The Description of the Document.
    ///   - Is returned when retrieving a document.
    ///   - Should be sent when saving a document.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     This is the external (calling) systems key or Id for this document.  Like an Invoice # or Bill #
    ///   - Is returned when retrieving a document.
    ///   - Should be sent when saving a document.
    /// </summary>
    public string? DocTypeExternalId { get; set; }

    /// <summary>
    ///     File Extension of the document
    ///   - Is NOT returned when retrieving a document.
    ///   - Should be sent when saving a document.  It is informational only
    /// </summary>
    public string FileExtension { get; set; } = "";

    /// <summary>
    ///     The File in Base64 format
    /// </summary>
    public string FileInBase64Format { get; set; }


    /// <summary>
    ///     This is the external (Calling) systems primary relation ID, Ie, the primary thing this is related to, for instance a Claim
    ///     #.
    ///   - Is returned when retrieving a document.
    ///   - Must be sent when saving a document.
    /// </summary>
    public string RootObjectId { get; set; }


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
            if (FileExtension != string.Empty)
                fileName = fileName + "." + FileExtension;
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
}