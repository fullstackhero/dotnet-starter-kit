using FSH.WebApi.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FSH.WebApi.Infrastructure.BackgroundJobs.RecurringJobs.Initialization;

internal static class Startup
{
    internal static IServiceCollection AddRecurringBackgroundJobs(this IServiceCollection services)
    {
        services.AddServices(typeof(IRecurringJobInitialization), ServiceLifetime.Transient);

        return services;
    }
}
