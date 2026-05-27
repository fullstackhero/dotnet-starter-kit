// Add this hosted service class once in your auditing module
using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Auditing;

public sealed class AuditingConfigurator : IHostedService
{
    private readonly IAuditPublisher _publisher;
    private readonly IAuditSerializer _serializer;
    private readonly IEnumerable<IAuditEnricher> _enrichers;

    public AuditingConfigurator(
        IAuditPublisher publisher,
        IAuditSerializer serializer,
        IEnumerable<IAuditEnricher> enrichers)
    {
        _publisher = publisher;
        _serializer = serializer;
        _enrichers = enrichers;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Audit.Configure(_publisher, _serializer, _enrichers);
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}