using DN.WebApi.Shared.DTOs.General.Requests;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IMailService : ITransientService
    {
        Task SendAsync(MailRequest request);
    }
}