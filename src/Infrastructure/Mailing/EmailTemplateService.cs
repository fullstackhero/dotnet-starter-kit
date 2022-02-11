using System.Text;
using FSH.WebApi.Application.Common.Mailing;
using Microsoft.Extensions.Localization;
using RazorEngineCore;

namespace FSH.WebApi.Infrastructure.Mailing;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IStringLocalizer<EmailTemplateService> _localizer;

    public EmailTemplateService(IStringLocalizer<EmailTemplateService> localizer)
    {
        _localizer = localizer;
    }

    public string GenerateEmailTemplate<T>(string templateName, T mailTemplateModel)
    {
        string template = GetTemplate(templateName);

        IRazorEngine razorEngine = new RazorEngine();
        IRazorEngineCompiledTemplate modifiedTemplate = razorEngine.Compile(template);

        return modifiedTemplate.Run(mailTemplateModel);
    }

    public string GetTemplate(string templateName)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string tmplFolder = Path.Combine(baseDirectory, $"Email Templates");
        string filePath = Path.Combine(tmplFolder, $"{templateName}.cshtml");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.Default);
        string mailText = sr.ReadToEnd();
        sr.Close();

        return mailText;
    }
}