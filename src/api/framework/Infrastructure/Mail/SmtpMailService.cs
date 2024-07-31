namespace FSH.Framework.Infrastructure.Mail;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Mail;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

public class SmtpMailService(IOptions<MailOptions> settings, ILogger<SmtpMailService> logger) : IMailService
{
    private readonly MailOptions _settings = settings.Value;
    private readonly ILogger<SmtpMailService> _logger = logger;

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        using var email = new MimeMessage();

        // From
        email.From.Add(new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From));

        // To
        foreach (string address in request.To)
            email.To.Add(MailboxAddress.Parse(address));

        // Reply To
        if (!string.IsNullOrEmpty(request.ReplyTo))
            email.ReplyTo.Add(new MailboxAddress(request.ReplyToName, request.ReplyTo));

        // Bcc
        if (request.Bcc != null)
        {
            foreach (string address in request.Bcc.Where(bccValue => !string.IsNullOrWhiteSpace(bccValue)))
                email.Bcc.Add(MailboxAddress.Parse(address.Trim()));
        }

        // Cc
        if (request.Cc != null)
        {
            foreach (string? address in request.Cc.Where(ccValue => !string.IsNullOrWhiteSpace(ccValue)))
                email.Cc.Add(MailboxAddress.Parse(address.Trim()));
        }

        // Headers
        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
                email.Headers.Add(header.Key, header.Value);
        }

        // Content
        var builder = new BodyBuilder();
        email.Sender = new MailboxAddress(request.DisplayName ?? _settings.DisplayName, request.From ?? _settings.From);
        email.Subject = request.Subject;
        builder.HtmlBody = request.Body;

        // Create the file attachments for this e-mail message
        if (request.AttachmentData != null)
        {
            foreach (var attachmentInfo in request.AttachmentData)
            {
                using (var stream = new MemoryStream())
                {
                    await stream.WriteAsync(attachmentInfo.Value, ct);
                    stream.Position = 0;
                    await builder.Attachments.AddAsync(attachmentInfo.Key, stream, ct);
                }
            }
        }

        email.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
            await client.SendAsync(email, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email: {Message}", ex.Message);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
