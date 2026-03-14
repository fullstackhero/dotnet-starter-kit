using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Models;

namespace FSH.Framework.Mailing.Services;

public class MailService<TMessage>(
    IMailComposer<TMessage> composer,
    IMailTransport<TMessage> transport) : IMailService
{
    private readonly IMailComposer<TMessage> _composer = composer ?? throw new ArgumentNullException(nameof(composer));
    private readonly IMailTransport<TMessage> _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        TMessage message =  _composer.Compose(request, ct);
        await _transport.SendAsync(message, ct);
    }
}