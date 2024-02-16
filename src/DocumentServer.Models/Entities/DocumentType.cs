using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Models.Enums;
using SlugEnt.FluentResults;

namespace DocumentServer.Models.Entities
{
    /// <summary>
    /// Represents a Type of Document that can be stored.  Most importantly a Document Type determines a few things about a document:
    /// <para>1. Where it is stored.</para>
    /// <para>2. How it is stored.  Temporary, permanent, editable</para>
    /// <para>3. How long it is stored for</para>
    /// </summary>
    public class DocumentType : AbstractBaseEntity
    {
        public DocumentType() { }


        public DocumentType(string name,
                            string description,
                            string storageFolder,
                            EnumStorageMode storageMode,
                            int rootObjectId,
                            int activeStorageNodeId,
                            EnumDocumentLifetimes lifeTime = EnumDocumentLifetimes.Never)
        {
            Name                 = name;
            Description          = description;
            StorageFolderName    = storageFolder;
            StorageMode          = storageMode;
            RootObjectId         = rootObjectId;
            ActiveStorageNode1Id = activeStorageNodeId;
            InActiveLifeTime     = lifeTime;

            Result result = IsValid();
            if (result.IsSuccess)
                return;

            StringBuilder sb = new StringBuilder("Errors during creation of DocumentType object:");
            foreach (IError resultError in result.Errors)
            {
                sb.Append(Environment.NewLine + resultError);
            }

            throw new ArgumentException(sb.ToString());
        }


        public Result IsValid()
        {
            Result result = new Result();
            if (StorageFolderName.Length > 10)
                result.WithError(new Error("Storage Folder Name must be less than 10 characters"));

            if (!StorageFolderName.All(c => char.IsLetterOrDigit(c)))
                result.WithError(new Error("Storage Folder Name can only contain a single word with only letters or digits"));
            return result;
        }


        /// <summary>
        /// ID of Document Type
        /// </summary>
        [Key]
        public int Id { get; set; }


        /// <summary>
        ///  Description of this Document Type
        /// </summary>
        [MaxLength(75)]
        public string Name { get; set; }

        /// <summary>
        ///  Description of the Document
        /// </summary>
        [MaxLength(250)]
        public string Description { get; set; }

        /// <summary>
        /// Each document type can be stored in a custom folder location. Or you can leave blank.
        /// This just adds another subfolder into the path.
        /// For example as blank:  /storage/parent/yyyy/mm/dd/file.pdf
        /// With value set: /storage/parent/xyz/yyyy/mm/dd/file/pdf
        /// </summary>
        [MaxLength(10)]
        public string StorageFolderName { get; set; } = "";

        /// <summary>
        /// Default Storage mode for documents of this type.
        /// </summary>
        [Column(TypeName = "tinyint")]
        public EnumStorageMode StorageMode { get; set; }


        /// <summary>
        /// How long after the document is considered closed or InActive it should remain in system.
        /// </summary>
        [Column(TypeName = "tinyint")]
        public EnumDocumentLifetimes InActiveLifeTime { get; set; } = EnumDocumentLifetimes.Never;


        // Relationships
        public int RootObjectId { get; set; }


        /// <summary>
        /// If this is false, then there can only be one entry per DocumentType External Key.  Meaning an invoice 123 can only exist once for this documenttype and rootObjectKey combination.  If true, there can be multiple of the same key.
        /// </summary>
        public bool AllowSameDTEKeys { get; set; } = false;


        //public int ApplicationId { get; set; }
        public int? ActiveStorageNode1Id { get; set; }
        public int? ActiveStorageNode2Id { get; set; }
        public int? ArchivalStorageNode1Id { get; set; }
        public int? ArchivalStorageNode2Id { get; set; }

        /// <summary>
        /// The application this document type belongs to.
        /// </summary>

        //public Application Application { get; set; }
        public RootObject RootObject { get; set; }

        // Storage Nodes 
        public StorageNode ActiveStorageNode1 { get; set; }
        public StorageNode ActiveStorageNode2 { get; set; }
        public StorageNode ArchivalStorageNode1 { get; set; }
        public StorageNode ArchivalStorageNode2 { get; set; }


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
                                           Name);
                return msg;
            }
        }
    }
}