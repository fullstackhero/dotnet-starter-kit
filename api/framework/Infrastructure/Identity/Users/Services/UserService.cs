using System.Security.Claims;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Identity.Users.Dtos;
using FSH.Framework.Core.Identity.Users.Features.RegisterUser;
using FSH.Framework.Core.Identity.Users.Features.ToggleUserStatus;
using FSH.Framework.Core.Identity.Users.Features.UpdateUser;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Infrastructure.Identity.Users.Services;
internal class UserService(
    UserManager<FshUser> userManager,
    SignInManager<FshUser> signInManager
    ) : IUserService
{
    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithNameAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        throw new NotImplementedException();
    }

    public Task<UserDetail> GetAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

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

        // TODO: send confirmation mail

        return new RegisterUserResponse(user.Id);
    }

    public Task ToggleStatusAsync(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(UpdateUserCommand request, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

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
}
