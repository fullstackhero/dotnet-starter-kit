namespace FSH.Framework.Mailing;

public class MailOptions
{
    public bool UseSendGrid { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
    public SmtpOptions? Smtp { get; set; }
    public SendGridOptions? SendGrid { get; set; }
}

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class SendGridOptions
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
}
