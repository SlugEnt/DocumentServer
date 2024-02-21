using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SlugEnt.DocumentServer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SlugEnt.DocumentServer.Models.Entities
{
    /// <summary>
    /// The StoredDocument is the database entity that stores information about a single stored document.
    /// The document itself is saved on a file system with the name  Id.FileExtension
    /// It is thus vital that FileExtension and Id are never changed.  
    /// </summary>
    [Index(nameof(RootObjectExternalKey), nameof(DocTypeExternalKey), Name = "IDX_Ext_Keys")]
    public class StoredDocument : AbstractBaseEntity
    {
        /// <summary>
        /// Empty Constructor - Prefer the normal 
        /// </summary>
        public StoredDocument() { }


        /// <summary>
        /// Normal Constructor
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


        [Key] public long Id { get; set; }

        /// <summary>
        /// Filename that document is stored as on the file system.  This is just the file name, for full path and name use FileNameAndPath
        /// </summary>
        public string FileName { get; set; } = "";

//        [Key] public Guid Id { get; set; }


        [Column(TypeName = "tinyint")]
        [Required]
        public EnumDocumentStatus Status { get; set; } = 0;

        /// <summary>
        /// A readable name or description for the document.. Maximum length of 250
        /// </summary>
        [MaxLength(250)]
        public string Description { get; set; }

        /// <summary>
        /// Where its stored.
        /// </summary>
        public string StorageFolder { get; set; }


        /// <summary>
        /// The size of the file in Kilo Bytes.  -
        /// </summary>
        public int SizeInKB { get; set; } = 0;

        /// <summary>
        /// Whether this document is stored on archival media
        /// </summary>
        public bool IsArchived { get; set; } = false;


        /// <summary>
        /// Whether this document is considered a Live document.  A Live document is one who's expiration counter has not yet started.
        /// <para>For Temporary documents it is set immediately after creation.  For other documents it is when the parent system tells us it is not alive.</para>
        /// <para>Typicaly unalive documents are part of claims, referrals, etc that are considered closed and experiencing no activity on them and are moving towards an eventual archiving</para>
        /// </summary>
        public bool IsAlive { get; set; } = true;


        public DateTime LastAccessedUTC { get; set; }

        /// <summary>
        /// The number of times this document has been accessed by an end-user.  Either read, write or edit.  Background service tasks are not counted.
        /// </summary>
        public int NumberOfTimesAccessed { get; set; }


        /// <summary>
        /// This is the Id in the calling application that identifies the Root Object that this document belongs to.  For instance, a Claim #, an Actors Guild #, a Policy #.
        /// </summary>
        public string RootObjectExternalKey { get; set; }


        /// <summary>
        /// This is the Id in the calling application for this particular Document Type.  For instance it could be an invoice Number.
        /// </summary>
        public string? DocTypeExternalKey { get; set; }

        // Relationships


        // Each document is associated with a Document Type
        public int DocumentTypeId { get; set; }
        [Required] public DocumentType DocumentType { get; set; }


        // The nodes this document is stored on.
        public int? PrimaryStorageNodeId { get; set; }
        public int? SecondaryStorageNodeId { get; set; }

        public StorageNode PrimaryStorageNode { get; set; }
        public StorageNode SecondaryStorageNode { get; set; }


        /// <summary>
        /// This is a 0:1 relationship.
        /// </summary>
        //public List<ExpiringDocument> ExpiringDocuments { get; set; }

        /// <summary>
        /// Returns the stored filename
        /// </summary>
        [Obsolete]
        public string ComputedStoredFileName
        {
            get
            {
                return FileName;
                /*
                if (FileExtension == string.Empty)
                    return Id.ToString();

                string value = Id + "." + FileExtension;
                return value;
                */
            }
        }


        /// <summary>
        /// Will set the filename for the document IF it is blank.  If it already has a value then it is not changed.
        /// </summary>
        /// <param name="fileExtension"></param>
        public void SetFileName(string fileExtension)
        {
            if (FileName == string.Empty)
            {
                string ext = fileExtension == string.Empty ? string.Empty : "." + fileExtension;
                FileName = Guid.NewGuid().ToString() + ext;
            }
        }



        /// <summary>
        /// Replaces the existing FileName with a new one.
        /// </summary>
        /// <param name="fileExtension"></param>
        public void ReplaceFileName(string fileExtension)
        {
            FileName = string.Empty;
            SetFileName(fileExtension);
        }


        /// <summary>
        /// Returns the full path including the filename of the Stored Document
        /// </summary>
        [NotMapped]
        public string FileNameAndPath
        {
            get { return Path.Join(StorageFolder, FileName); }
        }


        /// <summary>
        /// For displaying information about this in an error type message
        /// </summary>
        [NotMapped]
        public string ErrorMessage
        {
            get
            {
                string className = this.GetType().Name;
                string msg = String.Format("{0}:  [Id: {1} | Name: {2} ]",
                                           className,
                                           Id,
                                           FileName);
                return msg;
            }
        }
    }
}