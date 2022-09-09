using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Infrastructure.Mailing;

internal static class Startup
{
    internal static IServiceCollection AddMailing(this IServiceCollection services, IConfiguration config) {
        if (config.GetSection("FeatureFlagSettings").GetSection("Mail").Value == "True") {
            services.Configure<MailSettings>(config.GetSection(nameof(MailSettings)));
        }
        return services;
    }
}