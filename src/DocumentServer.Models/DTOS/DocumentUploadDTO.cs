using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.Models.DTOS
{
    public class DocumentUploadDTO
    {
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public string FileBytes { get; set; }
    }
}