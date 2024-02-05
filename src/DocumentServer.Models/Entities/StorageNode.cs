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
    public class StorageNode
    {
        [Key] public ushort Id { get; set; }

        [MaxLength(50)] public string Name { get; set; }

        [MaxLength(400)] public string Description { get; set; }

        /// <summary>
        /// If true the Node can be used for production use
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// If true the node is a testing node, meaning its data can be periodically wiped no matter the Storage Mode set for the document 
        /// </summary>
        public bool IsTestNode { get; set; }

        /// <summary>
        /// Where this storage node stores its data - SMB or S3.
        /// </summary>
        [Column(TypeName = "tinyint")]
        public EnumStorageNodeLocation StorageNodeLocation { get; set; }

        /// <summary>
        /// Indicates how fast this storage node is.
        /// </summary>
        [Column(TypeName = "tinyint")]
        public EnumStorageNodeSpeed StorageSpeed { get; set; }


        /// <summary>
        /// The fully qualified path this node stores its files at.  
        /// </summary>
        [MaxLength(500)]
        public string NodePath { get; set; }


        public List<DocumentType> ActiveNode1DocumentTypes { get; set; }
        public List<DocumentType> ActiveNode2DocumentTypes { get; set; }
        public List<DocumentType> ArchivalNode1DocumentTypes { get; set; }
        public List<DocumentType> ArchivalNode2DocumentTypes { get; set; }

        public List<StoredDocument> PrimaryNodeStoredDocuments { get; set; }
        public List<StoredDocument> SecondaryNodeStoredDocuments { get; set; }
    }
}