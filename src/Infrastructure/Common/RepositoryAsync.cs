using Dapper;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Contracts;
using DN.WebApi.Infrastructure.Common.Extensions;
using DN.WebApi.Infrastructure.Persistence.Contexts;
using DN.WebApi.Shared.DTOs;
using DN.WebApi.Shared.DTOs.Filters;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using System.Data;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using DN.WebApi.Application.Common.Constants;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Specifications;

namespace DN.WebApi.Infrastructure.Persistence.Repositories;

public class RepositoryAsync : IRepositoryAsync
{
    private readonly IStringLocalizer<RepositoryAsync> _localizer;
    private readonly ICacheService _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly ISerializerService _serializer;

    public RepositoryAsync(ApplicationDbContext dbContext, ISerializerService serializer, ICacheService cache, IStringLocalizer<RepositoryAsync> localizer)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _cache = cache;
        _localizer = localizer;
    }

    #region Entity Framework Core : Get All

    public Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression, bool noTracking = false, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (noTracking) query = query.AsNoTracking();
        if (expression != null) query = query.Where(expression);
        return query.ToListAsync(cancellationToken);
    }

    #endregion Entity Framework Core : Get All

    public Task<T?> GetByIdAsync<T>(Guid entityId, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (specification != null)
            query = query.Specify(specification);
        return query.Where(e => e.Id == entityId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TDto> GetByIdAsync<T, TDto>(Guid entityId, BaseSpecification<T>? specification = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        string cacheKey = CacheKeys.GetCacheKey<T>(entityId);
        byte[]? cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? await _cache.GetAsync(cacheKey, cancellationToken) : null;
        if (cachedData != null)
        {
            await _cache.RefreshAsync(cacheKey, cancellationToken);
            return _serializer.Deserialize<TDto>(Encoding.Default.GetString(cachedData));
        }
        else
        {
            IQueryable<T> query = _dbContext.Set<T>();
            if (specification != null)
                query = query.Specify(specification);
            var entity = await query.Where(a => a.Id == entityId).FirstOrDefaultAsync(cancellationToken);
            if (entity is not null)
            {
                var dto = entity.Adapt<TDto>();

                if ((specification?.Includes?.Count == 0) || specification == null)
                {
                    var options = new DistributedCacheEntryOptions();
                    byte[] serializedData = Encoding.Default.GetBytes(_serializer.Serialize(dto));
                    await _cache.SetAsync(cacheKey, serializedData, options, cancellationToken);
                }

                return dto;
            }

            throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));
        }
    }

    public Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (expression != null) return query.AnyAsync(expression, cancellationToken);
        return query.AnyAsync(cancellationToken);
    }

    public Task UpdateAsync<T>(T entity)
    where T : BaseEntity
    {
        if (_dbContext.Entry(entity).State == EntityState.Unchanged) throw new NothingToUpdateException();

        var existing = _dbContext.Set<T>().Find(entity.Id);

        if (existing is null) throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entity.Id));

        _dbContext.Entry(existing).CurrentValues.SetValues(entity);
        _cache.Remove(CacheKeys.GetCacheKey<T>(entity.Id));
        return Task.CompletedTask;
    }

    #region Dapper

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        return (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction)).AsList();
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.Tenant);
        }

        var entity = await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        if (entity == null) throw new EntityNotFoundException(string.Empty);
        return entity;
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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetCountAsync<T>(Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query.AsNoTracking().CountAsync(cancellationToken);
    }

    #endregion Dapper

    #region Find

    public Task<List<T>> FindByConditionAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null)
         where T : BaseEntity
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (AsNoTracking)
            query = query.AsNoTracking();

        if (specification != null)
            query = query.Specify(specification);

        if (expression != null)
            query = query.Where(expression);

        return query.ToListAsync();
    }

    #endregion Find

    #region FirstOrDefault

    public Task<T?> FirstByConditionAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null)
         where T : BaseEntity
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (AsNoTracking)
            query = query.AsNoTracking();

        if (specification != null)
            query = query.Specify(specification);

        if (expression != null)
            query = query.Where(expression);

        return query.FirstOrDefaultAsync();
    }

    #endregion FirstOrDefault

    #region LastOrDefault

    public Task<T?> LastByConditionAsync<T>(Expression<Func<T, bool>>? expression, bool AsNoTracking = true, BaseSpecification<T>? specification = null)
         where T : BaseEntity
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (AsNoTracking)
            query = query.AsNoTracking();

        if (specification != null)
            query = query.Specify(specification);

        if (expression != null)
            query = query.Where(expression);

        return query.LastOrDefaultAsync();
    }

    #endregion LastOrDefault

    #region Create

    public async Task<Guid> CreateAsync<T>(T entity)
    where T : BaseEntity
    {
        await _dbContext.Set<T>().AddAsync(entity);
        return entity.Id;
    }

    public async Task<IList<Guid>> CreateRangeAsync<T>(IEnumerable<T> entity)
    where T : BaseEntity
    {
        await _dbContext.Set<T>().AddRangeAsync(entity);
        return entity.Select(x => x.Id).ToList();
    }

    #endregion Create

    #region DeleteOrRemoveOrClear

    public Task RemoveAsync<T>(T entity)
    where T : BaseEntity
    {
        _dbContext.Set<T>().Remove(entity);
        _cache.Remove(CacheKeys.GetCacheKey<T>(entity.Id));
        return Task.CompletedTask;
    }

    public async Task<T> RemoveByIdAsync<T>(Guid entityId)
    where T : BaseEntity
    {
        var entity = await _dbContext.Set<T>().FindAsync(entityId);
        if (entity == null) throw new EntityNotFoundException(string.Format(_localizer["entity.notfound"], typeof(T).Name, entityId));
        _dbContext.Set<T>().Remove(entity);
        _cache.Remove(CacheKeys.GetCacheKey<T>(entityId));
        return entity;
    }

    public Task ClearAsync<T>(Expression<Func<T, bool>>? expression = null, BaseSpecification<T>? specification = null)
    where T : BaseEntity
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (specification != null)
            query.Specify(specification);
        if (expression != null)
            query = query.Where(expression);

        return query.ForEachAsync(x =>
        {
            _dbContext.Entry(x).State = EntityState.Deleted;
            _cache.Remove(CacheKeys.GetCacheKey<T>(x.Id));
        });
    }

    #endregion DeleteOrRemoveOrClear

    #region Paginate

    public Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[]? orderBy = null, Search? advancedSearch = null, string? keyword = null, Expression<Func<T, bool>>? expression = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (expression != null) query = query.Where(expression);
        if (advancedSearch?.Fields.Count > 0 && !string.IsNullOrEmpty(advancedSearch.Keyword))
            query = query.AdvancedSearch(advancedSearch);
        else if (!string.IsNullOrEmpty(keyword))
            query = query.SearchByKeyword(keyword);
        query = query.ApplySort(orderBy);
        return query.ToMappedPaginatedResultAsync<T, TDto>(pageNumber, pageSize);
    }

    public Task<PaginatedResult<TDto>> GetSearchResultsAsync<T, TDto>(int pageNumber, int pageSize = int.MaxValue, string[]? orderBy = null, Filters<T>? filters = null, Search? advancedSearch = null, string? keyword = null, CancellationToken cancellationToken = default)
    where T : BaseEntity
    where TDto : IDto
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (filters is not null)
            query = query.ApplyFilter(filters);
        if (advancedSearch?.Fields.Count > 0 && !string.IsNullOrEmpty(advancedSearch.Keyword))
            query = query.AdvancedSearch(advancedSearch);
        else if (!string.IsNullOrEmpty(keyword))
            query = query.SearchByKeyword(keyword);
        query = query.ApplySort(orderBy);
        return query.ToMappedPaginatedResultAsync<T, TDto>(pageNumber, pageSize);
    }

    #endregion Paginate

    #region Aggregations

    public Task<int> CountByConditionAsync<T>(Expression<Func<T, bool>>? expression = null)
          where T : BaseEntity
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (expression != null)
            query = query.Where(expression);
        return query.CountAsync();
    }

    #endregion Aggregations
}