using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Factory;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Services;

//public class MailService<TMessage>(
//    IMailComposer<TMessage> composer,
//    IMailTransport<TMessage> transport) : IMailService
//{
//    private readonly IMailComposer<TMessage> _composer = composer ?? throw new ArgumentNullException(nameof(composer));
//    private readonly IMailTransport<TMessage> _transport = transport ?? throw new ArgumentNullException(nameof(transport));

//    public async Task SendAsync(MailRequest request, CancellationToken ct)
//    {
//        TMessage message =  _composer.Compose(request, ct);
//        await _transport.SendAsync(message, ct);
//    }
//}

public class MailService(
    MailProviderFactory factory,
    IOptions<MailOptions> options) : IMailService
{
    private readonly MailProviderFactory _factory = factory;
    private readonly MailOptions _options = options.Value;

    public Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var provider = _factory.Get(_options.Provider!);
        return provider.SendAsync(request, ct);
    }
}