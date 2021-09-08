using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Entities.Tenancy;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Utilties;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class TenantDatabaseExtensions
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(TenantDatabaseExtensions));

        public static IServiceCollection SetupRootDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<TenantSettings>(config.GetSection(nameof(TenantSettings)));
            var options = services.GetOptions<TenantSettings>(nameof(TenantSettings));
            var connectionString = options.RootConnectionString;
            var dbProvider = options.RootDBProvider;
            var tenantCode = "root";
            if (dbProvider.ToLower() == "postgresql")
            {
                services.AddDbContext<TenantDbContext>(m => m.UseNpgsql(e => e.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName)));
                services.AddDbContext<ApplicationDbContext>(m => m.UseNpgsql(e => e.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
                using var scope = services.BuildServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
                dbContext.Database.SetConnectionString(connectionString);
                if (dbContext.Database.GetMigrations().Count() > 0)
                {
                    dbContext.Database.Migrate();
                    SeedTenant(tenantCode, dbContext, connectionString);
                }

                services.AddHangfire(x => x.UsePostgreSqlStorage(connectionString));
            }

            return services;
        }

        // public static IServiceCollection PrepareTenantDatabases<T>(this IServiceCollection services, IConfiguration config)
        // where T : ApplicationDbContext
        // {
        //     services.Configure<TenantSettings>(config.GetSection(nameof(TenantSettings)));
        //     var options = services.GetOptions<TenantSettings>(nameof(TenantSettings));
        //     var rootConnectionString = options.RootConnectionString;
        //     var rootDbProvider = options.RootDBProvider;
        //     var rootTenantCode = "root";
        //     var tenants = options.Tenants;
        //     if (rootDbProvider.ToLower() == "postgresql")
        //     {
        //         services.AddDbContext<T>(m => m.UseNpgsql(e => e.MigrationsAssembly(typeof(T).Assembly.FullName)));
        //         services.MigrateAndSeedIdentityData<T>(rootConnectionString, rootTenantCode, options);
        //         services.AddHangfire(x => x.UsePostgreSqlStorage(rootConnectionString));
        //     }

        //     return services;
        // }

        // private static IServiceCollection MigrateAndSeedIdentityData<T>(this IServiceCollection services, string connectionString, string tenantId, TenantSettings options)
        // where T : DbContext
        // {
        //     using var scope = services.BuildServiceProvider().CreateScope();
        //     var dbContext = scope.ServiceProvider.GetRequiredService<T>();

        //     dbContext.Database.SetConnectionString(connectionString);
        //     if (dbContext.Database.GetMigrations().Count() > 0)
        //     {
        //         dbContext.Database.Migrate();
        //         SeedRoles(tenantId, dbContext);
        //         SeedTenantAdmins(tenantId, scope, dbContext, connectionString);
        //         SeedTenant(tenantId, dbContext, connectionString);
        //     }

        //     return services;
        // }
        // #region Seeding
        // private static void SeedTenantAdmins<T>(string tenantCode, IServiceScope scope, T dbContext, string connectionString)
        // where T : ApplicationDbContext
        // {

        //     var adminUserName = $"{tenantCode}.admin";
        //     var superUser = new ApplicationUser
        //     {
        //         FirstName = tenantCode,
        //         LastName = "admin",
        //         Email = "admin@root.com",
        //         UserName = adminUserName,
        //         EmailConfirmed = true,
        //         PhoneNumberConfirmed = true,
        //         NormalizedEmail = "admin@root.com".ToUpper(),
        //         NormalizedUserName = adminUserName.ToUpper(),
        //         IsActive = true,
        //         TenantId = tenantCode
        //     };
        //     if (!dbContext.Users.IgnoreQueryFilters().Any(u => u.Email == "admin@root.com"))
        //     {
        //         var password = new PasswordHasher<ApplicationUser>();
        //         var hashed = password.HashPassword(superUser, UserConstants.DefaultPassword);
        //         superUser.PasswordHash = hashed;
        //         var userStore = new UserStore<ApplicationUser>(dbContext);
        //         userStore.CreateAsync(superUser).Wait();
        //         AssignRolesAsync(scope.ServiceProvider, superUser.Email, RoleConstants.Admin).Wait();
        //     }
        // }

        private static void SeedTenant<T>(string tenantCode, T dbContext, string connectionString)
        where T : TenantDbContext
        {
            var tenant = new Domain.Entities.Tenancy.Tenant()
            {
                Name = tenantCode,
                ConnectionString = connectionString,
                IsActive = true,
                AdminEmail = "admin@root.com",
                Code = tenantCode,
                DBProvider = "postgresql",
                ValidUpto = DateTime.Now.AddMonths(1)
            };
            if (!dbContext.Tenants.Any(a => a.Code == tenantCode))
            {
                dbContext.Tenants.Add(tenant);
                dbContext.SaveChangesAsync().Wait();
            }
        }

        // private static void SeedRoles<T>(string tenantCode, T dbContext)
        // where T : ApplicationDbContext
        // {
        //     foreach (string roleName in typeof(RoleConstants).GetAllPublicConstantValues<string>())
        //     {
        //         var roleStore = new RoleStore<ApplicationRole>(dbContext);
        //         if (!dbContext.Roles.IgnoreQueryFilters().Any(r => r.Name == $"{roleName}"))
        //         {
        //             var role = new ApplicationRole(roleName, tenantCode, $"{roleName} Role for {tenantCode} Tenant");
        //             roleStore.CreateAsync(role).Wait();
        //         }
        //     }
        // }

        // public static async Task<IdentityResult> AssignRolesAsync(IServiceProvider services, string email, string role)
        // {
        //     UserManager<ApplicationUser> userManager = services.GetService<UserManager<ApplicationUser>>();
        //     var user = await userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.Equals(email));
        //     var result = await userManager.AddToRoleAsync(user, role);
        //     return result;
        // }
        // #endregion
        public static T GetOptions<T>(this IServiceCollection services, string sectionName)
        where T : new()
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