using System.Globalization;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Builds the subject + HTML body + text/plain alternative for tenant billing emails. Plain interpolated
/// HTML (the framework has no template engine); kept here so the handlers stay thin and the copy is easy
/// to review. Every message carries both parts: HTML-only mail leaves text-only clients with nothing and
/// scores worse with spam filters.
/// </summary>
internal static class BillingEmailBodies
{
    private static string Date(DateTime utc) => utc.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    public static (string Subject, string Body, string TextBody) NearingExpiry(string tenantName, string? planKey, DateTime validUpto, int daysRemaining)
    {
        var subject = daysRemaining <= 1
            ? "Your subscription expires tomorrow"
            : $"Your subscription expires in {daysRemaining} days";
        var plan = planKey ?? "current";
        var body = Wrap(subject,
            $"<p>Hi {Escape(tenantName)},</p>" +
            $"<p>Your <strong>{Escape(plan)}</strong> subscription is valid until " +
            $"<strong>{Date(validUpto)}</strong> ({daysRemaining} day(s) remaining).</p>" +
            "<p>Please contact your account operator to renew and avoid any interruption to your service.</p>");
        var text = Text(subject,
            $"Hi {tenantName},",
            $"Your {plan} subscription is valid until {Date(validUpto)} ({daysRemaining} day(s) remaining).",
            "Please contact your account operator to renew and avoid any interruption to your service.");
        return (subject, body, text);
    }

    public static (string Subject, string Body, string TextBody) EnteredGrace(string tenantName, string? planKey, DateTime validUpto, DateTime graceEnds)
    {
        const string subject = "Your subscription has lapsed — grace period active";
        var plan = planKey ?? "current";
        var body = Wrap(subject,
            $"<p>Hi {Escape(tenantName)},</p>" +
            $"<p>Your <strong>{Escape(plan)}</strong> subscription expired on " +
            $"<strong>{Date(validUpto)}</strong>. Your service continues during a grace period that ends on " +
            $"<strong>{Date(graceEnds)}</strong>.</p>" +
            "<p>Please renew before the grace period ends to keep your access uninterrupted.</p>");
        var text = Text(subject,
            $"Hi {tenantName},",
            $"Your {plan} subscription expired on {Date(validUpto)}. Your service continues during a grace period that ends on {Date(graceEnds)}.",
            "Please renew before the grace period ends to keep your access uninterrupted.");
        return (subject, body, text);
    }

    public static (string Subject, string Body, string TextBody) Expired(string tenantName, string? planKey, DateTime validUpto)
    {
        const string subject = "Your subscription has expired";
        var plan = planKey ?? "current";
        var body = Wrap(subject,
            $"<p>Hi {Escape(tenantName)},</p>" +
            $"<p>Your <strong>{Escape(plan)}</strong> subscription expired on " +
            $"<strong>{Date(validUpto)}</strong> and the grace period has ended, so access is now suspended.</p>" +
            "<p>Contact your account operator to renew and restore access.</p>");
        var text = Text(subject,
            $"Hi {tenantName},",
            $"Your {plan} subscription expired on {Date(validUpto)} and the grace period has ended, so access is now suspended.",
            "Contact your account operator to renew and restore access.");
        return (subject, body, text);
    }

    public static (string Subject, string Body, string TextBody) InvoiceIssued(string invoiceNumber, decimal amount, string currency, DateTime? dueAtUtc)
    {
        var subject = $"Invoice {invoiceNumber} issued";
        var amountText = $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currency}";
        var due = dueAtUtc is null ? string.Empty : $"<p>Due by <strong>{Date(dueAtUtc.Value)}</strong>.</p>";
        var body = Wrap(subject,
            $"<p>A new invoice <strong>{Escape(invoiceNumber)}</strong> for <strong>{amountText}</strong> has been issued.</p>" +
            due +
            "<p>You can view and download this invoice from your dashboard.</p>");
        var text = Text(subject,
            $"A new invoice {invoiceNumber} for {amountText} has been issued.",
            dueAtUtc is null ? string.Empty : $"Due by {Date(dueAtUtc.Value)}.",
            "You can view and download this invoice from your dashboard.");
        return (subject, body, text);
    }

    private static string Wrap(string heading, string innerHtml) =>
        "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1a1a1a;line-height:1.5\">" +
        $"<h2 style=\"font-size:18px;margin:0 0 12px\">{Escape(heading)}</h2>" +
        innerHtml +
        "<p style=\"margin-top:24px;color:#6b7280;font-size:12px\">This is an automated message.</p>" +
        "</div>";

    /// <summary>
    /// The text/plain twin of <see cref="Wrap"/>: same copy, no markup, empty paragraphs dropped so an
    /// optional line (an absent due date) does not leave a blank gap.
    /// </summary>
    private static string Text(string heading, params string[] paragraphs)
    {
        var lines = new List<string> { heading, string.Empty };
        lines.AddRange(paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        lines.Add(string.Empty);
        lines.Add("This is an automated message.");
        return string.Join(Environment.NewLine + Environment.NewLine, lines.Where(l => l.Length > 0));
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
             .Replace("<", "&lt;", StringComparison.Ordinal)
             .Replace(">", "&gt;", StringComparison.Ordinal);
}
