namespace FSH.Framework.Persistence;

/// <summary>
/// Interface for database initialization operations including migrations and seeding.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Applies pending database migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MigrateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SeedAsync(CancellationToken cancellationToken);
}