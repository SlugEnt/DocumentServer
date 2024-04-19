using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Settings for DocumentServer from the AppSettings File(s)
    /// </summary>
    public class DocumentServerFromAppSettings
    {
        public string NodeKey { get; set; } = string.Empty;

        /// <summary>
        /// The size in KB that is the initial limit for sending a document to a remote node on the initial save
        /// </summary>
        public int RemoteSizeThreshold { get; set; }
    }
}