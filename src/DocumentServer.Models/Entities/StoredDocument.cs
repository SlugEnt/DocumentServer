using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Models.Enums;

namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
///     The StoredDocument is the database entity that stores information about a single stored document.
///     The document itself is saved on a file system with the name  Id.FileExtension
///     It is thus vital that FileExtension and Id are never changed.
///     <remarks>
///     A Stored Document can only exist on a maximum of 2 storage nodes at one time (This may change in future)
///     A Stored Document can however move to different nodes during its lifetime.  
/// </remarks>
/// </summary>
[Index(nameof(RootObjectExternalKey), nameof(DocTypeExternalKey), Name = "IDX_Ext_Keys")]
public class StoredDocument : AbstractBaseEntity
{
    /// <summary>
    ///     Empty Constructor - Prefer the normal
    /// </summary>
    public StoredDocument() { }


    /// <summary>
    ///     Normal Constructor
    /// </summary>
    public StoredDocument(string fileExtension,
                          string description,
                          string rootObjectExternalKey,
                          string? docTypeExternalKey,
                          string storageFolder,
                          int sizeInKB,
                          int documentTypeId,
                          int primaryStorageNodeId,
                          int? secondaryStorageNodeId = null) : this()
    {
        SetFileName(fileExtension);

        //FileName = new Guid().ToString() + fileExtension == string.Empty ? string.Empty : "." + fileExtension;
        //FileExtension  = fileExtension;
        Description           = description;
        StorageFolder         = storageFolder;
        SizeInKB              = sizeInKB;
        DocumentTypeId        = documentTypeId;
        RootObjectExternalKey = rootObjectExternalKey;
        DocTypeExternalKey    = docTypeExternalKey;

        Status                 = EnumDocumentStatus.InitialSave;
        NumberOfTimesAccessed  = 0;
        PrimaryStorageNodeId   = primaryStorageNodeId;
        SecondaryStorageNodeId = secondaryStorageNodeId;
    }


    /// <summary>
    ///     A readable name or description for the document.. Maximum length of 250
    /// </summary>
    [MaxLength(250)]
    public string Description { get; set; }


    /// <summary>
    ///     This is the Id in the calling application for this particular Document Type.  For instance it could be an invoice
    ///     Number.
    /// </summary>
    public string? DocTypeExternalKey { get; set; }

    [Required] public DocumentType DocumentType { get; set; }

    // Relationships


    // Each document is associated with a Document Type
    public int DocumentTypeId { get; set; }


    /// <summary>
    ///     For displaying information about this in an error type message
    /// </summary>
    [NotMapped]
    public string ErrorMessage
    {
        get
        {
            string className = GetType().Name;
            string msg = string.Format("{0}:  [Id: {1} | Name: {2} ]",
                                       className,
                                       Id,
                                       FileName);
            return msg;
        }
    }

    /// <summary>
    ///     Filename that document is stored as on the file system.  This is just the file name, for full path and name use
    ///     FileNameAndPath
    /// </summary>
    public string FileName { get; set; } = "";


    /// <summary>
    ///     Returns the full path including the filename of the Stored Document
    /// </summary>
    [NotMapped]
    public string FileNameAndPath => Path.Join(StorageFolder, FileName);


    [Key] public long Id { get; set; }


    /// <summary>
    ///     Whether this document is considered a Live document.  A Live document is one who's expiration counter has not yet
    ///     started.
    ///     <para>
    ///         For Temporary documents it is set immediately after creation.  For other documents it is when the parent
    ///         system tells us it is not alive.
    ///     </para>
    ///     <para>
    ///         Typicaly unalive documents are part of claims, referrals, etc that are considered closed and experiencing no
    ///         activity on them and are moving towards an eventual archiving
    ///     </para>
    /// </summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>
    ///     Whether this document is stored on archival media
    /// </summary>
    public bool IsArchived { get; set; } = false;


    public DateTime LastAccessedUTC { get; set; }


    /// <summary>
    /// The Type of Document this is in regards to C# MediaTypes or additional app definitions, such as Word, Excel, Outlook, etc
    /// </summary>
    public EnumMediaTypes MediaType { get; set; }


    /// <summary>
    ///     The number of times this document has been accessed by an end-user.  Either read, write or edit.  Background
    ///     service tasks are not counted.
    /// </summary>
    public int NumberOfTimesAccessed { get; set; }

    public StorageNode PrimaryStorageNode { get; set; }



    // The nodes this document is stored on.
    public int? PrimaryStorageNodeId { get; set; }


    /// <summary>
    ///     This is the Id in the calling application that identifies the Root Object that this document belongs to.  For
    ///     instance, a Claim #, an Actors Guild #, a Policy #.
    /// </summary>
    public string RootObjectExternalKey { get; set; }

    public StorageNode SecondaryStorageNode { get; set; }
    public int? SecondaryStorageNodeId { get; set; }


    /// <summary>
    ///     The size of the file in Kilo Bytes.  -
    /// </summary>
    public int SizeInKB { get; set; }



    [Column(TypeName = "tinyint")]
    [Required]
    public EnumDocumentStatus Status { get; set; } = 0;

    /// <summary>
    ///     Where its stored.  This should not change after initial save.
    /// </summary>
    public string StorageFolder { get; set; }



#region "Methods and Functions"

    /// <summary>
    ///     Replaces the existing FileName with a new one.
    /// </summary>
    /// <param name="fileExtension"></param>
    public void ReplaceFileName(string fileExtension)
    {
        FileName = string.Empty;
        SetFileName(fileExtension);
    }


    /// <summary>
    ///     Will set the filename for the document IF it is blank.  If it already has a value then it is not changed.
    /// </summary>
    /// <param name="fileExtension"></param>
    public void SetFileName(string fileExtension)
    {
        string dotSeparate = ".";

        if (FileName == string.Empty)
        {
            if (fileExtension.StartsWith("."))
                FileName = Guid.NewGuid() + fileExtension;
            else
                FileName = Guid.NewGuid() + "." + fileExtension;
        }
    }

#endregion
}