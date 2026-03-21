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
        //services.AddOptions<MailOptions>()
        //    .BindConfiguration(nameof(MailOptions))
        //    .ValidateOnStart();

        //services.AddTransient<IMailComposer<MimeMessage>, MimeKitEmailComposer>();
        //services.AddTransient<IMailComposer<SendGridMessage>, SendGridMailComposer>();
        //services.AddTransient<IMailComposer<FakeMimeMessage>, FakeMailComposer>();
        //services.AddTransient<IMailComposer<AzureMailMessage>, AzureMailComposer>();

        //services.AddTransient<IMailTransport<MimeMessage>, SmtpMailTransport>();
        //services.AddTransient<IMailTransport<SendGridMessage>, SendGridMailTransport>();
        //services.AddTransient<IMailTransport<FakeMimeMessage>, FakeMailTransport>();
        //services.AddTransient<IMailTransport<AzureMailMessage>, AzureMailTransport>();

        //services.AddTransient<IMailService>(sp =>
        //{
        //    var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;

        //    return options.Provider switch
        //    {
        //        "SMTP" => new MailService<MimeMessage>(
        //            sp.GetRequiredService<IMailComposer<MimeMessage>>(),
        //            sp.GetRequiredService<IMailTransport<MimeMessage>>()),

        //        "SendGrid" => new MailService<SendGridMessage>(
        //            sp.GetRequiredService<IMailComposer<SendGridMessage>>(),
        //            sp.GetRequiredService<IMailTransport<SendGridMessage>>()),

        //        "Fake" => new MailService<FakeMimeMessage>(
        //            sp.GetRequiredService<IMailComposer<FakeMimeMessage>>(),
        //            sp.GetRequiredService<IMailTransport<FakeMimeMessage>>()),

        //        "Azure" => new MailService<AzureMailMessage>(
        //            sp.GetRequiredService<IMailComposer<AzureMailMessage>>(),
        //            sp.GetRequiredService<IMailTransport<AzureMailMessage>>()),

        //        _ => throw new NotSupportedException($"Mail provider {options.Provider} not supported")
        //    };
        //});

        //return services;

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

        services.AddTransient<IMailProvider, SmtpMailProvider>();
        services.AddTransient<IMailProvider, SendGridMailProvider>();
        services.AddTransient<IMailProvider, AzureMailProvider>();
        services.AddTransient<IMailProvider, FakeMailProvider>();

        services.AddSingleton<MailProviderFactory>();

        services.AddTransient<IMailService, MailService>();

        return services;
    }
}