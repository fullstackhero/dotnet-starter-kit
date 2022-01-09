using System.Data;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Dapper;
using DN.WebApi.Application.Common;
using DN.WebApi.Application.Common.Caching;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Persistance;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Mapping;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using Mapster;
using Microsoft.EntityFrameworkCore;
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

    // Get List

    public Task<List<T>> GetListAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(condition, orderBy, includes, asNoTracking: asNoTracking)
            .ToListAsync(cancellationToken);

    public Task<List<T>> GetListAsync<T>(
        BaseSpecification<T>? specification = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(specification: specification, asNoTracking: asNoTracking)
            .ToListAsync(cancellationToken);

    public Task<List<TProjectedType>> GetListAsync<T, TProjectedType>(
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, TProjectedType>>? selectExpression = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        selectExpression == null
            ? throw new ArgumentNullException(nameof(selectExpression))
            : Filter(condition, orderBy, includes, asNoTracking: true)
                .Select(selectExpression)
                .ToListAsync(cancellationToken);

    public Task<List<TProjectedType>> GetListAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        BaseSpecification<T>? specification = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        selectExpression == null
            ? throw new ArgumentNullException(nameof(selectExpression))
            : Filter(specification: specification, asNoTracking: true)
                .Select(selectExpression)
                .ToListAsync(cancellationToken);

    public Task<PaginatedResult<TDto>> GetListAsync<T, TDto>(
        PaginationSpecification<T> specification,
        CancellationToken cancellationToken = default)
    where T : class, IEntity
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

        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();

        if (specification.Filters is not null)
        {
            query = query.ApplyFilter(specification.Filters);
        }

        if (specification.AdvancedSearch?.Fields.Count > 0 && !string.IsNullOrWhiteSpace(specification.AdvancedSearch.Keyword))
        {
            query = query.AdvancedSearch(specification.AdvancedSearch);
        }
        else if (!string.IsNullOrWhiteSpace(specification.Keyword))
        {
            query = query.SearchByKeyword(specification.Keyword);
        }

        query = query.Specify(specification);

        return query.ToMappedPaginatedResultAsync<T, TDto>(specification.PageIndex, specification.PageSize, cancellationToken);
    }

    // Get One By Id

    public Task<T?> GetByIdAsync<T, TKey>(
        TKey entityId,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull =>
        Filter(e => e.Id.Equals(entityId), includes: includes, asNoTracking: asNoTracking)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<T?> GetByIdAsync<T>(
        DefaultIdType entityId,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => GetByIdAsync<T, DefaultIdType>(entityId, includes, asNoTracking, cancellationToken);

    public Task<TProjectedType?> GetByIdAsync<T, TKey, TProjectedType>(
        TKey entityId,
        Expression<Func<T, TProjectedType>> selectExpression,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull =>
        selectExpression == null
            ? throw new ArgumentNullException(nameof(selectExpression))
            : Filter(e => e.Id.Equals(entityId), includes: includes, asNoTracking: true)
                .Select(selectExpression)
                .FirstOrDefaultAsync(cancellationToken);
    public Task<TProjectedType?> GetByIdAsync<T, TProjectedType>(
        DefaultIdType entityId,
        Expression<Func<T, TProjectedType>> selectExpression,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => GetByIdAsync<T, DefaultIdType, TProjectedType>(entityId, selectExpression, includes, cancellationToken);

    public async Task<TDto> GetByIdAsync<T, TKey, TDto>(
        TKey entityId,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull
    where TDto : IDto
    {
        async Task<TDto?> getDto() =>
            await GetByIdAsync<T, TKey>(entityId, includes, true, cancellationToken) is object entity
                ? entity.Adapt<TDto?>()
                : default;

        // Only get from cache when no includes defined
        var dto = includes == null
            ? await _cache.GetOrSetAsync(CacheKeys.GetCacheKey<T>(entityId), getDto, cancellationToken: cancellationToken)
            : await getDto();

        return dto is null
            ? throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId))
            : dto;
    }

    public async Task<TDto> GetByIdAsync<T, TDto>(
        DefaultIdType entityId,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType>
    where TDto : IDto => await GetByIdAsync<T, DefaultIdType, TDto>(entityId, includes, cancellationToken);

    // Get One By Condition

    public Task<T?> GetAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(condition, includes: includes, asNoTracking: asNoTracking)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<T?> GetAsync<T>(
       BaseSpecification<T>? specification = null,
       bool asNoTracking = false,
       CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(specification: specification, asNoTracking: asNoTracking)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TProjectedType?> GetAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        Expression<Func<T, bool>>? condition = null,
        Expression<Func<T, object>>[]? includes = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        selectExpression == null
            ? throw new ArgumentNullException(nameof(selectExpression))
            : Filter(condition, includes: includes, asNoTracking: asNoTracking)
                .Select(selectExpression)
                .FirstOrDefaultAsync(cancellationToken);

    public Task<TProjectedType?> GetAsync<T, TProjectedType>(
        Expression<Func<T, TProjectedType>> selectExpression,
        BaseSpecification<T>? specification = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        selectExpression == null
            ? throw new ArgumentNullException(nameof(selectExpression))
            : Filter(specification: specification, asNoTracking: asNoTracking)
                .Select(selectExpression)
                .FirstOrDefaultAsync(cancellationToken);

    // Get Count

    public Task<int> GetCountAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(condition)
            .CountAsync(cancellationToken);

    // Check if Exists

    public Task<bool> ExistsAsync<T>(
        Expression<Func<T, bool>> condition,
        CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        Filter(condition)
            .AnyAsync(cancellationToken);

    // Filter

    private IQueryable<T> Filter<T>(
        Expression<Func<T, bool>>? condition = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Expression<Func<T, object>>[]? includes = null,
        BaseSpecification<T>? specification = null,
        bool asNoTracking = false)
    where T : class, IEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (condition is not null)
        {
            query = query.Where(condition);
        }

        if (specification is not null)
        {
            query = query.Specify(specification);
        }

        if (includes is not null)
        {
            query = query.IncludeMultiple(includes);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        return query;
    }

    // Create (these won't work with database generated id's if we were to suport that...)

    public async Task<TKey> CreateAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    {
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    public async Task<DefaultIdType> CreateAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => await CreateAsync<T, DefaultIdType>(entity, cancellationToken);

    public async Task<IList<TKey>> CreateRangeAsync<T, TKey>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    {
        await _dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);
        return entities.Select(x => x.Id).ToList();
    }

    public async Task<IList<DefaultIdType>> CreateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => await CreateRangeAsync<T, DefaultIdType>(entities, cancellationToken);

    // Update

    public Task UpdateAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull
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

    public Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType>
    {
        return UpdateAsync<T, DefaultIdType>(entity, cancellationToken);
    }

    public async Task UpdateRangeAsync<T, TKey>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull
    {
        foreach (var entity in entities)
        {
            if (_dbContext.Entry(entity).State == EntityState.Unchanged)
            {
                throw new NothingToUpdateException();
            }

            var existing = _dbContext.Set<T>().Find(entity.Id);

            _ = existing ?? throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entity.Id));

            _dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entity.Id), cancellationToken);
        }
    }

    public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => await UpdateRangeAsync<T, DefaultIdType>(entities, cancellationToken);

    // Delete

    public Task RemoveAsync<T, TKey>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull
    {
        _dbContext.Set<T>().Remove(entity);
        return _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entity.Id), cancellationToken);
    }

    public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => RemoveAsync<T, DefaultIdType>(entity, cancellationToken);

    public async Task<T> RemoveByIdAsync<T>(DefaultIdType entityId, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        var entity = await _dbContext.Set<T>().FindAsync(new object?[] { entityId }, cancellationToken: cancellationToken);
        _ = entity ?? throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));

        _dbContext.Set<T>().Remove(entity);
        await _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entityId), cancellationToken);
        return entity;
    }

    public async Task RemoveRangeAsync<T, TKey>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull
    {
        foreach (var entity in entities)
        {
            _dbContext.Set<T>().Remove(entity);
            await _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(entity.Id), cancellationToken);
        }
    }

    public async Task RemoveRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => await RemoveRangeAsync<T, DefaultIdType>(entities, cancellationToken);

    public Task ClearAsync<T, TKey>(
        Expression<Func<T, bool>>? condition = null,
        BaseSpecification<T>? specification = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<TKey>
    where TKey : notnull =>
        Filter(condition, specification: specification)
            .ForEachAsync(
                x =>
            {
                _dbContext.Entry(x).State = EntityState.Deleted;
                _cache.RemoveAsync(CacheKeys.GetCacheKey<T>(x.Id), cancellationToken).GetAwaiter().GetResult();
            },
                cancellationToken: cancellationToken);

    public Task ClearAsync<T>(
        Expression<Func<T, bool>>? condition = null,
        BaseSpecification<T>? specification = null,
        CancellationToken cancellationToken = default)
    where T : class, IEntity<DefaultIdType> => ClearAsync<T, DefaultIdType>(condition, specification, cancellationToken);

    // SaveChanges

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    // Dapper

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction))
            .AsList();

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantKey);
        }

        var entity = await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);

        return entity ?? throw new EntityNotFoundException(string.Empty);
    }

    public Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantKey);
        }

        return _dbContext.Connection.QuerySingleAsync<T>(sql, param, transaction);
    }
}