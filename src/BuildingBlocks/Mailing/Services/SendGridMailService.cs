using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Mailing.Services;

public sealed class SendGridMailService : IMailService
{
    private readonly MailOptions _settings;
    private readonly ISendGridClient _client;

    public SendGridMailService(IOptions<MailOptions> settings, ISendGridClient client)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(client);
        _settings = settings.Value;
        _client = client;
    }

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        if (request.To is null or { Count: 0 })
        {
            throw new InvalidOperationException("At least one recipient is required.");
        }

        var from = CreateFromAddress(request);
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(request.To[0]),
            request.Subject,
            request.Body,
            request.Body);

        ConfigureRecipients(msg, request);
        AddAttachments(msg, request);

        await _client.SendEmailAsync(msg, ct).ConfigureAwait(false);
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