using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentServer.Models.Interfaces;

namespace DocumentServer.Models.Entities;

/// <summary>
/// Contains some base fields used by all Entites
/// </summary>
public class AbstractBaseEntity : IBaseEntity
{
    /// <summary>
    /// When it was created.
    /// </summary>
    [Required]
    public DateTime CreatedAtUTC { get; set; }

    /// <summary>
    /// When it was last updated.
    /// </summary>
    public DateTime? ModifiedAtUTC { get; set; }


    /// <summary>
    /// True if the object is currently active, false if not.
    /// </summary>
    public bool IsActive { get; set; }
}