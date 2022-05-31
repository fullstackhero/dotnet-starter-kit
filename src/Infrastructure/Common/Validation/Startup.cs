using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.WebApi.Infrastructure.Common.Validation;

internal static class Startup
{
    public static IServiceCollection AddFluentValidation(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        Func<AssemblyScanner.AssemblyScanResult, bool>? filter = null)
    {
        services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>), lifetime));

        services.AddValidatorsFromAssemblies(assemblies, lifetime, filter);

        return services;
    }
}