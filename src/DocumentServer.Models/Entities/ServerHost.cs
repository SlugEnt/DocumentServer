using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Models.Entities
{
    public class ServerHost : AbstractBaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Column(TypeName = "smallint")]
        public short Id { get; set; }

        /// <summary>
        /// The DNS name of the server.  This is not the Fully Qualified name.
        /// </summary>
        public string NameDNS { get; set; }


        /// <summary>
        /// This must be the Fully Qualified Domain Name.
        /// </summary>
        public string FQDN { get; set; }


        /// <summary>
        /// Path on actual server to get to root of the Data.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///  If true, all communication with server is over HTTPS.
        /// </summary>
        public bool IsHttps { get; set; }
    }
}