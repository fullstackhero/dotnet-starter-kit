using FSH.Framework.Core.Abstraction.Persistence;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Identity.Users;
using FSH.Framework.Infrastructure.Tenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Identity.Persistence;
internal sealed class IdentityDbInitializer(
    ILogger<IdentityDbInitializer> logger,
    IdentityDbContext context,
    RoleManager<FshRole> roleManager,
    UserManager<FshUser> userManager,
    FshTenantInfo currentTenant) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for identity module", context.TenantInfo.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (string roleName in IdentityConstants.Roles.DefaultRoles)
        {
            if (await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName)
                is not FshRole role)
            {
                // create role
                role = new FshRole(roleName, $"{roleName} Role for {currentTenant.Id} Tenant");
                await roleManager.CreateAsync(role);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (string.IsNullOrWhiteSpace(currentTenant.Id) || string.IsNullOrWhiteSpace(currentTenant.AdminEmail))
        {
            return;
        }

        if (await userManager.Users.FirstOrDefaultAsync(u => u.Email == currentTenant.AdminEmail)
            is not FshUser adminUser)
        {
            string adminUserName = $"{currentTenant.Id.Trim()}.{IdentityConstants.Roles.Admin}".ToLowerInvariant();
            adminUser = new FshUser
            {
                FirstName = currentTenant.Id.Trim().ToLowerInvariant(),
                LastName = IdentityConstants.Roles.Admin,
                Email = currentTenant.AdminEmail,
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = currentTenant.AdminEmail?.ToUpperInvariant(),
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                IsActive = true
            };

            logger.LogInformation("Seeding Default Admin User for '{tenantId}' Tenant.", currentTenant.Id);
            var password = new PasswordHasher<FshUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, IdentityConstants.DefaultPassword);
            await userManager.CreateAsync(adminUser);
        }

        // Assign role to user
        if (!await userManager.IsInRoleAsync(adminUser, IdentityConstants.Roles.Admin))
        {
            logger.LogInformation("Assigning Admin Role to Admin User for '{tenantId}' Tenant.", currentTenant.Id);
            await userManager.AddToRoleAsync(adminUser, IdentityConstants.Roles.Admin);
        }
    }
}
