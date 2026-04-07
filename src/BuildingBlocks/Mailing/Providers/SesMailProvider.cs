using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;

namespace FSH.Framework.Mailing.Providers;
public class SesMailProvider(
    IMailComposer<SesMailMessage> composer,
    IMailTransport<SesMailMessage> transport)
    : IMailProvider
{
    public MailProviderType ProviderType => MailProviderType.Ses;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message =  composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}