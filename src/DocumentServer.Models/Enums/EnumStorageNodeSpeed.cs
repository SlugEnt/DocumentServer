namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
///     This indicates how fast accessing a document on this node will be.
/// </summary>
public enum EnumStorageNodeSpeed
{
    /// <summary>
    ///     Fastest Access, usually SSD or equivalent
    /// </summary>
    Hot = 0,

    /// <summary>
    ///     Usually spinning disk
    /// </summary>
    Warm = 30,

    /// <summary>
    ///     File is located in the cloud, meaning access might be slower.
    /// </summary>
    CloudWarm = 31,

    /// <summary>
    ///     This is slow storage.  Used for long term archival of documents that are rarely accessed.
    /// </summary>
    Archival = 250
}