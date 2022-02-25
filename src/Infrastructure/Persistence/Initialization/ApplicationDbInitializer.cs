using Finbuckle.MultiTenant;
using FSH.WebApi.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Persistence.Initialization;

internal class ApplicationDbInitializer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantInfo _currentTenant;
    private readonly ILogger<ApplicationDbInitializer> _logger;
    private readonly CustomSeederRunner _seederRunner;

    public ApplicationDbInitializer(ApplicationDbContext dbContext, ITenantInfo currentTenant, CustomSeederRunner seederRunner, ILogger<ApplicationDbInitializer> logger)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _seederRunner = seederRunner;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.GetMigrations().Any())
        {
            if (_dbContext.Database.GetPendingMigrations().Any())
            {
                _logger.LogInformation("Applying Migrations for '{tenantId}' tenant.", _currentTenant.Id);
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }

            if (_dbContext.Database.CanConnect())
            {
                _logger.LogInformation("Connection to {tenantId}'s Database Succeeded.", _currentTenant.Id);

                await _seederRunner.RunSeedersAsync(cancellationToken);

                // await _dbSeeder.SeedDatabaseAsync(_dbContext, cancellationToken);
            }
        }
    }
}