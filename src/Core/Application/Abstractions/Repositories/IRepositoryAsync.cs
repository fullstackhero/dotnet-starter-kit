using DN.WebApi.Application.Abstractions.Services;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Contracts;
using DN.WebApi.Shared.DTOs;
using DN.WebApi.Shared.DTOs.Filters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Repositories
{
    public interface IRepositoryAsync : ITransientService
    {
        Task<T> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task<TDto> GetByIdAsync<T, TDto>(object id, CancellationToken cancellationToken = default)
        where T : BaseEntity
        where TDto : IDto;

        Task<int> GetCountAsync<T>(Expression<Func<T, bool>> expression = null, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task<PaginatedResult<TDto>> GetPaginatedListAsync<T, TDto>(int pageNumber, int pageSize, string[] orderBy = null, Search search = null, Expression<Func<T, bool>> expression = null, CancellationToken cancellationToken = default)
        where T : BaseEntity
        where TDto : IDto;

        Task<object> CreateAsync<T>(T entity)
        where T : BaseEntity;

        Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task UpdateAsync<T>(T entity)
        where T : BaseEntity;

        Task RemoveAsync<T>(T entity)
        where T : BaseEntity;
        Task RemoveByIdAsync<T>(object id)
        where T : BaseEntity;

        #region  Dapper
        Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        where T : BaseEntity;

        #endregion

        #region Save Changes
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        #endregion

    }
}