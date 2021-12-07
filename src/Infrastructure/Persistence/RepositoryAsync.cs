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

    public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
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