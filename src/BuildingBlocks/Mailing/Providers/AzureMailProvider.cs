using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;

namespace FSH.Framework.Mailing.Providers;

public class AzureMailProvider(
    IMailComposer<AzureMailMessage> composer,
    IMailTransport<AzureMailMessage> transport)
    : IMailProvider
{
    public string Name => "Azure";

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message = composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}