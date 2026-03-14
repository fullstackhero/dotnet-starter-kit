using FSH.Framework.Mailing.Models;
using MimeKit;

namespace FSH.Framework.Mailing.Contracts;
public interface IMailComposer
{
    Task<MimeMessage> Compose(MailRequest request, CancellationToken ct);
}
