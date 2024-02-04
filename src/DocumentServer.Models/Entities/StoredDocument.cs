using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.Models.Entities
{
    public class StoredDocument
    {
        [Key] public Guid Id { get; set; }

        public string AppName { get; set; }
        public string StorageFolder { get; set; }


        /// <summary>
        /// The size of the file in Kilo Bytes.  -
        /// </summary>
        public int sizeInKB { get; set; }

        public string AppAreaId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}