using FSH.Framework.Mailing.Composers;
using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Factory;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using FSH.Framework.Mailing.Providers;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Mailing.Transports;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddTransient<IMailTransport<MimeMessage>, SmtpMailTransport>();

        services.AddTransient<IMailComposer<SendGridMessage>, SendGridMailComposer>();
        services.AddTransient<IMailTransport<SendGridMessage>, SendGridMailTransport>();

        services.AddTransient<IMailComposer<AzureMailMessage>, AzureMailComposer>();
        services.AddTransient<IMailTransport<AzureMailMessage>, AzureMailTransport>();

        services.AddTransient<IMailComposer<FakeMimeMessage>, FakeMailComposer>();
        services.AddTransient<IMailTransport<FakeMimeMessage>, FakeMailTransport>();

        services.AddTransient<IMailComposer<SesMailMessage>, SesMailComposer>();
        services.AddTransient<IMailTransport<SesMailMessage>, SesMailTransport>();

        services.AddTransient<IMailComposer<FluentMailMessage>, FluentMailComposer>();
        services.AddTransient<IMailTransport<FluentMailMessage>, FluentMailTransport>();

        services.AddTransient<IMailProvider, SmtpMailProvider>();
        services.AddTransient<IMailProvider, SendGridMailProvider>();
        services.AddTransient<IMailProvider, AzureMailProvider>();
        services.AddTransient<IMailProvider, FakeMailProvider>();
        services.AddTransient<IMailProvider, SesMailProvider>();
        services.AddTransient<IMailProvider, FluentMailProvider>();

        services.AddSingleton<MailProviderFactory>();

        services.AddTransient<IMailService, MailService>();

        return services;
    }
}