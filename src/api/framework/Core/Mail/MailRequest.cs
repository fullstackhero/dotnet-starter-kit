using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace FSH.Framework.Core.Mail;
public class MailRequest(Collection<string> to, string subject, string? body = null, string? from = null, string? displayName = null, string? replyTo = null, string? replyToName = null, Collection<string>? bcc = null, Collection<string>? cc = null, IDictionary<string, byte[]>? attachmentData = null, IDictionary<string, string>? headers = null)
{
    public Collection<string> To { get; } = to;

    public string Subject { get; } = subject;

    public string? Body { get; } = body;

    public string? From { get; } = from;

    public string? DisplayName { get; } = displayName;

    public string? ReplyTo { get; } = replyTo;

    public string? ReplyToName { get; } = replyToName;

    public Collection<string> Bcc { get; } = bcc ?? new Collection<string>(new List<string>());

    public Collection<string> Cc { get; } = cc ?? new Collection<string>(new List<string>());

    public IDictionary<string, byte[]> AttachmentData { get; } = attachmentData ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Headers { get; } = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
