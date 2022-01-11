using Dapper;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Multitenancy;
using DN.WebApi.Infrastructure.Persistence.Context;
using System.Data;

namespace DN.WebApi.Infrastructure.Persistence.Repository;

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
        if (typeof(IMustHaveTenant).IsAssignableFrom(typeof(T)))
        {
            sql = sql.Replace("@tenant", _dbContext.TenantKey);
        }

        var entity = await _dbContext.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);

        return entity ?? throw new NotFoundException(string.Empty);
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