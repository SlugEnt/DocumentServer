namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
/// Tells the status of a Document.
/// </summary>
public enum EnumDocumentStatus
{
    /// <summary>
    /// Set when the document is first stored in the server
    /// </summary>
    InitialSave = 1,

    /// <summary>
    /// Indicates the document is waiting to be security checked before being released
    /// </summary>
    NeedsSecurityCheck = 10,

    /// <summary>
    /// Document is categorized, passed security checks and can now be retrieved.
    /// </summary>
    Available = 100,

    /// <summary>
    /// Document is available, but has been updated at some point in time.
    /// </summary>
    Updated = 105,

    /// <summary>
    /// Document has reached archival time period and is awaiting being moved to archive locations
    /// </summary>
    AwaitingArchival = 199,

    /// <summary>
    /// Document has been moved to archival storage
    /// </summary>
    Archived = 200,

    /// <summary>
    /// Document has been marked to be deleted, but is waiting for the deletion time out period.
    /// </summary>
    AwaitingDeletion = 245,

    /// <summary>
    /// Document has been deleted from file system
    /// </summary>
    Deleted = 249,

    /// <summary>
    /// Document has been removed, meaning it is no longer available and is waiting for the archival time period to elapse before removing it from the DocumentServer database
    /// </summary>
    Removed = 253
}