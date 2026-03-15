using Azure.Communication.Email;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Mailing.Transports;

public class AzureMailTransport(
    EmailClient emailClient,
    ILogger<AzureMailTransport> logger) : IMailTransport<AzureEmailMessage>
{
    private readonly EmailClient _client = emailClient;
    private readonly ILogger<AzureMailTransport> _logger = logger;

    public async Task SendAsync(AzureEmailMessage message, CancellationToken ct)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(message);

            var recipients = new EmailRecipients(
                [.. message.To.Select(x => new EmailAddress(x))],
                [.. message.Cc.Select(x => new EmailAddress(x))],
                [.. message.Bcc.Select(x => new EmailAddress(x))]
            );

            var content = new EmailContent(message.Subject)
            {
                PlainText = message.Body
            };

            var emailMessage = new EmailMessage(
                message.From,
                recipients,
                content
            );

            if (message.Attachments.Any())
            {
                foreach (var attachment in message.Attachments)
                {
                    emailMessage.Attachments.Add(
                        new EmailAttachment(
                            attachment.Key,
                            "application/octet-stream",
                            BinaryData.FromBytes(attachment.Value)
                        )
                    );
                }
            }

            var response = await _client.SendAsync(
                Azure.WaitUntil.Completed,
                emailMessage,
                ct);

            _logger.LogInformation(
                "Azure Email sent. MessageId: {MessageId}",
                response.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Email sending failed: {Message}", ex.Message);
            throw;
        }
    }
}