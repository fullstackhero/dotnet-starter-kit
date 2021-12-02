using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;

namespace DN.WebApi.Application.Identity.Interfaces;

public interface IIdentityService : ITransientService
{
    Task<Result<string>> RegisterAsync(RegisterRequest request, string origin);

    Task<Result<string>> ConfirmEmailAsync(string userId, string code, string tenant);

    Task<Result<string>> ConfirmPhoneNumberAsync(string userId, string code);

    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<Result<string>> ResetPasswordAsync(ResetPasswordRequest request);

    Task<Result<string>> UpdateProfileAsync(UpdateProfileRequest request, string userId);
}