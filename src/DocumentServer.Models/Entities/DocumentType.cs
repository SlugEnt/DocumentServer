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