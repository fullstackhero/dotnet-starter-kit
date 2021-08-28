using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Abstractions.Repositories
{
    public interface IEFRepository<T, in TId> where T : BaseEntity
    {
        IQueryable<T> Entities { get; }

        Task<T> GetByIdAsync(TId id);

        Task<List<T>> GetAllAsync();

        Task<List<T>> GetPagedResponseAsync(int pageNumber, int pageSize);

        Task<T> AddAsync(T entity);

        Task UpdateAsync(T entity);

        Task DeleteAsync(T entity);
    }
}