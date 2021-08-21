using DN.WebApi.Application.Abstractions.Database;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Utilties;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Persistence.Seeders
{
    public class IdentitySeeder : ISeeder
    {
        private readonly UserManager<ExtendedUser> _userManager;
        private readonly RoleManager<ExtendedRole> _roleManager;
        private readonly ILogger<IdentitySeeder> _logger;
        private readonly IStringLocalizer<IdentitySeeder> _localizer;

        public IdentitySeeder(IStringLocalizer<IdentitySeeder> localizer, ILogger<IdentitySeeder> logger, RoleManager<ExtendedRole> roleManager, UserManager<ExtendedUser> userManager)
        {
            _localizer = localizer;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void Initialize()
        {
            AddDefaultRoles();
            AddSuperAdmin();
        }
        private void AddDefaultRoles()
        {
            Task.Run(async () =>
            {
                foreach (string roleName in typeof(RoleConstants).GetAllPublicConstantValues<string>())
                {
                    var role = new ExtendedRole(roleName);
                    var roleInDb = await _roleManager.FindByNameAsync(roleName);
                    if (roleInDb == null)
                    {
                        await _roleManager.CreateAsync(role);
                        _logger.LogInformation(string.Format(_localizer["Added '{0}' to Roles"], roleName));
                    }
                }
            }).GetAwaiter().GetResult();
        }
        private void AddSuperAdmin()
        {
            Task.Run(async () =>
            {
                // Check if Role Exists
                var superAdminRole = new ExtendedRole(RoleConstants.SuperAdmin);
                var superAdminRoleInDb = await _roleManager.FindByNameAsync(RoleConstants.SuperAdmin);
                if (superAdminRoleInDb == null)
                {
                    await _roleManager.CreateAsync(superAdminRole);
                    superAdminRoleInDb = await _roleManager.FindByNameAsync(RoleConstants.SuperAdmin);
                }

                // Check if User Exists
                var superUser = new ExtendedUser
                {
                    FirstName = "Mukesh",
                    LastName = "Murugan",
                    Email = "sa@demo.com",
                    UserName = "superadmin",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true
                };
                var superUserInDb = await _userManager.FindByEmailAsync(superUser.Email);
                if (superUserInDb == null)
                {
                    await _userManager.CreateAsync(superUser, UserConstants.DefaultPassword);
                    var result = await _userManager.AddToRoleAsync(superUser, RoleConstants.SuperAdmin);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(_localizer["Seeded SA User."]);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            _logger.LogError(error.Description);
                        }
                    }
                }
            }).GetAwaiter().GetResult();
        }


    }
}