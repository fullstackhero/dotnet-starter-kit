namespace FSH.Framework.Mailing.Contracts;
public interface IMailTransport<in T>
{
    Task SendAsync(T message, CancellationToken ct);
}
