using Azure.Communication.Email;
using FSH.Framework.Mailing.Composers;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Mailing.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid.Helpers.Mail;

namespace FSH.Framework.Mailing;
public static class Extensions
{
    public static IServiceCollection AddHeroMailing(this IServiceCollection services)
    {
        services.AddOptions<MailOptions>()
            .BindConfiguration(nameof(MailOptions))
            .ValidateOnStart();

        services.AddTransient<IMailComposer<MimeMessage>, MimeKitEmailComposer>();
        services.AddTransient<IMailComposer<SendGridMessage>, SendGridMailComposer>();
        services.AddTransient<IMailComposer<FakeMimeMessage>, FakeMailComposer>();

        services.AddTransient<IMailTransport<MimeMessage>, SmtpMailTransport>();
        services.AddTransient<IMailTransport<SendGridMessage>, SendGridMailTransport>();
        services.AddTransient<IMailTransport<FakeMimeMessage>, FakeMailTransport>();

        services.AddTransient<IMailService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;

            return options.Provider switch
            {
                "SMTP" => new MailService<MimeMessage>(
                    sp.GetRequiredService<IMailComposer<MimeMessage>>(),
                    sp.GetRequiredService<IMailTransport<MimeMessage>>()),

                "SendGrid" => new MailService<SendGridMessage>(
                    sp.GetRequiredService<IMailComposer<SendGridMessage>>(),
                    sp.GetRequiredService<IMailTransport<SendGridMessage>>()),

                "Fake" => new MailService<FakeMimeMessage>(
                    sp.GetRequiredService<IMailComposer<FakeMimeMessage>>(),
                    sp.GetRequiredService<IMailTransport<FakeMimeMessage>>()),

                _ => throw new NotSupportedException($"Mail provider {options.Provider} not supported")
            };
        });

        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["Azure:ConnectionString"];
            return new EmailClient(connectionString);
        });

        return services;
    }
}