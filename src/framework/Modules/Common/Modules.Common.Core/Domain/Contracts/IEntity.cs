using FSH.Framework.Core.Messaging.Events;
using System.Collections.ObjectModel;

namespace FSH.Framework.Core.Domain.Contracts;

public interface IEntity
{
    Collection<AppEvent> DomainEvents { get; }
}

public interface IEntity<out TId> : IEntity
{
    TId Id { get; }
}
