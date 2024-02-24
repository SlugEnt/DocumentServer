namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
///     Where the Storage Node stores its documents
/// </summary>
public enum EnumStorageNodeLocation
{
    /// <summary>
    ///     Documents are stored on on-premise SMB Window / Linux Server
    /// </summary>
    HostedSMB = 0,

    /// <summary>
    ///     Documents are stored on a Minio self Hosted S3 store
    /// </summary>
    S3MinioHosted = 10
}