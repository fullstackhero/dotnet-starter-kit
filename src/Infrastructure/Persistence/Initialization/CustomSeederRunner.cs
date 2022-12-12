using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.Persistence.Initialization;

internal class CustomSeederRunner
{
    private readonly ICustomSeeder[] _seeders;

    public CustomSeederRunner(IServiceProvider serviceProvider) =>
        _seeders = serviceProvider.GetServices<ICustomSeeder>().ToArray();

    public async Task RunSeedersAsync(CancellationToken cancellationToken)
    {
        // For now, we don't have to run the Custom Seeders
        // foreach (var seeder in _seeders)
        // {
        //     await seeder.InitializeAsync(cancellationToken);
        // }

        await Task.CompletedTask;
    }
}