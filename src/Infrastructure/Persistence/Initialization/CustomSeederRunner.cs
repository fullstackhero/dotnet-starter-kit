using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.Persistence.Initialization;

internal class CustomSeederRunner
{
    private readonly ICustomSeeder[] _seeders;

    public CustomSeederRunner(IServiceProvider serviceProvider) =>
        _seeders = serviceProvider.GetServices<ICustomSeeder>().ToArray();

    public async Task RunSeedersAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seeders.OrderBy(seed => seed.OrderByKeyName))
        {
            await seeder.InitializeAsync(cancellationToken);
        }
    }
}