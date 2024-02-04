using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.Models
{
    public class Document
    {
        public Guid Id { get; set; }

        public string AppName { get; set; }
        public string AppAreaId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}