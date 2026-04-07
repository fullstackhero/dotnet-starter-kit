namespace FSH.Framework.Mailing.Messages;

public abstract class CloudMailMessage
{
    public string From { get; set; } = null!;
    public string? DisplayName { get; set; }

    public List<string> To { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];

    public string Subject { get; set; } = null!;
    public string? Body { get; set; }

    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}