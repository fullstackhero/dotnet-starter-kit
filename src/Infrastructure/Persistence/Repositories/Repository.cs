using System.Data;
using System.Linq.Expressions;
using Dapper;
using DN.WebApi.Application.Abstractions.Repositories;
using DN.WebApi.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence.Repositories
{
    public class Repository : IRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public Repository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region  Entity Framework Core : Get All
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> conditions, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            IQueryable<T> query = _dbContext.Set<T>();
            if (conditions != null) query = query.Where(conditions);
            return await query.ToListAsync(cancellationToken);
        }
        #endregion

        public async Task<T> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default)  where T : BaseEntity
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }
        public async Task<List<T>> GetPaginatedListAsync<T>(int pageNumber, int pageSize)  where T : BaseEntity
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



        public Task RemoveAsync<T>(T entity)  where T : BaseEntity
        {
            _dbContext.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync<T>(T entity)  where T : BaseEntity
        {
            T exist = _dbContext.Set<T>().Find(entity.Id);
            _dbContext.Entry(exist).CurrentValues.SetValues(entity);
            return Task.CompletedTask;
        }

        #region Dapper
        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)  where T : BaseEntity
        {
            return (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction)).AsList();
        }
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)  where T : BaseEntity
        { 
            return await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)  where T : BaseEntity
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