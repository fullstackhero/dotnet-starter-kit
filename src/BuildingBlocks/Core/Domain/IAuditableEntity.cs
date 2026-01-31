namespace FSH.Framework.Core.Domain;

/// <summary>
/// Defines audit metadata for an entity.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets the UTC timestamp when the entity was created.
    /// </summary>
    DateTimeOffset CreatedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the creator.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified.
    /// </summary>
    DateTimeOffset? LastModifiedOnUtc { get; }

    /// <summary>
    /// Gets the identifier of the last modifier.
    /// </summary>
    string? LastModifiedBy { get; }
}
