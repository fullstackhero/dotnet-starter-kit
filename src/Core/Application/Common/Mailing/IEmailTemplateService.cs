namespace FSH.WebAPI.Application.Common.Mailing;

public interface IEmailTemplateService : ITransientService
{
    string GenerateEmailConfirmationMail(string userName, string email, string emailVerificationUri);
}