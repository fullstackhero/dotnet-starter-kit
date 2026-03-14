using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Models;

namespace FSH.Framework.Mailing.Services;
public class MailService(
    IMailComposer composer,
    IMailTransport transport) : IMailService
{
    private readonly IMailComposer _composer = composer;
    private readonly IMailTransport _transport = transport;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var mimeMessage = await _composer.Compose(request, ct);
        await _transport.SendAsync(mimeMessage, ct);
    }
}