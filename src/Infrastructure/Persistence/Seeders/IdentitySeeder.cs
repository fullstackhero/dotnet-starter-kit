using DN.WebApi.Application.Abstractions.Database;
using DN.WebApi.Application.Settings;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Utilties;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DN.WebApi.Infrastructure.Persistence.Seeders
{
    public class IdentitySeeder : ISeeder
    {
        private readonly TenantSettings _tenantSettings;
        private readonly UserManager<ExtendedUser> _userManager;
        private readonly RoleManager<ExtendedRole> _roleManager;
        private readonly ILogger<IdentitySeeder> _logger;
        private readonly IStringLocalizer<IdentitySeeder> _localizer;

        public IdentitySeeder(IStringLocalizer<IdentitySeeder> localizer, ILogger<IdentitySeeder> logger, RoleManager<ExtendedRole> roleManager, UserManager<ExtendedUser> userManager, IOptions<TenantSettings> tenantSettings)
        {
            _localizer = localizer;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _tenantSettings = tenantSettings.Value;
        }

        public void Initialize()
        {
            //super tenant seeding
            // foreach (var tenant in _tenantSettings.Tenants)
            // {
            //     AddDefaultRoles(tenant);
            //     AddAdmin(tenant);
            // }

        }
        private void AddDefaultRoles(Tenant tenant)
        {
            // Task.Run(async () =>
            // {
            //     foreach (string roleName in typeof(RoleConstants).GetAllPublicConstantValues<string>())
            //     {
            //         var role = new ExtendedRole(roleName);
            //         var roleInDb = await _roleManager.FindByNameAsync(roleName);
            //         if (roleInDb == null)
            //         {
            //             await _roleManager.CreateAsync(role);
            //             _logger.LogInformation(string.Format(_localizer["Added '{0}' to Roles"], roleName));
            //         }
            //     }
            // }).GetAwaiter().GetResult();
        }
        private void AddAdmin(Tenant tenant)
        {
            // Task.Run(async () =>
            // {
            //     // Check if Role Exists
            //     var adminRole = new ExtendedRole(RoleConstants.Admin);
            //     var adminRoleInDb = await _roleManager.FindByNameAsync(RoleConstants.Admin);
            //     if (adminRoleInDb == null)
            //     {
            //         await _roleManager.CreateAsync(adminRole);
            //         adminRoleInDb = await _roleManager.FindByNameAsync(RoleConstants.Admin);
            //     }

            //     // Check if User Exists
            //     var superUser = new ExtendedUser
            //     {
            //         FirstName = tenant.Name,
            //         Email = tenant.AdminEmail,
            //         UserName = tenant.Name,
            //         EmailConfirmed = true,
            //         PhoneNumberConfirmed = true,
            //         IsActive = true
            //     };
            //     var superUserInDb = await _userManager.FindByEmailAsync(superUser.Email);
            //     if (superUserInDb == null)
            //     {
            //         await _userManager.CreateAsync(superUser, UserConstants.DefaultPassword);
            //         var result = await _userManager.AddToRoleAsync(superUser, RoleConstants.Admin);
            //         if (result.Succeeded)
            //         {
            //             _logger.LogInformation(_localizer["Added Admin User to {0} Tenant."], tenant.Name);
            //         }
            //         else
            //         {
            //             foreach (var error in result.Errors)
            //             {
            //                 _logger.LogError(error.Description);
            //             }
            //         }
            //     }
            // }).GetAwaiter().GetResult();
        }


    }
}