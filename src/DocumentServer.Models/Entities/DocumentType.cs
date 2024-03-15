using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;

namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
///     Represents a Type of Document that can be stored.  Most importantly a Document Type determines a few things about a
///     document:
///     <para>1. Where it is stored.</para>
///     <para>2. How it is stored.  Temporary, permanent, editable</para>
///     <para>3. How long it is stored for</para>
/// </summary>
public class DocumentType : AbstractBaseEntity
{
    // Storage Nodes 
    public StorageNode? ActiveStorageNode1 { get; set; }


    //public int ApplicationId { get; set; }
    public int? ActiveStorageNode1Id { get; set; }
    public StorageNode? ActiveStorageNode2 { get; set; }
    public int? ActiveStorageNode2Id { get; set; }
    public Application Application { get; set; }
    public StorageNode? ArchivalStorageNode1 { get; set; }
    public int? ArchivalStorageNode1Id { get; set; }
    public StorageNode? ArchivalStorageNode2 { get; set; }
    public int? ArchivalStorageNode2Id { get; set; }

    /// <summary>
    ///     Description of the Document
    /// </summary>
    [MaxLength(250)]
    public string Description { get; set; }


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
                                       Name);
            return msg;
        }
    }


    /// <summary>
    ///     ID of Document Type
    /// </summary>
    [Key]
    public int Id { get; set; }


    /// <summary>
    ///     How long after the document is considered closed or InActive it should remain in system.
    /// </summary>
    [Column(TypeName = "tinyint")]
    public EnumDocumentLifetimes InActiveLifeTime { get; set; } = EnumDocumentLifetimes.Never;


    /// <summary>
    ///     Name of this Document Type
    /// </summary>
    [MaxLength(75)]
    public string Name
    {
        get { return _name; }
        set
        {
            if (value == null)
                _name = string.Empty;
            else
            {
                _name = value.Trim();
                if (_name.Length > 75)
                    _name = _name.Substring(0, 75);
            }
        }
    }

    private string _name;


    public RootObject RootObject { get; set; }

    /// <summary>
    ///     Each document type can be stored in a custom folder location. Or you can leave blank.
    ///     This just adds another subfolder into the path.
    ///     For example as blank:  /storage/parent/yyyy/mm/dd/file.pdf
    ///     With value set: /storage/parent/xyz/yyyy/mm/dd/file/pdf
    /// </summary>
    [MaxLength(10)]
    public string StorageFolderName
    {
        get { return _storageFolderName; }
        set
        {
            int maxLength = 10;
            if (value == null)
                _storageFolderName = string.Empty;
            else
            {
                _storageFolderName = value.Trim();
                if (_storageFolderName.Length > maxLength)
                    _storageFolderName = _storageFolderName.Substring(0, maxLength);
            }
        }
    }

    private string _storageFolderName = "";


    public static Result<DocumentType> CreateDocumentType(string name,
                                                          string description,
                                                          string storageFolder,
                                                          EnumStorageMode storageMode,
                                                          int applicationId,
                                                          int rootObjectId,
                                                          int activeStorageNodeId,
                                                          EnumDocumentLifetimes lifeTime = EnumDocumentLifetimes.Never)
    {
        DocumentType x = new();
        x.Name                 = name;
        x.Description          = description;
        x.StorageFolderName    = storageFolder;
        x.StorageMode          = storageMode;
        x.RootObjectId         = rootObjectId;
        x.ApplicationId        = applicationId;
        x.ActiveStorageNode1Id = activeStorageNodeId;
        x.InActiveLifeTime     = lifeTime;

        Result result = x.IsValid();
        if (result.IsSuccess)
            return Result.Ok(x);

        StringBuilder sb = new("Errors during creation of DocumentType object:");
        foreach (IError resultError in result.Errors)
            sb.Append(Environment.NewLine + resultError);

        return Result.Fail(new Error(sb.ToString()));

        throw new ArgumentException(sb.ToString());
    }


    /// <summary>
    ///     Prevent WORM Fields from being able to be updated and saved.
    /// </summary>
    /// <returns></returns>
    public override bool HasWormFields() => true;


    public Result IsValid()
    {
        Result result = new();
        if (StorageFolderName.Length > 10)
            result.WithError(new Error("Storage Folder Name must be less than 10 characters"));

        if (!StorageFolderName.All(c => char.IsLetterOrDigit(c)))
            result.WithError(new Error("Storage Folder Name can only contain a single word with only letters or digits"));
        return result;
    }


    public override void OnEditRemoveWORMFields(EntityEntry entityEntry)
    {
        entityEntry.Property("ApplicationId").IsModified    = false;
        entityEntry.Property("StorageMode").IsModified      = false;
        entityEntry.Property("RootObjectId").IsModified     = false;
        entityEntry.Property("AllowSameDTEKeys").IsModified = false;

        base.OnEditRemoveWORMFields(entityEntry);
    }


#region "Worm Fields"

    /// <summary>
    ///     Default Storage mode for documents of this type.  This cannot be changed once saved.
    /// </summary>
    [Required]
    [Column(TypeName = "tinyint")]
    public EnumStorageMode StorageMode { get; set; }


    /// <summary>
    ///     The Root object associated with this document type.  This cannot be changed after initial creation.
    /// </summary>
    [Required]
    public int RootObjectId { get; set; }


    /// <summary>
    ///     The application this Document Type is associated with
    /// </summary>
    [Required]
    public int ApplicationId { get; set; }


    /// <summary>
    ///     If this is false, then there can only be one entry per DocumentType External Key.  Meaning an invoice 123 can only
    ///     exist once for this documenttype and rootObjectKey combination.  If true, there can be multiple of the same key.
    ///     This cannot be changed after initial save
    /// </summary>
    public bool AllowSameDTEKeys { get; set; } = false;

#endregion
}