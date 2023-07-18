namespace FL_CRMS_ERP_WEBAPI.Domain.Common.Contracts;

// Apply this marker interface only to aggregate root entities
// Repositories will only work with aggregate roots, not their children
public interface IAggregateRoot : IEntity
{
}