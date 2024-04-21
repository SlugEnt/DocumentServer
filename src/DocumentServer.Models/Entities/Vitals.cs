using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Models.Entities;

public class VitalInfo
{
    [Key] [MaxLength(32)] public string Id { get; set; }

    [MaxLength(50)] public string Name { get; set; }

    public long ValueLong { get; set; }

    public string ValueString { get; set; }

    /// <summary>
    /// Is the datetime the vital stat was last updated.  
    /// </summary>
    public DateTime LastUpdateUtc { get; set; }


#region "Constants"

    /// <summary>
    ///  The Last Time a Key Entity had important info updated or a new one was created.
    /// Applies to:  Application, Root Object, Document Type, StorageNode
    /// </summary>
    public const string VI_LASTKEYENTITY_UPDATED = "LastKeyEntityUpdate";

#endregion
}