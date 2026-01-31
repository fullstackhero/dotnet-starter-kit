namespace FSH.Framework.Core.Domain;

/// <summary>
/// Represents an aggregate root in the domain model.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class AggregateRoot<TId> : BaseEntity<TId>
{
    // Put aggregate-wide behaviors/helpers here if needed
}
