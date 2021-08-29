using System.Reflection;
using DN.WebApi.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            return services;
        }
    }
}