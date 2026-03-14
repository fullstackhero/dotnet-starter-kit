using FSH.Framework.Mailing.Composers;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Options;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Mailing.Transports;
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

        services.AddTransient <IMailComposer, MimeKitEmailComposer>();

        services.AddTransient<SmtpMailTransport>();
        services.AddTransient<SendGridMailTransport>();
        //services.AddTransient<FakeEmailTransport>();

        services.AddTransient<IMailTransport>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;

            return options.Provider switch
            {
                "SMTP" => sp.GetRequiredService<SmtpMailTransport>(),
                //"SendGrid" => sp.GetRequiredService<SendGridMailTransport>(),
                //"Fake" => sp.GetRequiredService<FakeEmailTransport>(),
                _ => throw new NotSupportedException()
            };
        });

        services.AddTransient<IMailService, MailService>();

        return services;
    }
}