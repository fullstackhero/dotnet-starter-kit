namespace FSH.Framework.Core.Domain;

/// <summary>
/// Represents an entity with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntity<out TId>
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    TId Id { get; }
}
