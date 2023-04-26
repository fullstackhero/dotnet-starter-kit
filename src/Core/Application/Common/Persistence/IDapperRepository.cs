using System.Data;

namespace FSH.WebApi.Application.Common.Persistence;

public interface IDapperRepository : ITransientService
{
    /// <summary>
    /// Get an <see cref="IReadOnlyList{T}"/> using raw sql string with parameters.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="sql">The sql string.</param>
    /// <param name="param">The paramters in the sql string.</param>
    /// <param name="transaction">The transaction to be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>Returns <see cref="Task"/> of <see cref="IReadOnlyCollection{T}"/>.</returns>
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity;

    /// <summary>
    /// Get a <typeparamref name="T"/> using raw sql string with parameters.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="sql">The sql string.</param>
    /// <param name="param">The paramters in the sql string.</param>
    /// <param name="transaction">The transaction to be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>Returns <see cref="Task"/> of <typeparamref name="T"/>.</returns>
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity;

    /// <summary>
    /// Get a <typeparamref name="T"/> using raw sql string with parameters.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="sql">The sql string.</param>
    /// <param name="param">The paramters in the sql string.</param>
    /// <param name="transaction">The transaction to be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>Returns <see cref="Task"/> of <typeparamref name="T"/>.</returns>
    Task<T> QuerySingleAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    where T : class, IEntity;

    /// <summary>
    /// This method used for Delete, Insert, Update methords.
    /// </summary>
    /// <param name="sql"></param>
    /// sql = $"DELETE FROM {_tableName} WHERE Id=@Id"
    /// sql = $"delete from {typeof(T).Name}s where Id = @Id";
    /// sql = $"select * from {typeof(T).Name}s ";
    /// string sql = $"insert into {typeof(T).Name}s ({stringOfColumns}) values ({stringOfParameters})";
    /// string sql = $"update {typeof(T).Name}s set {stringOfColumns} where Id = @Id";
    /// <param name="param">The paramters in the sql string.</param>
    /// <param name="transaction">The transaction to be performed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>int.</returns>
    Task<int> ExecuteAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        where T : class, IEntity;
    Task<T> GetAsync<T>(DefaultIdType id)
        where T : class, IEntity;

    /// <summary>
    /// This method used for Update Range Entities.
    /// </summary>
    Task UpdateRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class, IAggregateRoot;
    Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity;
    Task<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    where T : class, IEntity;
}