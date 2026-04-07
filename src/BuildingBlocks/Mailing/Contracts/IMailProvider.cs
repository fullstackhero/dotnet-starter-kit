using FSH.Framework.Mailing.Messages;
namespace FSH.Framework.Mailing.Contracts;

public interface IMailProvider
{
    MailProviderType ProviderType { get; }

    Task SendAsync(MailRequest request, CancellationToken ct);
}