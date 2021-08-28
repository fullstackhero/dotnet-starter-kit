using System.Data;
using System.Linq.Expressions;
using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Application.Abstractions.Repositories
{
    public interface IRepository
    {
        Task<T> GetByIdAsync<T>(object id, bool enforceCaching = true, CancellationToken cancellationToken = default) where T : BaseEntity;

        Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> conditions, CancellationToken cancellationToken = default) where T : BaseEntity;

        Task<List<T>> GetPaginatedListAsync<T>(int pageNumber, int pageSize) where T : BaseEntity;

        Task<T> InsertAsync<T>(T entity) where T : BaseEntity;

        Task UpdateAsync<T>(T entity) where T : BaseEntity;

        Task RemoveAsync<T>(T entity) where T : BaseEntity;

        #region  Dapper
        Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default);
        #endregion
    }
}