using System.IO.Abstractions;
using Microsoft.AspNetCore.Http;
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
    ///     This is the external (Calling) systems primary relation ID, Ie, the primary thing this is related to, for instance a Claim
    ///     #.
    ///   - Is returned when retrieving a document.
    ///   - Must be sent when saving a document.
    /// </summary>
    public string RootObjectId { get; set; }
}