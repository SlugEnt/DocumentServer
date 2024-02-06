using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Models.Enums;

namespace DocumentServer.Models.DTOS
{
    public class DocumentUploadDTO
    {
        public string Description { get; set; }
        public string FileExtension { get; set; }
        public string FileBytes { get; set; }

        /// <summary>
        /// Type of Document This is
        /// </summary>
        public int DocumentTypeId { get; set; }
    }
}