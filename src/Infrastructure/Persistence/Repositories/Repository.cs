using System.Data;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Constants;
using DN.WebApi.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Persistence.Repositories
{
    public class Repository : IRepository
    {
        private ISerializerService _serializer;
        private readonly IDistributedCache _cache;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<Repository> _logger;

        public Repository(ApplicationDbContext dbContext, ISerializerService serializer, IDistributedCache cache, ILogger<Repository> logger)
        {
            _dbContext = dbContext;
            _serializer = serializer;
            _cache = cache;
            _logger = logger;
        }

        #region  Entity Framework Core : Get All
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> conditions, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            IQueryable<T> query = _dbContext.Set<T>();
            if (conditions != null) query = query.Where(conditions);
            return await query.ToListAsync(cancellationToken);
        }
        #endregion

        public async Task<T> GetByIdAsync<T>(object id, bool enforceCaching = true, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            if (enforceCaching)
            {
                var cacheKey = CacheKeys.GetEntityCacheKey<T>(id);
                byte[] cachedData = !string.IsNullOrWhiteSpace(cacheKey) ? await _cache.GetAsync(cacheKey, cancellationToken) : null;
                if (cachedData != null)
                {
                    await _cache.RefreshAsync(cacheKey);
                    _logger.LogInformation($"Refreshed Cache : {cacheKey}");
                    return _serializer.Deserialize<T>(Encoding.Default.GetString(cachedData));
                }
                else
                {
                    var result = await _dbContext.Set<T>().FindAsync(id);
                    var options = new DistributedCacheEntryOptions();
                    byte[] serializedData = Encoding.Default.GetBytes(_serializer.Serialize(result));
                    await _cache.SetAsync(cacheKey, serializedData, options, cancellationToken);
                    _logger.LogInformation($"Added To Cache : {cacheKey}");
                    return result;
                }
            }
            return await _dbContext.Set<T>().FindAsync(id);
        }
        public async Task<List<T>> GetPaginatedListAsync<T>(int pageNumber, int pageSize) where T : BaseEntity
        {
            return await _dbContext
                .Set<T>()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<T> InsertAsync<T>(T entity) where T : BaseEntity
        {
            await _dbContext.Set<T>().AddAsync(entity);
            return entity;
        }



        public Task RemoveAsync<T>(T entity) where T : BaseEntity
        {
            _dbContext.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync<T>(T entity) where T : BaseEntity
        {
            T exist = _dbContext.Set<T>().Find(entity.Id);
            _dbContext.Entry(exist).CurrentValues.SetValues(entity);
            return Task.CompletedTask;
        }

        #region Dapper
        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction)).AsList();
        }
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return await _dbContext.Connection.QuerySingleAsync<T>(sql, param, transaction);
        }
        public async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Connection.ExecuteAsync(sql, param, transaction);
        }

        #endregion
    }
}