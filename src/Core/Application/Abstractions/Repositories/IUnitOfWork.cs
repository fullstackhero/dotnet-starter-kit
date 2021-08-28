using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Abstractions.Repositories
{
     public interface IUnitOfWork<TId> : IDisposable
    {
        IEFRepository<T, TId> Repository<T>() where T : AuditableEntity;

        Task<int> Commit(CancellationToken cancellationToken);

        Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys);

        Task Rollback();
    }
}