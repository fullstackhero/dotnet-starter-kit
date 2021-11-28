using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Settings;
using DN.WebApi.Shared.DTOs.General.Requests;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DN.WebApi.Infrastructure.Common.Services;

public class SmtpMailService : IMailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<SmtpMailService> _logger;

    public SmtpMailService(IOptions<MailSettings> settings, ILogger<SmtpMailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailRequest request)
    {
        try
        {
            var email = new MimeMessage
            {
                Sender = new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From),
                From = { new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From) },
                Subject = request.Subject,
                Body = new BodyBuilder
                {
                    HtmlBody = request.Body
                }.ToMessageBody()
            };
            email.To.Add(MailboxAddress.Parse(request.To));
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }
}