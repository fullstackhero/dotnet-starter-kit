using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;

namespace DN.WebApi.Application.Identity.Interfaces;

public interface IIdentityService : ITransientService
{
    Task<IResult<string>> RegisterAsync(RegisterRequest request, string origin);

    Task<IResult<string>> ConfirmEmailAsync(string userId, string code, string tenant);

    Task<IResult<string>> ConfirmPhoneNumberAsync(string userId, string code);

    Task<IResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<IResult<string>> ResetPasswordAsync(ResetPasswordRequest request);

    Task<IResult<string>> UpdateProfileAsync(UpdateProfileRequest request, string userId);
}