using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Models;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FSH.Framework.Mailing.Composers;

public class MimeKitEmailComposer(IOptions<MailOptions> settings) : IMailComposer
{
    private readonly MailOptions _settings = settings!.Value;

    public async Task<MimeMessage> Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = new MimeMessage();
        ConfigureSender(email, request);
        ConfigureRecipients(email, request);
        ConfigureContent(email, request);
        await AddAttachmentsAsync(email, request, ct);
        return email;
    }

    private void ConfigureSender(MimeMessage email, MailRequest request)
    {
        email.From.Add(new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From));
        email.Sender = new MailboxAddress(request.DisplayName ?? _settings.DisplayName, request.From ?? _settings.From);
    }

    private static void ConfigureRecipients(MimeMessage email, MailRequest request)
    {
        foreach (string address in request.To)
        {
            email.To.Add(MailboxAddress.Parse(address));
        }

        if (!string.IsNullOrEmpty(request.ReplyTo))
        {
            email.ReplyTo.Add(new MailboxAddress(request.ReplyToName, request.ReplyTo));
        }

        AddBccRecipients(email, request);
        AddCcRecipients(email, request);
        AddHeaders(email, request);
    }

    private static void AddBccRecipients(MimeMessage email, MailRequest request)
    {
        if (request.Bcc is null || request.Bcc.Count == 0)
        {
            return;
        }

        foreach (string address in request.Bcc.Where(bcc => !string.IsNullOrWhiteSpace(bcc)))
        {
            email.Bcc.Add(MailboxAddress.Parse(address.Trim()));
        }
    }

    private static void AddCcRecipients(MimeMessage email, MailRequest request)
    {
        if (request.Cc is null || request.Cc.Count == 0)
        {
            return;
        }

        foreach (string? address in request.Cc.Where(cc => !string.IsNullOrWhiteSpace(cc)))
        {
            email.Cc.Add(MailboxAddress.Parse(address.Trim()));
        }
    }

    private static void AddHeaders(MimeMessage email, MailRequest request)
    {
        if (request.Headers is null)
        {
            return;
        }

        foreach (var header in request.Headers)
        {
            email.Headers.Add(header.Key, header.Value);
        }
    }

    private static void ConfigureContent(MimeMessage email, MailRequest request)
    {
        email.Subject = request.Subject;
    }

    private static async Task AddAttachmentsAsync(MimeMessage email, MailRequest request, CancellationToken ct)
    {
        var builder = new BodyBuilder { HtmlBody = request.Body };

        if (request.AttachmentData is not null)
        {
            foreach (var attachment in request.AttachmentData)
            {
                using var stream = new MemoryStream();
                await stream.WriteAsync(attachment.Value, ct);
                stream.Position = 0;
                await builder.Attachments.AddAsync(attachment.Key, stream, ct);
            }
        }

        email.Body = builder.ToMessageBody();
    }
}