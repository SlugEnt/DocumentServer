using System.IO.Abstractions;

namespace DocumentServer.ClientLibrary;

/// <summary>
/// This object is used to transfer an actual file back and forth from the Document Server
/// </summary>
public class TransferDocumentDto
{
    /// <summary>
    /// The Description of the Document
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///  File Extension of the document
    /// </summary>
    public string FileExtension { get; set; } = "";

    /// <summary>
    /// The File in Base64 format
    /// </summary>
    public string FileInBase64Format { get; set; }

    /// <summary>
    /// Type of Document This is
    /// </summary>
    public int DocumentTypeId { get; set; }


    /// <summary>
    /// Reads the specified file into the FileInBase64Format property.
    /// </summary>
    /// <param name="fileName">Full path and name of file to read in</param>
    /// <param name="fileSystem">Should be set to Mock File System in Unit Testing.  Leave null or empty for real use scenarios</param>
    /// <returns></returns>
    public bool ReadFileIn(string fileName,
                           IFileSystem fileSystem = null)
    {
        try
        {
            FileInBase64Format = Convert.ToBase64String(File.ReadAllBytes(fileName));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading file - {0}", fileName);
        }

        return false;
    }


    /// <summary>
    /// Saves the file.  The Extension is automatically added
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