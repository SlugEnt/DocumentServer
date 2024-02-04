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
    public class DocumentType
    {
        /// <summary>
        /// ID of Document Type
        /// </summary>
        [Key]
        public uint Id { get; set; }


        /// <summary>
        ///  Description of this Document Type
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Description of the Document
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Default Storage mode for documents of this type.
        /// </summary>
        [Column(TypeName = "tinyint")]
        public EnumStorageMode StorageMode { get; set; }



        // Relationships

        /// <summary>
        /// The application this document type belongs to.
        /// </summary>
        public Application Application { get; set; }
    }
}