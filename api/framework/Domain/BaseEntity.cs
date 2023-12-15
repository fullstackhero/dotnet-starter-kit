using FSH.Framework.Abstractions.Domain;

namespace FSH.Framework.Domain;

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected init; } = default!;
}
