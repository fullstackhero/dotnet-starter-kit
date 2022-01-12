namespace FSH.WebApi.Application.Common.Mailing;

public class MailRequest
{
    public MailRequest(List<string> to, string subject, string? body = null, string? from = null, string? displayName = null, string? replyTo = null, string? replyToName = null, List<string>? bcc = null, List<string>? cc = null, IDictionary<string, byte[]>? attachmentData = null, IDictionary<string, string>? headers = null)
    {
        To = to;
        Subject = subject;
        Body = body;
        From = from;
        DisplayName = displayName;
        ReplyTo = replyTo;
        ReplyToName = replyToName;
        Bcc = bcc ?? new List<string>();
        Cc = cc ?? new List<string>();
        AttachmentData = attachmentData ?? new Dictionary<string, byte[]>();
        Headers = headers ?? new Dictionary<string, string>();
    }

    public List<string> To { get; }

    public string Subject { get; }

    public string? Body { get; }

    public string? From { get; }

    public string? DisplayName { get; }

    public string? ReplyTo { get; }

    public string? ReplyToName { get; }

    public List<string> Bcc { get; }

    public List<string> Cc { get; }

    public IDictionary<string, byte[]> AttachmentData { get; }

    public IDictionary<string, string> Headers { get; }
}