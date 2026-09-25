using System.Collections.ObjectModel;

namespace FSH.Framework.Mailing;

public class MailRequest(Collection<string> to, string subject, string? body = null, string? from = null, string? displayName = null, string? replyTo = null, string? replyToName = null, Collection<string>? bcc = null, Collection<string>? cc = null, IDictionary<string, byte[]>? attachmentData = null, IDictionary<string, string>? headers = null, string? textBody = null)
{
    public Collection<string> To { get; } = to;

    public string Subject { get; } = subject;

    /// <summary>
    /// The HTML body. Every provider sends this as <c>text/html</c>, so a caller that passes bare text
    /// gets a message whose URLs are not anchors — most clients do not auto-link inside HTML — and whose
    /// interpolated values are parsed as markup. Build real HTML here and put the fallback in
    /// <see cref="TextBody"/>.
    /// </summary>
    public string? Body { get; } = body;

    /// <summary>
    /// Optional <c>text/plain</c> alternative, sent alongside <see cref="Body"/> as multipart/alternative.
    /// Clients that cannot render HTML (and spam filters, which score HTML-only mail worse) fall back to it.
    /// </summary>
    public string? TextBody { get; } = textBody;

    public string? From { get; } = from;

    public string? DisplayName { get; } = displayName;

    public string? ReplyTo { get; } = replyTo;

    public string? ReplyToName { get; } = replyToName;

    public Collection<string> Bcc { get; } = bcc ?? new Collection<string>();

    public Collection<string> Cc { get; } = cc ?? new Collection<string>();

    public IDictionary<string, byte[]> AttachmentData { get; } = attachmentData ?? new Dictionary<string, byte[]>();

    public IDictionary<string, string> Headers { get; } = headers ?? new Dictionary<string, string>();
}