using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using SendGrid.Helpers.Mail;

namespace FSH.Framework.Mailing.Providers;

public class SendGridMailProvider(
    IMailComposer<SendGridMessage> composer,
    IMailTransport<SendGridMessage> transport)
    : IMailProvider
{
    public MailProviderType ProviderType => MailProviderType.SendGrid;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message = composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}