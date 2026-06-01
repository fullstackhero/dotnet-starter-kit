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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MonthlyInvoiceJob> _logger;

    public MonthlyInvoiceJob(IBillingService billing, TimeProvider timeProvider, ILogger<MonthlyInvoiceJob> logger)
    {
        _billing = billing;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var previous = _timeProvider.GetUtcNow().UtcDateTime.AddMonths(-1);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] MonthlyInvoiceJob generating invoices for period {Year}-{Month:00}",
                previous.Year, previous.Month);
        }

        var count = await _billing.GenerateInvoicesForAllTenantsAsync(previous.Year, previous.Month, cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[Billing] MonthlyInvoiceJob generated {Count} draft invoices for {Year}-{Month:00}",
                count, previous.Year, previous.Month);
        }
    }
}
