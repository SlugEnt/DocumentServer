using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core
{
    internal record StoragePathInfo
    {
        public string StoredDocumentPath { get; set; }
        public string ActualPath { get; set; }
    }
}