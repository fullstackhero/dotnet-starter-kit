using System.Security.Claims;

namespace FSH.WebApi.Application.Identity.Users;

public interface IIdentityService : ITransientService
{
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal);

    Task<string> RegisterAsync(RegisterUserRequest request, string origin);

    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken);

    Task<string> ConfirmPhoneNumberAsync(string userId, string code);

    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<string> ResetPasswordAsync(ResetPasswordRequest request);

    Task UpdateProfileAsync(UpdateProfileRequest request, string userId);

    Task ChangePasswordAsync(ChangePasswordRequest request, string userId);
}