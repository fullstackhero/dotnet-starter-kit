using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Factory;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Services;


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