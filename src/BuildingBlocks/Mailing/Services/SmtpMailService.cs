using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace FSH.Framework.Mailing.Services;

public class SmtpMailService(IOptions<MailOptions> settings, ILogger<SmtpMailService> logger) : IMailService
{
    private readonly MailOptions _settings = settings.Value;
    private readonly ILogger<SmtpMailService> _logger = logger;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSmtpConfiguration();

        using var email = BuildMimeMessage(request);
        await AddAttachmentsAsync(email, request, ct);
        await SendEmailAsync(email, ct);
    }

    private void ValidateSmtpConfiguration()
    {
        if (_settings.Smtp?.Host is null)
        {
            throw new InvalidOperationException("SMTP Host is not configured.");
        }
    }

    private MimeMessage BuildMimeMessage(MailRequest request)
    {
        var email = new MimeMessage();

        ConfigureSender(email, request);
        ConfigureRecipients(email, request);
        ConfigureContent(email, request);

        return email;
    }

    private void ConfigureSender(MimeMessage email, MailRequest request)
    {
        email.From.Add(new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From));
        email.Sender = new MailboxAddress(request.DisplayName ?? _settings.DisplayName, request.From ?? _settings.From);
    }

    private void ConfigureRecipients(MimeMessage email, MailRequest request)
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

    private async Task SendEmailAsync(MimeMessage email, CancellationToken ct)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_settings.Smtp!.Host, _settings.Smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_settings.Smtp.UserName, _settings.Smtp.Password, ct);
            await client.SendAsync(email, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email: {Message}", ex.Message);
            throw new InvalidOperationException("Failed to send email.", ex);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
