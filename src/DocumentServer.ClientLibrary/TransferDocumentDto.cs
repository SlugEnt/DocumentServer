using SlugEnt.DocumentServer.Models.Enums;
using System.IO.Abstractions;

namespace SlugEnt.DocumentServer.ClientLibrary;

/// <summary>
///     This class is used to send a document to the DocumentServer for storage.
/// </summary>
public class TransferDocumentDto
{
    /// <summary>
    /// The token that corresponds to the application this document is a member of.
    /// </summary>
    // public string ApplicationToken { get; set; } = "";


    /// <summary>
    /// If this is a versioned or replacable StoredDocument and this is the latest iteration you must provide the Current StoredDocument's ID this is replacing.
    ///   - Is returned when retrieving a document.
    ///   - Is only sent when document is possibly replacing an existing one with same DocTypeExternalId
    /// </summary>
    public long CurrentStoredDocumentId { get; set; } = 0;


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
    public string? Description { get; set; } = "";

    /// <summary>
    ///     This is the external (calling) systems key or Id for this document.  Like an Invoice # or Bill #
    ///   - Is returned when retrieving a document.
    ///   - Should be sent when saving a document.
    /// </summary>
    public string? DocTypeExternalId { get; set; } = "";

    /// <summary>
    ///     File Extension of the document
    ///   - Is NOT returned when retrieving a document.
    ///   - Should be sent when saving a document.  It is informational only
    /// </summary>
    public string? FileExtension { get; set; } = "";

    /// <summary>
    ///     This is the external (Calling) systems primary relation ID, Ie, the primary thing this is related to, for instance a Claim
    ///     #.
    ///   - Is returned when retrieving a document.
    ///   - Must be sent when saving a document.
    /// </summary>
    public string RootObjectId { get; set; } = "";


    /// <summary>
    /// This is essentially the type of physical document this is (jpg, png, pdf, doc, xls, etc).
    /// <para>This is optional.  If not specified the File Extension is used.</para>
    /// </summary>
    public EnumMediaTypes MediaType { get; set; }


    //public byte[] fileBytes { get; set; }

    public IFormFile File { get; set; }



#region "SupportMethods"

    /// <summary>
    /// Creates a FormFile object from a physical file
    /// </summary>
    /// <param name="fullFileName"></param>
    /// <returns></returns>
    public static IFormFile GetFormFile(string fullFileName,
                                        string mediaTypeNameValue,
                                        IFileSystem fileSystem = null)
    {
        IFormFile file;
        Stream    stream = new FileStream(fullFileName, FileMode.Open);


        file = new FormFile(stream,
                            0,
                            stream.Length,
                            null,
                            Path.GetFileName(fullFileName))
        {
            Headers     = new HeaderDictionary(),
            ContentType = mediaTypeNameValue,
        };


        return file;
    }


    /// <summary>
    /// Creates a FormFile from a Byte Array
    /// </summary>
    /// <param name="fileBytes"></param>
    /// <param name="mediaType">The media type the file is</param>
    /// <returns></returns>
    public static IFormFile GetFormFile(byte[] fileBytes,
                                        string mediaTypeNameValue)
    {
        FormFile     file;
        MemoryStream ms = new(fileBytes);
        file = new FormFile(ms,
                            0,
                            ms.Length,
                            null,
                            "somefile")
        {
            Headers     = new HeaderDictionary(),
            ContentType = mediaTypeNameValue,
        };

        ms.Seek(0, SeekOrigin.Begin);


        file.OpenReadStream();
        return file;
    }

#endregion
}