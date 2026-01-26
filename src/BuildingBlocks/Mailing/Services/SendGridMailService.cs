using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Mailing.Services;

public class SendGridMailService : IMailService
{
    private readonly MailOptions _settings;

    public SendGridMailService(IOptions<MailOptions> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
    }

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        var client = new SendGridClient(_settings.SendGrid!.ApiKey!);
        var from = CreateFromAddress(request);
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(request.To[0]),
            request.Subject,
            request.Body,
            request.Body);

        ConfigureRecipients(msg, request);
        AddAttachments(msg, request);

        await client.SendEmailAsync(msg, ct);
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
