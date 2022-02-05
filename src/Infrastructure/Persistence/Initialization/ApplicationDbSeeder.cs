using System.ComponentModel;
using System.Reflection;
using FSH.WebApi.Infrastructure.Auth.Permissions;
using FSH.WebApi.Infrastructure.Identity;
using FSH.WebApi.Infrastructure.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence.Context;
using FSH.WebApi.Shared.Authorization;
using FSH.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.Persistence.Initialization;

internal class ApplicationDbSeeder
{
    private readonly FSHTenantInfo _currentTenant;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CustomSeederRunner _seederRunner;
    private readonly ILogger<ApplicationDbSeeder> _logger;

    public ApplicationDbSeeder(FSHTenantInfo currentTenant, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, CustomSeederRunner seederRunner, ILogger<ApplicationDbSeeder> logger)
    {
        _currentTenant = currentTenant;
        _roleManager = roleManager;
        _userManager = userManager;
        _seederRunner = seederRunner;
        _logger = logger;
    }

    public async Task SeedDatabaseAsync(ApplicationDbContext _dbContext, CancellationToken cancellationToken)
    {
        await SeedRolesAsync(_dbContext);
        await SeedAdminUserAsync();
        await _seederRunner.RunSeedersAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(ApplicationDbContext _dbContext)
    {
        foreach (string roleName in RoleService.DefaultRoles)
        {
            if (await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName)
                is not ApplicationRole role)
            {
                // Create the role
                role = new ApplicationRole(roleName, $"{roleName} Role for {_currentTenant.Id} Tenant");
                _logger.LogInformation("Seeding {role} Role for '{tenantId}' Tenant.", roleName, _currentTenant.Id);
                await _roleManager.CreateAsync(role);
            }

            // Assign permissions
            if (roleName == FSHRoles.Basic)
            {
                var basicPermissions = DefaultPermissions.BasicPermissionTypes;
                await AssignPermissionsToRoleAsync(_dbContext, role, basicPermissions);
            }
            else if (roleName == FSHRoles.Admin)
            {
                var adminPermissions = DefaultPermissions.AdminPermissionTypes;
                await AssignPermissionsToRoleAsync(_dbContext, role, adminPermissions);

                if (_currentTenant.Id == MultitenancyConstants.Root.Id)
                {
                    var rootPermissions = DefaultPermissions.RootPermissionTypes;
                    await AssignPermissionsToRoleAsync(_dbContext, role, rootPermissions);
                }
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(ApplicationDbContext _dbContext, ApplicationRole role, Type[] modules)
    {
        var currentClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var module in modules)
        {
            string? moduleName = string.Empty;
            string? moduleDescription = string.Empty;

            if (module.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                .FirstOrDefault() is DisplayNameAttribute displayNameAttribute)
            {
                moduleName = displayNameAttribute.DisplayName;
            }

            if (module.GetCustomAttributes(typeof(DescriptionAttribute), true)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
            {
                moduleDescription = descriptionAttribute.Description;
            }

            var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var fi in fields)
            {
                object? propertyValue = fi.GetValue(null);

                if (propertyValue?.ToString() != null && !currentClaims.Any(a => a.Type == FSHClaims.Permission && a.Value == propertyValue.ToString()))
                {
                    _logger.LogInformation("Seeding {role} Permission '{permission}' for '{tenantId}' Tenant.", role.Name, propertyValue.ToString(), _currentTenant.Id);
                    _dbContext.RoleClaims.Add(new ApplicationRoleClaim()
                    {
                        RoleId = role.Id,
                        ClaimType = FSHClaims.Permission,
                        ClaimValue = propertyValue.ToString(),
                        Description = moduleDescription,
                        Group = moduleName,
                        CreatedOn = DateTime.UtcNow,
                        LastModifiedOn = DateTime.UtcNow
                    });
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentTenant.Id) || string.IsNullOrWhiteSpace(_currentTenant.AdminEmail))
        {
            return;
        }

        if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == _currentTenant.AdminEmail)
            is not ApplicationUser adminUser)
        {
            string adminUserName = $"{_currentTenant.Id.Trim()}.{FSHRoles.Admin}".ToLowerInvariant();
            adminUser = new ApplicationUser
            {
                FirstName = _currentTenant.Id.Trim().ToLowerInvariant(),
                LastName = FSHRoles.Admin,
                Email = _currentTenant.AdminEmail,
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = _currentTenant.AdminEmail?.ToUpperInvariant(),
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                IsActive = true
            };

            _logger.LogInformation("Seeding Default Admin User for '{tenantId}' Tenant.", _currentTenant.Id);
            var password = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, MultitenancyConstants.DefaultPassword);
            await _userManager.CreateAsync(adminUser);
        }

        // Assign role to user
        if (!await _userManager.IsInRoleAsync(adminUser, FSHRoles.Admin))
        {
            _logger.LogInformation("Assigning Admin Role to Admin User for '{tenantId}' Tenant.", _currentTenant.Id);
            await _userManager.AddToRoleAsync(adminUser, FSHRoles.Admin);
        }
    }
}