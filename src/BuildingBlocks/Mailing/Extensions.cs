using FSH.Framework.Mailing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid;

namespace FSH.Framework.Mailing;

public static class Extensions
{
    public static IServiceCollection AddHeroMailing(this IServiceCollection services)
    {
        services.AddOptions<MailOptions>()
            .BindConfiguration(nameof(MailOptions))
            .ValidateOnStart();

        // One SendGrid client (and its underlying HttpClient) shared process-wide.
        // Constructing it per-send leaks sockets / exhausts ephemeral ports under load.
        // The factory is lazy, so the client is only built when SendGrid is actually used.
        services.AddSingleton<ISendGridClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;
            return new SendGridClient(options.SendGrid?.ApiKey ?? string.Empty);
        });

        services.AddTransient<IMailService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MailOptions>>().Value;
            if (options.UseSendGrid)
            {
                return new SendGridMailService(
                    sp.GetRequiredService<IOptions<MailOptions>>(),
                    sp.GetRequiredService<ISendGridClient>());
            }
            return new SmtpMailService(sp.GetRequiredService<IOptions<MailOptions>>(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmtpMailService>>());
        });
        return services;
    }
}