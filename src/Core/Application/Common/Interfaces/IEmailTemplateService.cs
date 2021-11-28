using DN.WebApi.Application.Abstractions.Services;

namespace DN.WebApi.Application.Common.Interfaces;

public interface IEmailTemplateService : ITransientService
{
    string GenerateEmailConfirmationMail(string userName, string email, string emailVerificationUri);
}