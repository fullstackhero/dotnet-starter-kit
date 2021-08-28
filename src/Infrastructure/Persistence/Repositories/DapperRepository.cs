using System.Data;
using Dapper;
using DN.WebApi.Application.Abstractions.Repositories;

namespace DN.WebApi.Infrastructure.Persistence.Repositories
{
    public class DapperRepository : IDapperRepository
    {
        protected readonly ApplicationDbContext _db;

        public DapperRepository(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return (await _db.Connection.QueryAsync<T>(sql, param, transaction)).AsList();
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await _db.Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await _db.Connection.QuerySingleAsync<T>(sql, param, transaction);
        }
        public async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await _db.Connection.ExecuteAsync(sql, param, transaction);
        }


    }
}