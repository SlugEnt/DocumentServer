using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_DocumentServer.SupportObjects;

public class SupportMethodsConfiguration
{
    /// <summary>
    /// If true, the database will be setup and utilized.  If false, it will not
    /// </summary>
    public bool UseDatabase { get; set; } = true;

    /// <summary>
    /// If using the database, this determines if the system should use transactions or not.  Some of the tests use
    /// /// code that cannot have a nested transaction so it needs to be false.
    /// </summary>
    public bool UseTransactions { get; set; } = true;

    /// <summary>
    /// If set this will override the DNS Host name for the DocumentServerInformation Object on creation using the HostB in the database.
    /// </summary>
    public bool OverrideDNSName { get; set; } = false;

    /// <summary>
    /// What Folder Creation Mode to use.  None is the default, which creates none of the Fake File System folders
    /// </summary>
    public EnumFolderCreation FolderCreationSetting { get; set; } = EnumFolderCreation.None;

    /// <summary>
    /// This starts a second API instance.  Used only for testing node-to-node communication
    /// </summary>
    public bool StartSecondAPIInstance { get; set; } = false;
}