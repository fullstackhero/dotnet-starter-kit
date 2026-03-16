namespace FSH.Framework.Mailing.Options;
public class MailOptions
{
    public string? Provider { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
    public SmtpOptions? Smtp { get; set; }
    public SendGridOptions? SendGrid { get; set; }
    public AzureOptions? Azure { get; set; }
}
public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool UseStartTls { get; set; }
    public bool UseAuthentication { get; set; }
}
public class SendGridOptions
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
}

public class AzureOptions
{
    public string ConnectionString { get; set; } = null!;
}
