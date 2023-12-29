namespace FSH.Framework.Infrastructure.Persistence.Abstractions;
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
