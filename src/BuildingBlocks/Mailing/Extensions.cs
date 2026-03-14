using FSH.Framework.Mailing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing;

public static class Extensions
{
    public static IServiceCollection AddHeroMailing(this IServiceCollection services)
    {
        services.AddOptions<MailOptions>()
            .BindConfiguration(nameof(MailOptions))
            .ValidateOnStart();

        services.AddTransient<IMailService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;

            return options.Provider switch
            {
                "SMTP" => new SmtpMailService(
                    sp.GetRequiredService<IOptions<MailOptions>>(),
                    sp.GetRequiredService<ILogger<SmtpMailService>>()),

                "SendGrid" => new SendGridMailService(
                    sp.GetRequiredService<IOptions<MailOptions>>()),

                _ => throw new NotSupportedException($"Mail provider {options.Provider} not supported")
            };
        });

        return services;
    }
}