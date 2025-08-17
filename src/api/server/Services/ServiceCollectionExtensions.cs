using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWhatsAppProvider(this IServiceCollection services, IConfiguration config)
    {
        var provider = config.GetSection(WhatsAppOptions.Section).GetValue<string>("Provider") ?? "Stub";
        if (provider.Equals("Twilio", System.StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();
        else
            services.AddScoped<IWhatsAppService, WhatsAppStubService>();
        return services;
    }
}
