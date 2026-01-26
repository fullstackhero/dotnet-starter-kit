using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Data;

internal sealed class IdentityDbInitializer(
    ILogger<IdentityDbInitializer> logger,
    IdentityDbContext context,
    RoleManager<FshRole> roleManager,
    UserManager<FshUser> userManager,
    TimeProvider timeProvider,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IOptions<OriginOptions> originSettings) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for identity module", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedSystemGroupsAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        foreach (string roleName in RoleConstants.DefaultRoles)
        {
            if (await roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName, cancellationToken)
                is not FshRole role)
            {
                // create role
                role = new FshRole(roleName, $"{roleName} Role for {multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id} Tenant");
                await roleManager.CreateAsync(role);
            }

            // Assign permissions
            if (roleName == RoleConstants.Basic)
            {
                await AssignPermissionsToRoleAsync(context, PermissionConstants.Basic, role, cancellationToken);
            }
            else if (roleName == RoleConstants.Admin)
            {
                await AssignPermissionsToRoleAsync(context, PermissionConstants.Admin, role, cancellationToken);

                if (multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id == MultitenancyConstants.Root.Id)
                {
                    await AssignPermissionsToRoleAsync(context, PermissionConstants.Root, role, cancellationToken);
                }
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(IdentityDbContext dbContext, IReadOnlyList<FshPermission> permissions, FshRole role, CancellationToken cancellationToken = default)
    {
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var newClaims = permissions
            .Where(permission => !currentClaims.Any(c => c.Type == ClaimConstants.Permission && c.Value == permission.Name))
            .Select(permission => new FshRoleClaim
            {
                RoleId = role.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = permission.Name,
                CreatedBy = "application",
                CreatedOn = timeProvider.GetUtcNow()
            })
            .ToList();

        foreach (var claim in newClaims)
        {
            logger.LogInformation("Seeding {Role} Permission '{Permission}' for '{TenantId}' Tenant.", role.Name, claim.ClaimValue, multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id);
            await dbContext.RoleClaims.AddAsync(claim, cancellationToken);
        }

        // Save changes to the database context
        if (newClaims.Count != 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

    }

    private async Task SeedSystemGroupsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        // Seed "All Users" default group - all new users are automatically added to this group
        const string allUsersGroupName = "All Users";
        var allUsersGroup = await context.Groups
            .FirstOrDefaultAsync(g => g.Name == allUsersGroupName && g.IsSystemGroup, cancellationToken);

        if (allUsersGroup is null)
        {
            allUsersGroup = Group.Create(
                name: allUsersGroupName,
                description: "Default group for all users. New users are automatically added to this group.",
                isDefault: true,
                isSystemGroup: true,
                createdBy: "System");

            await context.Groups.AddAsync(allUsersGroup, cancellationToken);
            logger.LogInformation("Seeding '{GroupName}' system group for '{TenantId}' Tenant.", allUsersGroupName, tenantId);
        }

        // Seed "Administrators" group with Admin role
        const string administratorsGroupName = "Administrators";
        var administratorsGroup = await context.Groups
            .FirstOrDefaultAsync(g => g.Name == administratorsGroupName && g.IsSystemGroup, cancellationToken);

        if (administratorsGroup is null)
        {
            administratorsGroup = Group.Create(
                name: administratorsGroupName,
                description: "System group for administrators with full administrative privileges.",
                isDefault: false,
                isSystemGroup: true,
                createdBy: "System");

            await context.Groups.AddAsync(administratorsGroup, cancellationToken);
            logger.LogInformation("Seeding '{GroupName}' system group for '{TenantId}' Tenant.", administratorsGroupName, tenantId);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Assign Admin role to Administrators group
        var adminRole = await roleManager.FindByNameAsync(RoleConstants.Admin);
        if (adminRole is not null)
        {
            var existingGroupRole = await context.GroupRoles
                .FirstOrDefaultAsync(gr => gr.GroupId == administratorsGroup.Id && gr.RoleId == adminRole.Id, cancellationToken);

            if (existingGroupRole is null)
            {
                context.GroupRoles.Add(GroupRole.Create(administratorsGroup.Id, adminRole.Id));

                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Assigned Admin role to '{GroupName}' group for '{TenantId}' Tenant.", administratorsGroupName, tenantId);
            }
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id) || string.IsNullOrWhiteSpace(multiTenantContextAccessor.MultiTenantContext.TenantInfo?.AdminEmail))
        {
            return;
        }

        if (await userManager.Users.FirstOrDefaultAsync(u => u.Email == multiTenantContextAccessor.MultiTenantContext.TenantInfo!.AdminEmail, cancellationToken)
            is not FshUser adminUser)
        {
            string adminUserName = $"{multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id.Trim()}.{RoleConstants.Admin}".ToUpperInvariant();
            adminUser = new FshUser
            {
                FirstName = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id.Trim().ToUpperInvariant(),
                LastName = RoleConstants.Admin,
                Email = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.AdminEmail,
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.AdminEmail!.ToUpperInvariant(),
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                ImageUrl = new Uri(originSettings.Value.OriginUrl! + MultitenancyConstants.Root.DefaultProfilePicture),
                IsActive = true
            };

            logger.LogInformation("Seeding Default Admin User for '{TenantId}' Tenant.", multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id);
            var password = new PasswordHasher<FshUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, MultitenancyConstants.DefaultPassword);
            await userManager.CreateAsync(adminUser);
        }

        // Assign role to user
        if (!await userManager.IsInRoleAsync(adminUser, RoleConstants.Admin))
        {
            logger.LogInformation("Assigning Admin Role to Admin User for '{TenantId}' Tenant.", multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id);
            await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
        }
    }
}
