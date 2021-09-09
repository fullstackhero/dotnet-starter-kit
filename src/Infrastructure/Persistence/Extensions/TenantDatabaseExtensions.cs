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
using Serilog;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class TenantDatabaseExtensions
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(TenantDatabaseExtensions));
        public static IServiceCollection PrepareTenantDatabases<T>(this IServiceCollection services, IConfiguration config)
        where T : ApplicationDbContext
        {
            services.Configure<TenantSettings>(config.GetSection(nameof(TenantSettings)));
            var options = services.GetOptions<TenantSettings>(nameof(TenantSettings));
            var defaultConnectionString = options.Defaults?.ConnectionString;
            var dbProvider = options.Defaults?.DBProvider;
            switch (dbProvider.ToLower())
            {
                case "postgresql":
                    services.AddHangfire(x => x.UsePostgreSqlStorage(defaultConnectionString));
                    break;
                case "mssql":
                    services.AddHangfire(x => x.UseSqlServerStorage(defaultConnectionString));
                    break;
            }

            var tenants = options.Tenants;
            foreach (var tenant in tenants)
            {
                string connectionString;
                if (string.IsNullOrEmpty(tenant.ConnectionString)) connectionString = defaultConnectionString;
                else connectionString = tenant.ConnectionString;

                switch (dbProvider.ToLower())
                {
                    case "postgresql":
                        services.AddDbContext<T>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                        services.MigrateAndSeedIdentityData<T>(connectionString, tenant.TID, options);
                        break;
                    case "mssql":
                        services.AddDbContext<T>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                        services.MigrateAndSeedIdentityData<T>(connectionString, tenant.TID, options);
                        break;
                }
            }

            return services;
        }

        private static IServiceCollection MigrateAndSeedIdentityData<T>(this IServiceCollection services, string connectionString, string tenantId, TenantSettings options)
        where T : ApplicationDbContext
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<T>();
            dbContext.Database.SetConnectionString(connectionString);
            var tenant = options.Tenants.Where(a => a.TID == tenantId).FirstOrDefault();
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
                _logger.Information($"{tenant.Name} : Migrations complete....");
            }

            if (dbContext.Database.CanConnect())
            {
                SeedRoles(tenant, dbContext);
                SeedTenantAdmins(tenantId, tenant, scope, dbContext);
            }

            return services;
        }
        #region Seeding
        private static void SeedTenantAdmins<T>(string tenantId, Tenant tenant, IServiceScope scope, T dbContext)
        where T : ApplicationDbContext
        {
            var adminUserName = $"{tenant.Name}.admin";
            var superUser = new ApplicationUser
            {
                FirstName = tenant.Name,
                LastName = "admin",
                Email = tenant.AdminEmail,
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = tenant.AdminEmail.ToUpper(),
                NormalizedUserName = adminUserName.ToUpper(),
                IsActive = true,
                TenantId = tenantId
            };
            if (!dbContext.Users.IgnoreQueryFilters().Any(u => u.Email == tenant.AdminEmail))
            {
                var password = new PasswordHasher<ApplicationUser>();
                var hashed = password.HashPassword(superUser, UserConstants.DefaultPassword);
                superUser.PasswordHash = hashed;
                var userStore = new UserStore<ApplicationUser>(dbContext);
                userStore.CreateAsync(superUser).Wait();
                _logger.Information($"{tenant.Name} : Seeding Admin User {tenant.AdminEmail}....");
                AssignRolesAsync(scope.ServiceProvider, superUser.Email, RoleConstants.Admin, dbContext, tenantId).Wait();
            }
        }

        private static void SeedRoles<T>(Tenant tenant, T dbContext)
        where T : ApplicationDbContext
        {
            foreach (string roleName in typeof(RoleConstants).GetAllPublicConstantValues<string>())
            {
                var roleStore = new RoleStore<ApplicationRole>(dbContext);
                if (!dbContext.Roles.IgnoreQueryFilters().Any(r => r.Name == roleName && r.TenantId == tenant.TID))
                {
                    var role = new ApplicationRole(roleName, tenant.TID, $"{roleName} Role for {tenant.Name} Tenant");
                    roleStore.CreateAsync(role).Wait();
                }
            }
        }

        public static async Task AssignRolesAsync<T>(IServiceProvider services, string email, string role, T dbContext, string tenantId)
        where T : ApplicationDbContext
        {
            UserManager<ApplicationUser> userManager = services.GetService<UserManager<ApplicationUser>>();
            var user = await userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.Equals(email));
            if (user == null) return;
            var roleRecord = dbContext.Roles.Where(a => a.NormalizedName == role.ToUpper() && a.TenantId == tenantId).FirstOrDefaultAsync().Result;
            if (roleRecord == null) return;
            var isUserInRole = dbContext.UserRoles.Any(a => a.UserId == user.Id && a.RoleId == roleRecord.Id);
            if (!isUserInRole)
            {
                dbContext.UserRoles.Add(new IdentityUserRole<string>() { RoleId = roleRecord.Id, UserId = user.Id });
                dbContext.SaveChangesAsync().Wait();
            }
        }
        #endregion
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