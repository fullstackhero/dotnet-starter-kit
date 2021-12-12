using System.Data;
using System.Linq.Expressions;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IRepositoryAsync : ITransientService
{
    // Read

    /*Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<int> GetCountAsync<T>(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T?> GetByIdAsync<T>(Guid id, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<TDto> GetByIdAsync<T, TDto>(Guid id, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto;

    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<List<T>> FindAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T?> FirstAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T?> LastAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[]? orderBy = null, Filters<T>? filters = null, Search? advancedSearch = null, string? keyword = null, Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto;*/

    Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>>? condition = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Expression<Func<T, object>>[]? includes = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<List<T>> GetListAsync<T, TKey>(BaseSpecification<T>? specification = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<List<TProjectedType>> GetListAsync<T, TProjectedType>(Expression<Func<T, bool>>? condition = null, Expression<Func<T, TProjectedType>>? selectExpression = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Expression<Func<T, object>>[]? includes = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<List<TProjectedType>> GetListAsync<T, TKey, TProjectedType>(Expression<Func<T, TProjectedType>> selectExpression, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<PaginatedResult<TDto>> GetListAsync<T, TKey, TDto>(PaginationSpecification<T> specification, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>
    where TDto : IDto;

    Task<T?> GetByIdAsync<T, TKey>(TKey entityId, Expression<Func<T, object>>[]? includes = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<TProjectedType?> GetByIdAsync<T, TKey, TProjectedType>(TKey entityId, Expression<Func<T, TProjectedType>> selectExpression, Expression<Func<T, object>>[]? includes = null, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<TDto> GetByIdAsync<T, TKey, TDto>(TKey entityId, Expression<Func<T, object>>[]? includes = null, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>
    where TDto : IDto;

    Task<T?> GetAsync<T>(Expression<Func<T, bool>>? condition = null, Expression<Func<T, object>>[]? includes = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T?> GetAsync<T, TKey>(BaseSpecification<T>? specification = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<TProjectedType?> GetAsync<T, TProjectedType>(Expression<Func<T, TProjectedType>> selectExpression, Expression<Func<T, bool>>? condition = null, Expression<Func<T, object>>[]? includes = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<TProjectedType?> GetAsync<T, TKey, TProjectedType>(Expression<Func<T, TProjectedType>> selectExpression, BaseSpecification<T>? specification = null, bool asNoTracking = true, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<int> GetCountAsync<T>(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    // Create / Update / Delete

    Task<TKey> CreateAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<IList<TKey>> CreateRangeAsync<T, TKey>(IEnumerable<T> entity, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task UpdateAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task RemoveAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<T> RemoveByIdAsync<T>(Guid entityId, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task ClearAsync<T, TKey>(Expression<Func<T, bool>>? expression = null, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntityWith<TKey>;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Dapper

    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;

    Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity;
}