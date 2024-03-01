namespace DocumentServer.ClientLibrary;

/// <summary>
///     Dto used to upload a document that is replacing a current document
/// </summary>
public class ReplacementDto
{
    public long CurrentStoredDocumentId { get; set; }

    /// <summary>
    ///     Description of the document being stored
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     The extension for the file
    /// </summary>
    public string FileExtension { get; set; }

    /// <summary>
    ///     The File in Base64 format
    /// </summary>
    public string FileInBase64Format { get; set; }
}