using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Composers;

public class AzureMailComposer(IOptions<MailOptions> settings) : IMailComposer<AzureMailMessage>
{
    private readonly MailOptions _settings = settings.Value;

    public AzureMailMessage Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = new AzureMailMessage
        {
            From = request.From ?? _settings.From!,
            DisplayName = request.DisplayName ?? _settings.DisplayName,
            Subject = request.Subject,
            Body = request.Body
        };

        foreach (var to in request.To)
            message.To.Add(to);

        foreach (var cc in request.Cc)
            message.Cc.Add(cc);

        foreach (var bcc in request.Bcc)
            message.Bcc.Add(bcc);

        foreach (var attachment in request.AttachmentData)
            message.Attachments.Add(attachment.Key, attachment.Value);

        return message;
    }
}
