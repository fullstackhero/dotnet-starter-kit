using System.Collections.Concurrent;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Capturing no-op mail service for integration tests — prevents real SMTP calls (and Hangfire retry
/// loops from email failures) while recording sent messages so tests can assert email behavior.
/// Tests in the shared collection run sequentially, so <see cref="Clear"/> + act + assert is safe.
/// </summary>
internal sealed class NoOpMailService : IMailService
{
    private readonly ConcurrentQueue<MailRequest> _sent = new();

    public IReadOnlyList<MailRequest> Sent => _sent.ToArray();

    public void Clear() => _sent.Clear();

    public Task SendAsync(MailRequest request, CancellationToken ct)
    {
        _sent.Enqueue(request);
        return Task.CompletedTask;
    }
}
