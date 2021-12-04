using DN.WebApi.Shared.DTOs.Mails;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IMailService : ITransientService
{
    Task SendAsync(MailRequest request);
}