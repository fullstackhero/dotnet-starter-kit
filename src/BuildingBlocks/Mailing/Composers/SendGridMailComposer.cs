using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;

namespace FSH.Framework.Mailing.Composers;

public class SendGridMailComposer(IOptions<MailOptions> settings) : IMailComposer<SendGridMessage>
{
    private readonly MailOptions _settings = settings!.Value;

    public SendGridMessage Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        var from = CreateFromAddress(request);
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(request.To[0]),
            request.Subject,
            request.Body,
            request.Body);

        ConfigureRecipients(msg, request);
        AddAttachments(msg, request);
        return msg;
    }

    private void ValidateConfiguration()
    {
        if (_settings.SendGrid?.ApiKey is null)
        {
            throw new InvalidOperationException("SendGrid ApiKey is not configured.");
        }
    }

    private EmailAddress CreateFromAddress(MailRequest request)
    {
        var email = request.From ?? _settings.SendGrid?.From ?? _settings.From;
        var displayName = request.DisplayName ?? _settings.SendGrid?.DisplayName ?? _settings.DisplayName;
        return new EmailAddress(email, displayName);
    }

    private static void ConfigureRecipients(SendGridMessage msg, MailRequest request)
    {
        if (request.Cc.Count > 0)
        {
            msg.AddCcs(request.Cc.Select(cc => new EmailAddress(cc)).ToList());
        }

        if (request.Bcc.Count > 0)
        {
            msg.AddBccs(request.Bcc.Select(bcc => new EmailAddress(bcc)).ToList());
        }

        if (request.ReplyTo != null)
        {
            msg.ReplyTo = new EmailAddress(request.ReplyTo, request.ReplyToName);
        }
    }

    private static void AddAttachments(SendGridMessage msg, MailRequest request)
    {
        foreach (var att in request.AttachmentData)
        {
            msg.AddAttachment(att.Key, Convert.ToBase64String(att.Value));
        }
    }
}
