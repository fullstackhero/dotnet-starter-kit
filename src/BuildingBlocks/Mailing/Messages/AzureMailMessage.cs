using System.Collections.ObjectModel;

namespace FSH.Framework.Mailing.Messages;

public class AzureEmailMessage
{
    public string From { get; set; } = null!;

    public string? DisplayName { get; set; }

    public Collection<string> To { get; set; } = new();

    public Collection<string> Cc { get; set; } = new();

    public Collection<string> Bcc { get; set; } = new();

    public string Subject { get; set; } = null!;

    public string? Body { get; set; }

    public IDictionary<string, byte[]> Attachments { get; set; } = new Dictionary<string, byte[]>();
}