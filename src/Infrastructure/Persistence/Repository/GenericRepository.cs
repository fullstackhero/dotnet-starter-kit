//using Finbuckle.MultiTenant;
//using FSH.WebApi.Application.Common.Persistence;
//using FSH.WebApi.Domain.Common.Contracts;
//using FSH.WebApi.Infrastructure.Persistence.Context;
//using Microsoft.EntityFrameworkCore;

//namespace FSH.WebApi.Infrastructure.Persistence.Repository;
//public class GenericRepository : IGenericRepository
//{
//    protected readonly ApplicationDbContext _dbContext;

//    public GenericRepository(ApplicationDbContext dbContext)
//    {
//        _dbContext = dbContext;
//    }

//    public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        _dbContext.Set<T>().Add(entity);

//        await _dbContext.SaveChangesAsync(cancellationToken);

//        return entity;
//    }

//    public async Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        _dbContext.Set<T>().AddRange(entities);

//        await _dbContext.SaveChangesAsync(cancellationToken);

//        return entities;
//    }

//    public async Task UpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        _dbContext.Entry(entity).State = EntityState.Modified;

//        await _dbContext.SaveChangesAsync(cancellationToken);
//    }

//    public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
//        where T : class, IAggregateRoot
//    {
//        _dbContext.TenantNotSetMode = TenantNotSetMode.Overwrite;
//        _dbContext.Set<T>().UpdateRange(entities);
//        await _dbContext.SaveChangesAsync(cancellationToken);
//    }

//    public async Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        _dbContext.Set<T>().Remove(entity);

//        await _dbContext.SaveChangesAsync(cancellationToken);
//    }

//    public async Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        _dbContext.Set<T>().RemoveRange(entities);

//        await _dbContext.SaveChangesAsync(cancellationToken);
//    }

//    public async Task<int> SaveChangesAsync<T>(CancellationToken cancellationToken = default)
//        where T : class, IEntity
//    {
//        return await _dbContext.SaveChangesAsync(cancellationToken);
//    }
//}
