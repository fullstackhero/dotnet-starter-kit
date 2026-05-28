using FSH.Framework.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Data;

public sealed class BillingDbInitializer(
    BillingDbContext dbContext,
    ILogger<BillingDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Billing] applied migrations");
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Plans are a global catalogue (IGlobalEntity). Seed the defaults once — the "free" plan backs
        // the trial fallback used when a tenant is created without an explicit plan. Keys align with
        // QuotaOptions plan keys so quota limits resolve.
        if (await dbContext.Plans.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        dbContext.Plans.Add(BillingPlan.Create("free", "Free", "USD", 0m, interval: PlanInterval.Monthly));
        dbContext.Plans.Add(BillingPlan.Create("pro", "Pro", "USD", 29m, interval: PlanInterval.Monthly));
        dbContext.Plans.Add(BillingPlan.Create("pro-annual", "Pro (Annual)", "USD", 29m,
            interval: PlanInterval.Yearly, annualPrice: 290m));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Billing] seeded default plans (free, pro, pro-annual)");
    }
}
