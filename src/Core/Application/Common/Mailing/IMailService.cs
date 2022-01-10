using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Application.Common.Mailing;

public interface IMailService : ITransientService
{
    Task SendAsync(MailRequest request);
}