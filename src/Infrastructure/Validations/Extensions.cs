using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSH.WebApi.Infrastructure.Validations;
public static class Extensions
{
    public static IServiceCollection RegisterValidators(this IServiceCollection services, Assembly assemblyContainingValidators)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(assemblyContainingValidators);
        return services;
    }
}

