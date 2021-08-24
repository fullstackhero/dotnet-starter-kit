using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Utilties;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseContext<T>(this IServiceCollection services, IConfiguration config) where T : ApplicationDbContext
        {

            services.AddDbContext<T>(m => m.UseNpgsql(e => e.MigrationsAssembly(typeof(T).Assembly.FullName)));
            services.Configure<TenantSettings>(config.GetSection(nameof(TenantSettings)));
            var options = services.GetOptions<TenantSettings>(nameof(TenantSettings));
            var defaultConnectionString = options.Defaults?.ConnectionString;
            var defaultDbProvider = options.Defaults?.DBProvider;
            //other tenants
            var tenants = options.Tenants;
            foreach (var tenant in tenants)
            {
                string connectionString;
                if (string.IsNullOrEmpty(tenant.ConnectionString))
                {
                    connectionString = defaultConnectionString;
                }
                else
                {
                    connectionString = tenant.ConnectionString;
                }
                if (defaultDbProvider.ToLower() == "postgresql")
                {
                    services.AddPostgres<T>(connectionString, tenant.Name);
                }
            }
            return services;
        }
        private static IServiceCollection AddPostgres<T>(this IServiceCollection services, string connectionString, string tenantName) where T : ApplicationDbContext
        {
            var options = services.GetOptions<TenantSettings>(nameof(TenantSettings));
            var tenant = options.Tenants.Where(a => a.Name == tenantName).FirstOrDefault();
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<T>();
            dbContext.Database.SetConnectionString(connectionString);
            dbContext.Database.Migrate();

            foreach (string role in typeof(RoleConstants).GetAllPublicConstantValues<string>())
            {
                var roleStore = new RoleStore<ExtendedRole>(dbContext);

                if (!dbContext.Roles.Any(r => r.Name == role))
                {
                    roleStore.CreateAsync(new ExtendedRole(role)).Wait();
                }
            }
            var superUser = new ExtendedUser
            {
                FirstName = tenant.Name,
                Email = tenant.AdminEmail,
                UserName = tenant.Name,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };
            if (!dbContext.Users.Any(u => u.UserName == tenant.AdminEmail))
            {
                var password = new PasswordHasher<ExtendedUser>();
                var hashed = password.HashPassword(superUser, UserConstants.DefaultPassword);
                superUser.PasswordHash = hashed;
                var userStore = new UserStore<ExtendedUser>(dbContext);
                userStore.CreateAsync(superUser).Wait();
                userStore.AddToRoleAsync(superUser, RoleConstants.Admin);
            }



            services.AddHangfire(x => x.UsePostgreSqlStorage(connectionString));
            return services;
        }

        private static IServiceCollection AddMSSQL<T>(this IServiceCollection services, string connectionString) where T : DbContext
        {
            services.AddDbContext<T>(m => m.UseSqlServer(connectionString, e => e.MigrationsAssembly(typeof(T).Assembly.FullName)));
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<T>();
            dbContext.Database.Migrate();
            services.AddHangfire(x => x.UseSqlServerStorage(connectionString));
            return services;
        }

        public static T GetOptions<T>(this IServiceCollection services, string sectionName) where T : new()
        {
            using var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var section = configuration.GetSection(sectionName);
            var options = new T();
            section.Bind(options);

            return options;
        }
    }
}