using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core;

public class RuntimeSettings
{
    /// <summary>
    /// The maximum size in KB that a document can be in order to be sent to a remote storage node as part of the initial save.
    /// Documents larger than this are queued for later delivery to the remote node,
    /// Value is stored in KB.
    /// </summary>
    public int RemoteDocumentSizeThreshold { get; set; } = 3000 * 1024;
}