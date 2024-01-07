using System.Security.Claims;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Identity.Users.Dtos;
using FSH.Framework.Core.Identity.Users.Features.RegisterUser;
using FSH.Framework.Core.Identity.Users.Features.ToggleUserStatus;
using FSH.Framework.Core.Identity.Users.Features.UpdateUser;
using Microsoft.AspNetCore.Identity;

namespace FSH.Framework.Infrastructure.Identity.Users.Services;
internal class UserService(
    UserManager<FshUser> userManager
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

    public Task<List<UserDetail>> GetListAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterUserResponse> RegisterAsync(RegisterUserCommand request, CancellationToken cancellationToken)
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
            throw new FshException("something went wrong.");
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

    public Task UpdateAsync(UpdateUserCommand request, string userId)
    {
        throw new NotImplementedException();
    }
}
