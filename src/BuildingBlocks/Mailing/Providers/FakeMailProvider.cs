using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;

namespace FSH.Framework.Mailing.Providers;

public class FakeMailProvider(
    IMailComposer<FakeMimeMessage> composer,
    IMailTransport<FakeMimeMessage> transport)
    : IMailProvider
{
    public MailProviderType ProviderType => MailProviderType.Fake;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message = composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}