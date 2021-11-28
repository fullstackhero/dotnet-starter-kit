using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Shared.DTOs;
using DN.WebApi.Shared.DTOs.Filters;
using System.Data;
using System.Linq.Expressions;
using DN.WebApi.Application.Common.Specifications;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IRepositoryAsync : ITransientService
{
    Task<T> GetByIdAsync<T>(Guid id, BaseSpecification<T> specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<TDto> GetByIdAsync<T, TDto>(Guid id, BaseSpecification<T> specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto;

    Task<int> GetCountAsync<T>(Expression<Func<T, bool>> expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task UpdateAsync<T>(T entity)
    where T : BaseEntity;

    #region Dapper

    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    #endregion Dapper

    #region Save Changes

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    #endregion Save Changes

    #region Find

    Task<IEnumerable<T>> FindByConditionAsync<T>(Expression<Func<T, bool>> expression, bool AsNoTracking = true, BaseSpecification<T> specification = null)
    where T : BaseEntity;

    #endregion Find

    #region FirstOrDefault

    Task<T> FirstByConditionAsync<T>(Expression<Func<T, bool>> expression, bool AsNoTracking = true, BaseSpecification<T> specification = null)
    where T : BaseEntity;

    #endregion FirstOrDefault

    #region LastOrDefault

    Task<T> LastByConditionAsync<T>(Expression<Func<T, bool>> expression, bool AsNoTracking = true, BaseSpecification<T> specification = null)
    where T : BaseEntity;

    #endregion LastOrDefault

    #region Create

    Task<Guid> CreateAsync<T>(T entity)
    where T : BaseEntity;

    Task<IList<Guid>> CreateRangeAsync<T>(IEnumerable<T> entity)
    where T : BaseEntity;

    #endregion Create

    #region DeleteOrRemoveOrClear

    Task RemoveAsync<T>(T entity)
    where T : BaseEntity;

    Task<T> RemoveByIdAsync<T>(Guid entityId)
    where T : BaseEntity;

    Task ClearAsync<T>(Expression<Func<T, bool>> expression = null, BaseSpecification<T> specification = null)
    where T : BaseEntity;

    #endregion DeleteOrRemoveOrClear

    #region Paginate

    Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[] orderBy = null, Search advancedSearch = null, string keyword = null, Expression<Func<T, bool>> expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto;

    Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[] orderBy = null, Filters<T> filters = null, Search advancedSearch = null, string keyword = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto;

    #endregion Paginate

    #region Aggregations

    Task<int> CountByConditionAsync<T>(Expression<Func<T, bool>> expression = null)
    where T : BaseEntity;

    #endregion Aggregations
}