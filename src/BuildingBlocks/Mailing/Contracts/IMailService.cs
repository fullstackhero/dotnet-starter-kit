using FSH.Framework.Mailing.Models;

namespace FSH.Framework.Mailing.Contracts;
public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken ct);
}