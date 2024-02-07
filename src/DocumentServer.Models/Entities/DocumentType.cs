using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Models.Enums;

namespace DocumentServer.Models.Entities
{
    public class DocumentType : AbstractBaseEntity
    {
        public DocumentType() { }


        public DocumentType(string name,
                            string description,
                            string storageFolder,
                            EnumStorageMode storageMode,
                            int applicationId,
                            int activeStorageNodeId)
        {
/*            if (storageFolder.Contains(" "))
                throw new ArgumentException("Storage Folder Name cannot contain a space.");*/
            if (storageFolder.Length > 10)
                throw new ArgumentException("Storage Folder Name must be less than 10 characters");
            if (!storageFolder.All(c => char.IsLetterOrDigit(c)))
                throw new ArgumentException("Storage Folder Name can only contain a single word with only letters or digits");

            Name                 = name;
            Description          = description;
            StorageFolderName    = storageFolder;
            StorageMode          = storageMode;
            ApplicationId        = applicationId;
            ActiveStorageNode1Id = activeStorageNodeId;
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


        // Relationships
        public int ApplicationId { get; set; }
        public int? ActiveStorageNode1Id { get; set; }
        public int? ActiveStorageNode2Id { get; set; }
        public int? ArchivalStorageNode1Id { get; set; }
        public int? ArchivalStorageNode2Id { get; set; }

        /// <summary>
        /// The application this document type belongs to.
        /// </summary>
        public Application Application { get; set; }

        // Storage Nodes 
        public StorageNode ActiveStorageNode1 { get; set; }
        public StorageNode ActiveStorageNode2 { get; set; }
        public StorageNode ArchivalStorageNode1 { get; set; }
        public StorageNode ArchivalStorageNode2 { get; set; }
    }
}