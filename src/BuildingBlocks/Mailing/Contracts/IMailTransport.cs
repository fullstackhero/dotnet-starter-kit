using MimeKit;

namespace FSH.Framework.Mailing.Contracts;
public interface IMailTransport
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}
