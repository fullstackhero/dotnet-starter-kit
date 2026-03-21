using FSH.Framework.Mailing.Messages;

namespace FSH.Framework.Mailing.Contracts;
public interface IMailComposer<out T>
{
    T Compose(MailRequest request, CancellationToken ct);
}
