using FSH.Modules.Auditing;
using Hangfire;
using Microsoft.Extensions.Hosting;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Two pieces of state are process-wide rather than per host: Hangfire's <see cref="JobStorage.Current"/>
/// (<c>HangfireService</c> enqueues through the static <c>BackgroundJob</c> API) and the static <c>Audit</c>
/// publisher (set by <see cref="AuditingConfigurator"/> on start). A derived host built with
/// <c>WithWebHostBuilder</c> points both at its own storage and audit channel, and when it is disposed
/// they are left pointing at a dead store and a channel nobody reads: later enqueues throw
/// <c>ObjectDisposedException</c> and request audits are silently lost.
/// When any host from <see cref="FshWebApplicationFactory"/> stops, this points both back at the shared host.
/// </summary>
internal sealed class PrimaryHostStaticsRestorer : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var primaryStorage = FshWebApplicationFactory.PrimaryJobStorage;
        if (primaryStorage is not null && !ReferenceEquals(JobStorage.Current, primaryStorage))
        {
            JobStorage.Current = primaryStorage;
        }

        if (FshWebApplicationFactory.PrimaryAuditingConfigurator is { } configurator)
        {
            await configurator.StartAsync(cancellationToken);
        }
    }
}
