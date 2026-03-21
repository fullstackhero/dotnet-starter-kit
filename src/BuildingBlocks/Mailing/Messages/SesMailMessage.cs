namespace FSH.Framework.Mailing.Messages;

public class SesMailMessage
{
    public string From { get; set; } = null!;
    public List<string> To { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}