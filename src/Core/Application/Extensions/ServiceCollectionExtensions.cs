using System.Reflection;
using DN.WebApi.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<IRequestValidator>();
            services.AddMediatR(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}