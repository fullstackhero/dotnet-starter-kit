namespace FSH.Framework.Core.Abstraction.Persistence;
public interface IDbInitializer
{
    Task MigrateAsync(CancellationToken cancellationToken);
    Task SeedAsync(CancellationToken cancellationToken);
}
