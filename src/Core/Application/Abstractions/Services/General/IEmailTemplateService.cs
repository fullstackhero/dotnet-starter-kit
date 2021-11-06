namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IEmailTemplateService : ITransientService
    {
        string GenerateEmailConfirmationMail(string userName, string email, string emailVerificationUri);
    }
}