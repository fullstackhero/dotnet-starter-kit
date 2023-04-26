using System.Data;
using System.Reflection;
using Dapper;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Application.Common.Persistence;
using FSH.WebApi.Domain.Common.Contracts;
using FSH.WebApi.Infrastructure.Persistence.Context;

namespace FSH.WebApi.Infrastructure.Persistence.Repository;

public class DapperRepository : IDapperRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DapperRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity =>
        (await _dbContext.Connection.QueryAsync<T>(sql, param, transaction))
            .AsList();

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (_dbContext.Model.GetMultiTenantEntityTypes().Any(t => t.ClrType == typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantInfo.Id);
        }

        return await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
    }

    public Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        if (_dbContext.Model.GetMultiTenantEntityTypes().Any(t => t.ClrType == typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantInfo.Id);
        }

        return _dbContext.Connection.QuerySingleAsync<T>(sql, param, transaction);
    }

    public async Task<int> ExecuteAsync<T>(string sql, object? param, IDbTransaction? transaction, CancellationToken cancellationToken)
    where T : class, IEntity
    {
        try
        {
            if (!_dbContext.Model.GetMultiTenantEntityTypes().Any(t => t.ClrType == typeof(T)))
            {
                sql = sql.Replace("@tenant", _dbContext.TenantInfo.Id);
            }

            return await _dbContext.Connection.ExecuteAsync(sql, param, transaction);
        }
        catch (Exception ex)
        {
            // Handle the exception
            return 0;
        }
    }

    /// <summary>
    /// This method deleted a product specified by an ID
    /// </summary>
    /// <param name="id"></param>
    /// sql = $"DELETE FROM {_tableName} WHERE Id=@Id"
    /// sql = $"delete from {typeof(T).Name}s where Id = @Id";
    /// sql = $"select * from {typeof(T).Name}s ";
    /// <returns>int.</returns>
    public async Task<int> DeleteAsync<T>(Guid id)
    where T : class, IEntity
    {
        string sql = $"DELETE FROM {typeof(T).Name}s WHERE Id = @Id";

        return await _dbContext.Connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<T>> SearchAsync<T>(string? where = null)
    where T : class, IEntity
    {
        string sql = $"SELECT * FROM {typeof(T).Name}s ";

        if (!string.IsNullOrWhiteSpace(where))
            sql += where;

        return (await _dbContext.Connection.QueryAsync<T>(sql)).AsList();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>()
    where T : class, IEntity
    {
        string sql = $"SELECT * FROM {typeof(T).Name}s ";

        return (await _dbContext.Connection.QueryAsync<T>(sql)).AsList();
    }

    public async Task<T> GetAsync<T>(Guid id)
    where T : class, IEntity
    {
        string sql = $"SELECT * FROM {typeof(T).Name}s WHERE Id=@Id";

        // string query = "SELECT * FROM {typeof(T).Name}s WHERE Id =" + id;
        var entity = await _dbContext.Connection.QuerySingleOrDefaultAsync<T>(sql);
        return entity ?? throw new NotFoundException($"{typeof(T).Name}s with id [{id}] could not be found.");
    }

    public async Task<int> AddAsync<T>(T entity)
    where T : class, IEntity
    {
        var columns = GetColumns<T>();
        string stringOfColumns = string.Join(", ", columns);
        string stringOfParameters = string.Join(", ", columns.Select(e => "@" + e));
        string sql = $"INSERT INTO {typeof(T).Name}s ({stringOfColumns}) VALUES ({stringOfParameters})";

        return await _dbContext.Connection.ExecuteAsync(sql, entity);
    }

    public async Task<int> UpdateAsync<T>(T entity)
    where T : class, IEntity
    {
        var columns = GetColumns<T>();
        string stringOfColumns = string.Join(", ", columns.Select(e => $"{e} = @{e}"));
        string sql = $"UPDATE {typeof(T).Name}s SET {stringOfColumns} WHERE Id = @Id";

        return await _dbContext.Connection.ExecuteAsync(sql, entity);
    }

    private static IEnumerable<string> GetColumns<T>()
    {
        return typeof(T)
                .GetProperties()
                .Where(e => e.Name != "Id" && !e.PropertyType.GetTypeInfo().IsGenericType)
                .Select(e => e.Name);
    }

    public async Task UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IAggregateRoot
    {
        // foreach (var entity in entities)
        // {
        //    _dbContext.Entry(entity).State = EntityState.Modified;
        // }

        _dbContext.TenantNotSetMode = TenantNotSetMode.Overwrite;
        _dbContext.Set<T>().UpdateRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        _dbContext.Set<T>().AddRange(entities);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entities;
    }
    public async Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity
    {
        _dbContext.Set<T>().RemoveRange(entities);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}