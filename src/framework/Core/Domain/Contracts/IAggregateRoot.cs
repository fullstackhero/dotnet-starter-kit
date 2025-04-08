namespace FSH.Framework.Core.Domain.Contracts;

// Apply this marker interface only to aggregate root entities
// Repositories will only work with aggregate roots, not their children
public interface IAggregateRoot : IEntity
{
}
