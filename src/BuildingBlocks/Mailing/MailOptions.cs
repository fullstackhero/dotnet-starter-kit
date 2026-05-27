namespace FSH.Framework.Mailing;

public sealed class MailOptions
{
    public bool UseSendGrid { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
    public SmtpOptions? Smtp { get; set; }
    public SendGridOptions? SendGrid { get; set; }
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public sealed class SendGridOptions
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
}