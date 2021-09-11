using System.Security.Claims;
using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Utilties;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.MySql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace DN.WebApi.Infrastructure.Persistence.Extensions
{
    public static class MultitenancyExtensions
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(MultitenancyExtensions));

        public static IServiceCollection AddMultitenancy<T, TA>(this IServiceCollection services, IConfiguration config)
        where T : TenantManagementDbContext
        where TA : ApplicationDbContext
        {
            services.Configure<MultitenancySettings>(config.GetSection(nameof(MultitenancySettings)));
            var multitenancySettings = services.GetOptions<MultitenancySettings>(nameof(MultitenancySettings));
            var rootConnectionString = multitenancySettings.ConnectionString;
            var dbProvider = multitenancySettings.DBProvider;
            switch (dbProvider.ToLower())
            {
                case "postgresql":
                    services.AddDbContext<T>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                    services.AddHangfire(x => x.UsePostgreSqlStorage(rootConnectionString));
                    break;
                case "mssql":
                    services.AddDbContext<T>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                    services.AddHangfire(x => x.UseSqlServerStorage(rootConnectionString));
                    break;
                case "mysql":
                    services.AddDbContext<T>(m => m.UseMySql(rootConnectionString, ServerVersion.AutoDetect(rootConnectionString), e =>
                    {
                        e.MigrationsAssembly("Migrators.MySQL");
                        e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                    }));
                    services.AddHangfire(x => x.UseStorage(new MySqlStorage(rootConnectionString, new MySqlStorageOptions())));
                    break;
            }

            services.SetupDatabases<T, TA>(multitenancySettings);
            return services;
        }

        private static IServiceCollection SetupDatabases<T, TA>(this IServiceCollection services, MultitenancySettings options)
        where T : TenantManagementDbContext
        where TA : ApplicationDbContext
        {
            var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<T>();
            dbContext.Database.SetConnectionString(options.ConnectionString);
            switch (options.DBProvider.ToLower())
            {
                case "postgresql":
                    services.AddDbContext<TA>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                    break;
                case "mssql":
                    services.AddDbContext<TA>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                    break;
                case "mysql":
                    services.AddDbContext<TA>(m => m.UseMySql(options.ConnectionString, ServerVersion.AutoDetect(options.ConnectionString), e =>
                    {
                        e.MigrationsAssembly("Migrators.MySQL");
                        e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                    }));
                    break;
            }

            if (dbContext.Database.GetMigrations().Count() > 0)
            {
                if (dbContext.Database.GetPendingMigrations().Any())
                {
                    dbContext.Database.Migrate();
                    _logger.Information($"Applying Root Migrations.");
                }

                if (dbContext.Database.CanConnect())
                {
                    var availableTenants = dbContext.Tenants.ToListAsync().Result;
                    foreach (var tenant in availableTenants)
                    {
                        services.SetupTenantDatabase<TA>(options, tenant);
                    }

                    SeedRootTenant(dbContext, options);
                }
            }

            return services;
        }

        private static IServiceCollection SetupTenantDatabase<TA>(this IServiceCollection services, MultitenancySettings options, Domain.Entities.Multitenancy.Tenant tenant)
        where TA : ApplicationDbContext
        {

            var tenantConnectionString = tenant.ConnectionString ?? options.ConnectionString;
            switch (options.DBProvider.ToLower())
            {
                case "postgresql":
                    services.AddDbContext<TA>(m => m.UseNpgsql(e => e.MigrationsAssembly("Migrators.PostgreSQL")));
                    break;
                case "mssql":
                    services.AddDbContext<TA>(m => m.UseSqlServer(e => e.MigrationsAssembly("Migrators.MSSQL")));
                    break;
                case "mysql":
                    services.AddDbContext<TA>(m => m.UseMySql(tenantConnectionString, ServerVersion.AutoDetect(tenantConnectionString), e =>
                    {
                        e.MigrationsAssembly("Migrators.MySQL");
                        e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                    }));
                    break;
            }

            var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TA>();
            dbContext.Database.SetConnectionString(tenantConnectionString);
            if (dbContext.Database.GetMigrations().Count() > 0)
            {
                if (dbContext.Database.GetPendingMigrations().Any())
                {
                    dbContext.Database.Migrate();
                    _logger.Information($"{tenant.Name} : Migrations complete....");
                }

                if (dbContext.Database.CanConnect())
                {
                    SeedRoles(tenant, dbContext);
                    SeedAdmin(tenant, scope, dbContext);
                }
            }

            return services;
        }

        private static void SeedRootTenant<T>(T dbContext, MultitenancySettings options)
        where T : TenantManagementDbContext
        {
            if (!dbContext.Tenants.Any(t => t.Key == "root"))
            {
                var rootTenant = new DN.WebApi.Domain.Entities.Multitenancy.Tenant("Root", "root", "admin@root.com", options.ConnectionString);
                dbContext.Tenants.Add(rootTenant);
                dbContext.SaveChangesAsync().Wait();
            }
        }
        #region Seeding
        private static void SeedAdmin<T>(DN.WebApi.Domain.Entities.Multitenancy.Tenant tenant, IServiceScope scope, T dbContext)
        where T : ApplicationDbContext
        {
            var adminUserName = $"{tenant.Name.ToLower()}.admin";
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
                TenantKey = tenant.Key
            };
            if (!dbContext.Users.IgnoreQueryFilters().Any(u => u.Email == tenant.AdminEmail))
            {
                var password = new PasswordHasher<ApplicationUser>();
                var hashed = password.HashPassword(superUser, UserConstants.DefaultPassword);
                superUser.PasswordHash = hashed;
                var userStore = new UserStore<ApplicationUser>(dbContext);
                userStore.CreateAsync(superUser).Wait();
                _logger.Information($"{tenant.Name} : Seeding Admin User {tenant.AdminEmail}....");
                AssignAdminRoleAsync(scope.ServiceProvider, superUser.Email, dbContext, tenant.Key).Wait();
            }
        }

        private static void SeedRoles<T>(DN.WebApi.Domain.Entities.Multitenancy.Tenant tenant, T dbContext)
        where T : ApplicationDbContext
        {
            foreach (string roleName in typeof(RoleConstants).GetAllPublicConstantValues<string>())
            {
                var roleStore = new RoleStore<ApplicationRole>(dbContext);
                if (!dbContext.Roles.IgnoreQueryFilters().Any(r => r.Name == roleName && r.TenantKey == tenant.Key))
                {
                    var role = new ApplicationRole(roleName, tenant.Key, $"{roleName} Role for {tenant.Name} Tenant");
                    roleStore.CreateAsync(role).Wait();
                }
            }
        }

        public static async Task AssignAdminRoleAsync<T>(IServiceProvider services, string email, T dbContext, string tenantKey)
        where T : ApplicationDbContext
        {
            var adminRole = RoleConstants.Admin;
            UserManager<ApplicationUser> userManager = services.GetService<UserManager<ApplicationUser>>();
            RoleManager<ApplicationRole> roleManager = services.GetService<RoleManager<ApplicationRole>>();
            var user = await userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.Equals(email));
            if (user == null) return;
            var roleRecord = roleManager.Roles.IgnoreQueryFilters().Where(a => a.NormalizedName == adminRole.ToUpper() && a.TenantKey == tenantKey).FirstOrDefaultAsync().Result;
            if (roleRecord == null) return;
            var isUserInRole = dbContext.UserRoles.Any(a => a.UserId == user.Id && a.RoleId == roleRecord.Id);
            if (!isUserInRole)
            {
                dbContext.UserRoles.Add(new IdentityUserRole<string>() { RoleId = roleRecord.Id, UserId = user.Id });
                foreach (string permission in typeof(Permissions).GetNestedClassesStaticStringValues())
                {
                    var allClaims = await roleManager.GetClaimsAsync(roleRecord);
                    if (!allClaims.Any(a => a.Type == Domain.Constants.ClaimConstants.Permission && a.Value == permission))
                    {
                        await roleManager.AddClaimAsync(roleRecord, new Claim(Domain.Constants.ClaimConstants.Permission, permission));
                    }
                }
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