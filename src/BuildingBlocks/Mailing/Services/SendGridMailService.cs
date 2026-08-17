using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SendGridMailService> _logger;

    public SendGridMailService(IOptions<MailOptions> settings, ISendGridClient client, ILogger<SendGridMailService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        _settings = settings.Value;
        _client = client;
        _logger = logger;
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
        // plainTextContent and htmlContent are distinct parts: passing Body to both shipped the HTML
        // template as the text alternative, so a text-only client rendered raw markup.
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(request.To[0]),
            request.Subject,
            request.TextBody,
            request.Body);

        ConfigureRecipients(msg, request);
        AddAttachments(msg, request);

        var response = await _client.SendEmailAsync(msg, ct).ConfigureAwait(false);

        // The client is built with HttpErrorAsException=false, so a non-2xx reply (bad key, rejected
        // recipient, rate limit) comes back as a Response instead of throwing. A silently discarded
        // failure makes the caller believe the mail was delivered, so it must never be ignored.
        if (IsSuccess(response.StatusCode))
        {
            return;
        }

        var status = (int)response.StatusCode;
        var body = response.Body is not null
            ? await response.Body.ReadAsStringAsync(ct).ConfigureAwait(false)
            : string.Empty;

        // Only retry failures that can plausibly succeed later — 429 (rate limited) and 5xx
        // (SendGrid-side). Throwing routes these back through the caller's Hangfire automatic retry.
        // A permanent 4xx (bad API key, rejected recipient) will never succeed on retry, so log it
        // loudly and return instead of throwing — otherwise every enqueued send retries ~10x and
        // floods the dead-letter queue with attempts that cannot succeed.
        if (status == 429 || status >= 500)
        {
            throw new InvalidOperationException(
                $"SendGrid transiently failed the message with status {status}. {body}".TrimEnd());
        }

        _logger.LogError(
            "SendGrid permanently rejected the message with status {StatusCode}. {Body}",
            status,
            body);
    }

    private static bool IsSuccess(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode is >= 200 and < 300;

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