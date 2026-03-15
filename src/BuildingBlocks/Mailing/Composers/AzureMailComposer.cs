using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Models;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Composers;

public class AzureEmailComposer(IOptions<MailOptions> settings) : IMailComposer<AzureEmailMessage>
{
    private readonly MailOptions _settings = settings.Value;

    public AzureEmailMessage Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = new AzureEmailMessage
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
