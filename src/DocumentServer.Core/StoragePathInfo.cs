using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Used by DocumentServerEngine to keep track of the file paths of a document.
    /// </summary>
    internal record StoragePathInfo
    {
        /// <summary>
        /// This is the part of the file location that is stored in the StoredDocument.  It is the final piece of the path, containing ModeType, DocType and date parts of the path that are dynamic.
        /// </summary>
        public string StoredDocumentPath { get; set; }

        /// <summary>
        /// This is the entire physical path location to the stored document.  It includes the host path, node path and the StoredDocumentPath.
        /// </summary>
        public string ActualPath { get; set; }
    }
}