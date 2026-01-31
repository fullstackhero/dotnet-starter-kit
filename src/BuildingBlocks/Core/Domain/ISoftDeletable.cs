namespace FSH.Framework.Core.Domain;

/// <summary>
/// Marks an entity as supporting soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was deleted.
    /// </summary>
    DateTimeOffset? DeletedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted the entity.
    /// </summary>
    string? DeletedBy { get; }
}
