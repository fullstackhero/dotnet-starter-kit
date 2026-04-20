using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Services;

/// <summary>
/// Hangfire recurring job that generates draft invoices for the previous billing period. Scheduled
/// to run shortly after midnight UTC on the 1st of each month so that all usage counters for the
/// prior period are still present in Redis (TTL hasn't expired) when the snapshot is captured.
/// </summary>
public sealed class MonthlyInvoiceJob
{
    private readonly IBillingService _billing;
    private readonly ILogger<MonthlyInvoiceJob> _logger;

    public MonthlyInvoiceJob(IBillingService billing, ILogger<MonthlyInvoiceJob> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var previous = DateTime.UtcNow.AddMonths(-1);
        _logger.LogInformation("[Billing] MonthlyInvoiceJob generating invoices for period {Year}-{Month:00}",
            previous.Year, previous.Month);

        var count = await _billing.GenerateInvoicesForAllTenantsAsync(previous.Year, previous.Month, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[Billing] MonthlyInvoiceJob generated {Count} draft invoices for {Year}-{Month:00}",
            count, previous.Year, previous.Month);
    }
}
