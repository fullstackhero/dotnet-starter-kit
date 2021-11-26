using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class SeedersExtensions
    {
        public static IServiceCollection AddSeeders(this IServiceCollection services)
        {
            var dbSeederType = typeof(IDatabaseSeeder);
            var dbSeeders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => dbSeederType.IsAssignableFrom(p))
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.GetInterfaces().FirstOrDefault(),
                    Implementation = t
                })
                .Where(t => t.Service != null);

            foreach (var dbSeeder in dbSeeders)
            {
                if (dbSeederType.IsAssignableFrom(dbSeeder.Service))
                {
                    services.AddTransient(dbSeeder.Service, dbSeeder.Implementation);
                }
            }

            return services;
        }
    }
}