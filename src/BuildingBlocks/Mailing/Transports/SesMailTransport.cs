using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Transports;

public class SesMailTransport(
    IOptions<MailOptions> settings,
    ILogger<SesMailTransport> logger)
    : IMailTransport<SesMailMessage>
{
    private readonly MailOptions _settings = settings.Value;
    private readonly ILogger<SesMailTransport> _logger = logger;

    private AmazonSimpleEmailServiceClient? _client;
    private AmazonSimpleEmailServiceClient Client =>
    _client ??= new AmazonSimpleEmailServiceClient(
        _settings.Ses!.AccessKey,
        _settings.Ses.SecretKey,
        RegionEndpoint.GetBySystemName(_settings.Ses.Region));

    public async Task SendAsync(SesMailMessage message, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var request = new SendEmailRequest
            {
                Source = message.From,
                Destination = new Destination
                {
                    ToAddresses = message.To,
                    CcAddresses = message.Cc,
                    BccAddresses = message.Bcc
                },
                Message = new Message
                {
                    Subject = new Content(message.Subject),
                    Body = new Body
                    {
                        Text = new Content(message.Body)
                    }
                }
            };

            var response = await Client.SendEmailAsync(request, ct);

            _logger.LogInformation("SES Email sent. MessageId: {MessageId}", response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SES email: {Message}", ex.Message);
            throw;
        }
    }
}