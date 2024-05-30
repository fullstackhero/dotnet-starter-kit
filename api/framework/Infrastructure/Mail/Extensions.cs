using FSH.Framework.Core.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Mail;
internal static class Extensions
{
    internal static IServiceCollection ConfigureMailing(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MailOptions>(config.GetSection(nameof(MailOptions)));
        return services;
    }
}
