using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Models.Enums;

namespace DocumentServer.Models.Entities
{
    public class StoredDocument
    {
        [Key] public Guid Id { get; set; }

        [Column(TypeName = "tinyint")]
        [Required]
        public EnumDocumentStatus Status { get; set; }


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
        public int sizeInKB { get; set; } = 0;

        /// <summary>
        /// When it was created.
        /// </summary>
        [Required]
        public DateTime CreatedAtUTC { get; set; }

        /// <summary>
        /// When it was last updated.
        /// </summary>
        public DateTime ModifiedAtUTC { get; set; }


        public DateTime LastAccessedUTC { get; set; }

        /// <summary>
        /// The number of times this document has been accessed by an end-user.  Either read, write or edit.  Background service tasks are not counted.
        /// </summary>
        public ushort NumberOfTimesAccessed { get; set; }


        // Relationships
        // Each document is associated with a Document Type
        public uint DocumentTypeId { get; set; }
        [Required] public DocumentType DocumentType { get; set; }

        /// <summary>
        /// The node this document is stored on.
        /// </summary>
        public uint StorageNodeId { get; set; }

        [Required] public StorageNode StorageNode { get; set; }
    }
}