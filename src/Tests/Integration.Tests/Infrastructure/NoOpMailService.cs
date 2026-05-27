using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// No-op mail service for integration tests ��� prevents real SMTP calls
/// and avoids Hangfire retry loops from email failures.
/// </summary>
internal sealed class NoOpMailService : IMailService
{
    public Task SendAsync(MailRequest request, CancellationToken ct) => Task.CompletedTask;
}
