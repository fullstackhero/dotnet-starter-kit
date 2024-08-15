using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Identity.Users.Dtos;
using FSH.Framework.Core.Identity.Users.Features.AssignUserRole;
using FSH.Framework.Core.Identity.Users.Features.RegisterUser;
using FSH.Framework.Core.Identity.Users.Features.ToggleUserStatus;
using FSH.Framework.Core.Identity.Users.Features.UpdateUser;
using FSH.Framework.Core.Jobs;
using FSH.Framework.Core.Mail;
using FSH.Framework.Core.Storage;
using FSH.Framework.Core.Storage.File;
using FSH.Framework.Core.Tenant;
using FSH.Framework.Infrastructure.Constants;
using FSH.Framework.Infrastructure.Identity.Persistence;
using FSH.Framework.Infrastructure.Identity.Roles;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Identity.Users.Services;

internal sealed partial class UserService(
    UserManager<FshUser> userManager,
    SignInManager<FshUser> signInManager,
    RoleManager<FshRole> roleManager,
    IdentityDbContext db,
    ICacheService cache,
    IJobService jobService,
    IMailService mailService,
    IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor,
    IStorageService storageService
    ) : IUserService
{
    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        EnsureValidTenant();
        return await userManager.FindByEmailAsync(email.Normalize()) is FshUser user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        EnsureValidTenant();
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        EnsureValidTenant();
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is FshUser user && user.Id != exceptId;
    }

    public async Task<UserDetail> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        return user.Adapt<UserDetail>();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<List<UserDetail>> GetListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        return users.Adapt<List<UserDetail>>();
    }

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserCommand request, string origin, CancellationToken cancellationToken)
    {
        // create user entity
        var user = new FshUser
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            EmailConfirmed = true
        };

        // register user
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new FshException("error while registering a new user", errors);
        }

        // add basic role
        await userManager.AddToRoleAsync(user, IdentityConstants.Roles.Basic);

        // send confirmation mail
        if (!string.IsNullOrEmpty(user.Email))
        {
            string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
            var mailRequest = new MailRequest(
                new Collection<string> { user.Email },
                "Confirm Registration",
                emailVerificationUri);
            jobService.Enqueue("email", () => mailService.SendAsync(mailRequest, CancellationToken.None));
        }

        return new RegisterUserResponse(user.Id);
    }

    public async Task ToggleStatusAsync(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.Where(u => u.Id == request.UserId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("User Not Found.");

        bool isAdmin = await userManager.IsInRoleAsync(user, IdentityConstants.Roles.Admin);
        if (isAdmin)
        {
            throw new FshException("Administrators Profile's Status cannot be toggled");
        }

        user.IsActive = request.ActivateUser;

        await userManager.UpdateAsync(user);
    }

    public async Task UpdateAsync(UpdateUserCommand request, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        Uri imageUri = user.ImageUrl ?? null!;
        if (request.Image != null || request.DeleteCurrentImage)
        {
            user.ImageUrl = await storageService.UploadAsync<FshUser>(request.Image, FileType.Image);
            if (request.DeleteCurrentImage && imageUri != null)
            {
                storageService.Remove(imageUri);
            }
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        string? phoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (request.PhoneNumber != phoneNumber)
        {
            await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        }

        var result = await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        if (!result.Succeeded)
        {
            throw new FshException("Update profile failed");
        }
    }

    public async Task DeleteAsync(string userId)
    {
        FshUser? user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        user.IsActive = false;
        IdentityResult? result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            List<string> errors = result.Errors.Select(error => error.Description).ToList();
            throw new FshException("Delete profile failed", errors);
        }
    }

    private async Task<string> GetEmailVerificationUriAsync(FshUser user, string origin)
    {
        EnsureValidTenant();

        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        verificationUri = QueryHelpers.AddQueryString(verificationUri,
            TenantConstants.Identifier,
            multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id!);
        return verificationUri;
    }

    public async Task<string> AssignRolesAsync(string userId, AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        // Check if the user is an admin for which the admin role is getting disabled
        if (await userManager.IsInRoleAsync(user, FshRoles.Admin)
            && request.UserRoles.Exists(a => !a.Enabled && a.RoleName == FshRoles.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = (await userManager.GetUsersInRoleAsync(FshRoles.Admin)).Count;

            // Check if user is not Root Tenant Admin
            // Edge Case : there are chances for other tenants to have users with the same email as that of Root Tenant Admin. Probably can add a check while User Registration
            if (user.Email == TenantConstants.Root.EmailAddress)
            {
                if (multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id == TenantConstants.Root.Id)
                {
                    throw new FshException("action not permitted");
                }
            }
            else if (adminCount <= 2)
            {
                throw new FshException("tenant should have at least 2 admins.");
            }
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await roleManager.FindByNameAsync(userRole.RoleName!) is not null)
            {
                if (userRole.Enabled)
                {
                    if (!await userManager.IsInRoleAsync(user, userRole.RoleName!))
                    {
                        await userManager.AddToRoleAsync(user, userRole.RoleName!);
                    }
                }
                else
                {
                    await userManager.RemoveFromRoleAsync(user, userRole.RoleName!);
                }
            }
        }

        return "User Roles Updated Successfully.";

    }

    public async Task<List<UserRoleDetail>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDetail>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new NotFoundException("user not found");
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new NotFoundException("roles not found");
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDetail
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await userManager.IsInRoleAsync(user, role.Name!)
            });
        }

        return userRoles;
    }
}
