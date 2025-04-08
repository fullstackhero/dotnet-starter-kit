namespace FSH.Framework.Core.Mail;
public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken ct);
}
