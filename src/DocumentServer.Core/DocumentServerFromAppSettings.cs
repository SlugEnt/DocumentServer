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
        public string DataFolder { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
    }
}