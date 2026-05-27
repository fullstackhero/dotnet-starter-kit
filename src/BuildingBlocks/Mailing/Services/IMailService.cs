namespace FSH.Framework.Mailing.Services;

public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken ct);
}