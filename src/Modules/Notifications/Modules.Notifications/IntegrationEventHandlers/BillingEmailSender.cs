using System.Collections.ObjectModel;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>Shared best-effort send for billing emails — a delivery failure must never throw out of an
/// integration-event handler (it would fail the originating create/renew/scan).</summary>
internal static class BillingEmailSender
{
    public static async Task SendAsync(
        IMailService mail, ILogger logger, string? email, string subject, string body, string context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        try
        {
            await mail.SendAsync(new MailRequest(
                to: new Collection<string> { email },
                subject: subject,
                body: body), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Do not log the recipient address — it is PII and the log is an external sink
            // (CodeQL cs/exposure-of-sensitive-information). The context identifies the operation.
            logger.LogWarning(ex, "Failed to send {Context} email", context);
        }
    }
}
