using MailKit.Security;

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

    // StartTls keeps every existing deployment's behaviour; a local catcher that speaks plain SMTP
    // (Mailpit, MailHog, smtp4dev) needs None, and implicit-TLS port 465 needs SslOnConnect.
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;
}

public sealed class SendGridOptions
{
    public string? ApiKey { get; set; }
    public string? From { get; set; }
    public string? DisplayName { get; set; }
}