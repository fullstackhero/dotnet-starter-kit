using System.Data;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Dapper;
using DN.WebApi.Application.Common;
using DN.WebApi.Application.Common.Constants;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Contracts;
using DN.WebApi.Infrastructure.Mapping;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs;
using DN.WebApi.Shared.DTOs.Filters;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Infrastructure.Persistence;

public class RepositoryAsync : IRepositoryAsync
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IStringLocalizer<RepositoryAsync> _localizer;

    public RepositoryAsync(ApplicationDbContext dbContext, ICacheService cache, IStringLocalizer<RepositoryAsync> localizer)
    {
        _dbContext = dbContext;
        _cache = cache;
        _localizer = localizer;
    }

    // Read

    /*public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        GetQuery(expression, noTracking: noTracking)
            .ToListAsync(cancellationToken);

    public Task<int> GetCountAsync<T>(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        GetQuery(expression, noTracking: true)
            .CountAsync(cancellationToken);

    public Task<T?> GetByIdAsync<T>(Guid entityId, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        GetQuery(e => e.Id == entityId, specification)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<TDto> GetByIdAsync<T, TDto>(Guid entityId, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        async Task<TDto?> getDto() =>
            await GetByIdAsync(entityId, specification, cancellationToken) is object entity
                ? entity.Adapt<TDto?>()
                : default;

        // Only get from cache when no includes defined
        var dto = specification == null || specification.Includes.Count == 0
            ? await _cache.GetOrSetAsync(CacheKeys.GetCacheKey<T>(entityId), getDto, cancellationToken: cancellationToken)
            : await getDto();

        if (dto is null)
        {
            throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));
        }

        return dto;
    }

    public Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        GetQuery(expression)
            .AnyAsync(cancellationToken);

    public Task<List<T>> FindAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
         where T : BaseEntity =>
        GetQuery(expression, specification, AsNoTracking)
            .ToListAsync(cancellationToken);

    public Task<T?> FirstAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
         where T : BaseEntity =>
        GetQuery(expression, specification, AsNoTracking)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<T?> LastAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
         where T : BaseEntity =>
        GetQuery(expression, specification, AsNoTracking)
            .LastOrDefaultAsync(cancellationToken);

    public Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[]? orderBy = null, Filters<T>? filters = null, Search? advancedSearch = null, string? keyword = null, Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (filters is not null)
        {
            query = query.ApplyFilter(filters);
        }

        if (advancedSearch?.Fields.Count > 0 && !string.IsNullOrEmpty(advancedSearch.Keyword))
        {
            query = query.AdvancedSearch(advancedSearch);
        }
        else if (!string.IsNullOrEmpty(keyword))
        {
            query = query.SearchByKeyword(keyword);
        }

        if (expression is not null)
        {
            query = query.Where(expression);
        }

        query = query.ApplySort(orderBy);

        return query.ToMappedPaginatedResultAsync<T, TDto>(pageNumber, pageSize, cancellationToken);
    }*/

    // Get List
    public async Task<List<T>> GetListAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter<T>(condition, orderBy, includes, asNoTracking)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<List<T>> GetListAsync<T>(
        BaseSpecification<T>? specification = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter<T>(specification, asNoTracking)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<List<TProjectedType>> GetListAsync<T, TProjectedType>(
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, TProjectedType>>? selectExpression = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

        return await Filter(condition, orderBy, includes)
            .Select(selectExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<TProjectedType>> GetListAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        BaseSpecification<T>? specification = null,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

        return await Filter(specification)
            .Select(selectExpression)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<PaginatedResult<TDto>> GetListAsync<T, TDto>(PaginationSpecification<T> specification, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        if (specification == null)
        {
            throw new ArgumentNullException(nameof(specification));
        }

        if (specification.PageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(specification.PageIndex), $"The value of {nameof(specification.PageIndex)} must be greater than or equal to 0.");
        }

        if (specification.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(specification.PageSize), $"The value of {nameof(specification.PageSize)} must be greater than 0.");
        }

        IQueryable<T> query = _dbContext.Set<T>();

        if (specification.Filters is not null)
        {
            query = query.ApplyFilter(specification.Filters);
        }

        if (specification.AdvancedSearch?.Fields.Count > 0 && !string.IsNullOrEmpty(specification.AdvancedSearch.Keyword))
        {
            query = query.AdvancedSearch(specification.AdvancedSearch);
        }
        else if (!string.IsNullOrEmpty(specification.Keyword))
        {
            query = query.SearchByKeyword(specification.Keyword);
        }

        query = query.Specify(specification);

        return query.ToMappedPaginatedResultAsync<T, TDto>(specification.PageIndex, specification.PageSize, cancellationToken);
    }

    // Get One By Id
    public async Task<T?> GetByIdAsync<T>(
        Guid entityId,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter(e => e.Id == entityId, includes: includes, asNoTracking: asNoTracking)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<TProjectedType?> GetByIdAsync<T, TProjectedType>(
        Guid entityId,
        Expression<Func<T, TProjectedType>> selectExpression,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

        return await Filter(e => e.Id == entityId, includes: includes)
            .Select(selectExpression)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TDto> GetByIdAsync<T, TDto>(
        Guid entityId,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        async Task<TDto?> getDto() =>
            await GetByIdAsync(entityId, includes, true, cancellationToken) is object entity
                ? entity.Adapt<TDto?>()
                : default;

        // Only get from cache when no includes defined
        var dto = includes == null
            ? await _cache.GetOrSetAsync(CacheKeys.GetCacheKey<T>(entityId), getDto, cancellationToken: cancellationToken)
            : await getDto();

        if (dto is null)
        {
            throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));
        }

        return dto;
    }

    // Get One By Condition
    public async Task<T?> GetAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter(condition, includes: includes, asNoTracking: asNoTracking)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<T?> GetAsync<T>(
       BaseSpecification<T>? specification = null,
       bool asNoTracking = true,
       CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter(specification, asNoTracking)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<TProjectedType?> GetAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

        return await Filter(condition, includes: includes, asNoTracking: asNoTracking)
                .Select(selectExpression)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TProjectedType?> GetAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        BaseSpecification<T>? specification = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (selectExpression == null)
            throw new ArgumentNullException(nameof(selectExpression));

        return await Filter(specification, asNoTracking)
                .Select(selectExpression)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    // Get Count
    public async Task<int> GetCountAsync<T>(
        Expression<Func<T, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter(expression)
            .CountAsync(cancellationToken).ConfigureAwait(false);

    // Check if Exists
    public async Task<bool> ExistsAsync<T>(
        Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        await Filter(expression)
            .AnyAsync(cancellationToken).ConfigureAwait(false);

    // Filter Methods
    private IQueryable<T> Filter<T>(
        Expression<Func<T, bool>>? condition = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (condition != null)
            query = query.Where(condition);

        if (includes != null)
            query = query.IncludeMultiple(includes);

        if (asNoTracking)
            query = query.AsNoTracking();

        if (orderBy != null)
            query = orderBy(query);

        return query;
    }

    private IQueryable<T> Filter<T>(
        BaseSpecification<T>? specification = null,
        bool asNoTracking = true)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (specification != null)
            query = query.Specify(specification);

        if (asNoTracking)
            query = query.AsNoTracking();

        return query;
    }

    // Create / Update / Delete

    public async Task<Guid> CreateAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    public Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (_dbContext.Entry(entity).State == EntityState.Unchanged)
        {
            throw new NothingToUpdateException();
        }

        var existing = _dbContext.Set<T>().Find(entity.Id);

        _ = existing ?? throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entity.Id));

        _dbContext.Entry(existing).CurrentValues.SetValues(entity);
        return _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entity.Id), cancellationToken);
    }

    public async Task<IList<Guid>> CreateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        await _dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);
        return entities.Select(x => x.Id).ToList();
    }

    public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        _dbContext.Set<T>().Remove(entity);
        return _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entity.Id), cancellationToken);
    }

    public async Task<T> RemoveByIdAsync<T>(Guid entityId, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        var entity = await _dbContext.Set<T>().FindAsync(entityId);
        _ = entity ?? throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));

        _dbContext.Set<T>().Remove(entity);
        await _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entityId), cancellationToken);
        return entity;
    }

    public Task ClearAsync<T>(Expression<Func<T, bool>>? expression = null, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        GetQuery(expression, specification: specification)
            .ForEachAsync(x =>
            {
                _dbContext.Entry(x).State = EntityState.Deleted;
                _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(x.Id), cancellationToken).GetAwaiter().GetResult();
            });

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<T> GetQuery<T>(Expression<Func<T, bool>>? expression = null, BaseSpecification<T>? specification = null, bool noTracking = false)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (noTracking)
        {
            query = query.AsNoTracking();
        }

        if (expression is not null)
        {
            query = query.Where(expression);
        }

        if (specification is not null)
        {
            query = query.Specify(specification);
        }

        return query;
    }

    // Dapper

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity =>
        (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction)).AsList();

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.Tenant);
        }

        var entity = await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);

        return entity ?? throw new EntityNotFoundException(string.Empty);
    }

    public Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.Tenant);
        }

        return _dbContext.Connection.QuerySingleAsync<T>(sql, param, transaction);
    }
}