using FSH.Framework.Core.Messaging.Events;
using System.Collections.ObjectModel;

namespace FSH.Modules.Common.Core.Domain.Contracts;

public interface IEntity
{
    Collection<AppEvent> DomainEvents { get; }
}

public interface IEntity<out TId> : IEntity
{
    TId Id { get; }
}