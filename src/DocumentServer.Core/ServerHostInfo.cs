using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core;

/// <summary>
/// Runtime information about the host we are running on
/// </summary>
public class ServerHostInfo
{
    /// <summary>
    /// The Host ID of this physical server we are running on
    /// </summary>
    public short ServerHostId { get; set; }

    public string ServerHostName { get; set; }

    public string ServerFQDN { get; set; }

    /// <summary>
    /// Path to the data files on this host.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Unique Value that all nodes must agree on to talk to each other.
    /// </summary>
    public string NodeKey { get; set; }
}