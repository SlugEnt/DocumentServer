using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SlugEnt.DocumentServer.Models.Interfaces;

namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
///     Contains some base fields used by all Entites
/// </summary>
public class AbstractBaseEntity : IBaseEntity
{
    /// <summary>
    ///     True if the object is currently active, false if not.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     When it was created.
    /// </summary>
    [Required]
    public DateTime CreatedAtUTC { get; set; }


    public virtual bool HasWormFields() => false;

    /// <summary>
    ///     When it was last updated.
    /// </summary>
    public DateTime? ModifiedAtUTC { get; set; }


    public virtual void OnEditRemoveWORMFields(EntityEntry entityEntry) { }
}