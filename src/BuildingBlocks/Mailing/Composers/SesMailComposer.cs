using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Composers;

public class SesMailComposer(IOptions<MailOptions> settings)
    : IMailComposer<SesMailMessage>
{
    private readonly MailOptions _settings = settings.Value;

    public SesMailMessage Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = new SesMailMessage
        {
            From = request.From ?? _settings.From!,
            Subject = request.Subject,
            Body = request.Body ?? string.Empty,
            To = [.. request.To],
            Cc = [.. request.Cc],
            Bcc = [.. request.Bcc]
        };

        return message;
    }
}
