using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Mailing.Transports;

public class FakeMailTransport(ILogger<FakeMailTransport> logger) : IMailTransport<FakeMimeMessage>
{
    private readonly ILogger<FakeMailTransport> _logger = logger;

    public Task SendAsync(FakeMimeMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            $"FAKE EMAIL -> To:{{To}} Subject:{{Subject}}",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}