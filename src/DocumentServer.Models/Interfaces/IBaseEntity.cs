using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SlugEnt.DocumentServer.Models.Interfaces;

/// <summary>
///     Describes the basic fields that are on every Table
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    ///     When it was created.
    /// </summary>
    public DateTime CreatedAtUTC { get; set; }

    /// <summary>
    ///     When it was last updated.
    /// </summary>
    public DateTime? ModifiedAtUTC { get; set; }

    public bool HasWormFields();


    /// <summary>
    ///     Method used to remove WORM fields from the entity on update.  This prevents these fields from ever being updated.
    /// </summary>
    public void OnEditRemoveWORMFields(EntityEntry entityEntry);
}