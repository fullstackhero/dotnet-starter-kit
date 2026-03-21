using FluentEmail.Core;
using FluentEmail.Smtp;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace FSH.Framework.Mailing.Transports;

public class FluentMailTransport(
    IOptions<MailOptions> settings,
    ILogger<FluentMailTransport> logger)
    : IMailTransport<FluentMailMessage>
{
    private readonly MailOptions _settings = settings.Value;
    private readonly ILogger<FluentMailTransport> _logger = logger;

    public async Task SendAsync(FluentMailMessage message, CancellationToken ct)
    {
        try
        {
            var fluentOptions = _settings.FluentMail!;
            var smtpClient = new SmtpClient(fluentOptions.Host!, fluentOptions.Port!);
            var sender = new SmtpSender(smtpClient);

            Email.DefaultSender = sender;

            var email = Email.From(fluentOptions.DefaultFromEmail!, fluentOptions.DefaultFromName)
                .Subject(message.Request.Subject)
                .Body(message.Request.Body ?? "");

            foreach (var to in message.Request.To)
                email.To(to);

            foreach (var cc in message.Request.Cc)
                email.CC(cc);

            foreach (var Bcc in message.Request.Bcc)
                email.BCC(Bcc);

            var response = await email.SendAsync(ct);

            if (!response.Successful)
            {
                throw new InvalidOperationException(
                    string.Join(", ", response.ErrorMessages));
            }

            _logger.LogInformation("FluentEmail sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FluentEmail: {Message}", ex.Message);
            throw;
        }
    }
}