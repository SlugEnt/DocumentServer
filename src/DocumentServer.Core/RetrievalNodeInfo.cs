using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core;

/// <summary>
/// Information about the node to retrieve the document from
/// </summary>
internal class RetrievalNodeInfo
{
    /// <summary>
    /// True if the host that this is being called from is the node to retrieve the document from
    /// </summary>
    internal bool IsThisHist { get; set; }

    /// <summary>
    /// This is the full path to retrieve the document from, excluding the host part of the path.  This includes the filename and extension
    /// </summary>
    internal string FullPath { get; set; }
}