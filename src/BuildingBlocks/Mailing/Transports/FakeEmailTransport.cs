using FSH.Framework.Mailing.Contracts;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FSH.Framework.Mailing.Transports;

public class FakeEmailTransport(ILogger<FakeEmailTransport> logger) : IMailTransport
{
    private readonly ILogger<FakeEmailTransport> _logger = logger;

    public Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            $"FAKE EMAIL -> To:{{To}} Subject:{{Subject}}",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}