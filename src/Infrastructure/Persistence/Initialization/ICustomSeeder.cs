namespace FSH.WebApi.Infrastructure.Persistence.Initialization;

public interface ICustomSeeder
{
    string OrderByKeyName { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
}