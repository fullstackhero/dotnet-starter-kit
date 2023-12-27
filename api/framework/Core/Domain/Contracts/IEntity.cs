using System.Collections.ObjectModel;
using FSH.Framework.Core.Domain.Events;

namespace FSH.Framework.Abstractions.Domain;

public interface IEntity
{
    Collection<DomainEvent> DomainEvents { get; }
}

public interface IEntity<out TId> : IEntity
{
    TId Id { get; }
}
