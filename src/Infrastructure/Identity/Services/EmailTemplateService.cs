using DN.WebApi.Application.Abstractions.Services.General;
using Microsoft.Extensions.Localization;
using System.IO;

namespace DN.WebApi.Infrastructure.Identity.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IStringLocalizer<EmailTemplateService> _localizer;

        public EmailTemplateService(IStringLocalizer<EmailTemplateService> localizer)
        {
            _localizer = localizer;
        }

        public string GenerateEmailConfirmationMail(string userName, string email, string emailVerificationUri)
        {
            string filePath = Directory.GetCurrentDirectory() + "\\Email Templates\\email-confirmation.html";
            StreamReader str = new StreamReader(filePath);
            string mailText = str.ReadToEnd();
            str.Close();
            if (string.IsNullOrEmpty(mailText))
            {
                return string.Format(_localizer["Please confirm your account by <a href='{0}'>clicking here</a>."], emailVerificationUri);
            }

            return mailText.Replace("[userName]", userName).Replace("[email]", email).Replace("[emailVerificationUri]", emailVerificationUri);
        }
    }
}