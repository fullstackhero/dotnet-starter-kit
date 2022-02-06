using System.Security.Claims;

namespace FSH.WebApi.Application.Identity.Users.Profile;

public interface IProfileService : ITransientService
{
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal);

    Task<string> CreateAsync(CreateProfileRequest request, string origin);

    Task UpdateAsync(UpdateProfileRequest request, string userId);

    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken);

    Task<string> ConfirmPhoneNumberAsync(string userId, string code);

    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<string> ResetPasswordAsync(ResetPasswordRequest request);

    Task ChangePasswordAsync(ChangePasswordRequest request, string userId);
}