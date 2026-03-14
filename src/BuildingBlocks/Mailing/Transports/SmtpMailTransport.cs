using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Options;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace FSH.Framework.Mailing.Transports;
public class SmtpMailTransport(IOptions<MailOptions> settings, ILogger<SmtpMailTransport> logger) : IMailTransport<MimeMessage>
{
    private readonly MailOptions _settings = settings.Value;
    private readonly ILogger<SmtpMailTransport> _logger = logger;

    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();

        try
        {
            var secureOption = _settings!.Smtp!.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.Smtp.Host,
                _settings.Smtp.Port,
                secureOption,
                ct);

            if (_settings.Smtp.UseAuthentication)
            {
                await client.AuthenticateAsync(
                    _settings.Smtp.UserName,
                    _settings.Smtp.Password,
                    ct);
            }

            await client.SendAsync(message, ct);
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
