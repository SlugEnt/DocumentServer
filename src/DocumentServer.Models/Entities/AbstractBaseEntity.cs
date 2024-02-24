using SlugEnt.DocumentServer.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SlugEnt.DocumentServer.Models.Entities;

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


    public virtual bool HasWormFields() => false;


    public virtual void OnEditRemoveWORMFields(EntityEntry entityEntry) { }
}