using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;

namespace FSH.Framework.Mailing.Providers;

public class FluentMailProvider(
    IMailComposer<FluentMailMessage> composer,
    IMailTransport<FluentMailMessage> transport)
    : IMailProvider
{
    public MailProviderType ProviderType => MailProviderType.Fluent;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message = composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}