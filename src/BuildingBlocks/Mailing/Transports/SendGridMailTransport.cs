using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FSH.Framework.Mailing.Transports;

public class SendGridMailTransport(IOptions<MailOptions> settings) : IMailTransport<SendGridMessage>
{
    private readonly MailOptions _settings = settings.Value;

    public async Task SendAsync(SendGridMessage msg, CancellationToken ct)
    {
        var client = new SendGridClient(_settings.SendGrid!.ApiKey!);
        await client.SendEmailAsync(msg, ct);
    }
}
