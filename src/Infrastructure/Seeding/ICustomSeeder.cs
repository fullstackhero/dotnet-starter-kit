namespace FSH.WebApi.Infrastructure.Seeding;

public interface ICustomSeeder
{
    Task InitializeAsync(CancellationToken cancellationToken);
}