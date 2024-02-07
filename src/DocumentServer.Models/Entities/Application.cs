using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.Models.Entities
{
    /// <summary>
    /// An Application that needs to store documents
    /// </summary>
    public class Application : AbstractBaseEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The name of the Application.  This can be full English Description
        /// </summary>
        [MaxLength(75)]
        public string Name { get; set; }


        // Relationships

        // Each App has 1 or more Document Types it manages.
        public ICollection<DocumentType> DocumentTypes;
    }
}