using FSH.Framework.Infrastructure.Persistence.Abstractions;

namespace FSH.Framework.Infrastructure.Multitenancy;
public class TenantDataSeeder(TenantDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRootTenantAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedRootTenantAsync(CancellationToken cancellationToken)
    {
        if (await context.TenantInfo.FindAsync([MultitenancyConstants.Root.Id], cancellationToken).ConfigureAwait(false) is null)
        {
            var rootTenant = new FshTenantInfo(
                MultitenancyConstants.Root.Id,
                MultitenancyConstants.Root.Name,
                string.Empty,
                MultitenancyConstants.Root.EmailAddress);

            rootTenant.SetValidity(DateTime.UtcNow.AddYears(1));

            context.TenantInfo.Add(rootTenant);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
