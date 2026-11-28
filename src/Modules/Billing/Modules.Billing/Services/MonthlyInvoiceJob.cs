using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Services;

/// <summary>
/// Hangfire recurring job that generates draft invoices for the previous billing period. Scheduled
/// to run shortly after midnight UTC on the 1st of each month. Usage is read for the requested
/// period (<c>IQuotaService.GetForPeriodAsync</c>), and monthly Redis counters outlive their period
/// by <c>QuotaOptions:PeriodCounterRetentionDays</c>, so a late or retried run still sees them.
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
