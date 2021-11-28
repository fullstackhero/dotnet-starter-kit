using DN.WebApi.Shared.DTOs.General.Requests;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IMailService : ITransientService
{
    Task SendAsync(MailRequest request);
}