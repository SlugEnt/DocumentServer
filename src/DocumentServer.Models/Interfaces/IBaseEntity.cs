using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentServer.Models.Interfaces;

/// <summary>
/// Describes the basic fields that are on every Table
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// When it was created.
    /// </summary>
    public DateTime CreatedAtUTC { get; set; }

    /// <summary>
    /// When it was last updated.
    /// </summary>
    public DateTime? ModifiedAtUTC { get; set; }
}