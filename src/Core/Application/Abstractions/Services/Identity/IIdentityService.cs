using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity.Requests;

namespace DN.WebApi.Application.Abstractions.Services.Identity
{
    public interface IIdentityService : ITransientService
    {
        Task<IResult> RegisterAsync(RegisterRequest request, string origin);

        Task<IResult<string>> ConfirmEmailAsync(string userId, string code);

        Task<IResult<string>> ConfirmPhoneNumberAsync(string userId, string code);

        Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

        Task<IResult> ResetPasswordAsync(ResetPasswordRequest request);
    }
}