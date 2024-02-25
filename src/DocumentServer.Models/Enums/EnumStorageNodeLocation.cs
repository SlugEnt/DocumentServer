using System.ComponentModel.DataAnnotations;

namespace SlugEnt.DocumentServer.Models.Enums;

/// <summary>
///     Where the Storage Node stores its documents
/// </summary>
public enum EnumStorageNodeLocation
{
    /// <summary>
    ///     Documents are stored on on-premise SMB Window / Linux Server
    /// </summary>
    [Display(Description = "On Premise SMB")]
    HostedSMB = 0,

    /// <summary>
    ///     Documents are stored on a Minio self Hosted S3 store
    /// </summary>
    [Display(Description = "S3 On Premise")]
    S3MinioHosted = 10
}