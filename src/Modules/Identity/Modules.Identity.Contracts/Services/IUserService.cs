using FSH.Framework.Shared.Storage;
using FSH.Modules.Identity.Contracts.DTOs;
using System.Security.Claims;

namespace FSH.Modules.Identity.Contracts.Services;

public interface IUserService
{
    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken cancellationToken = default);
    Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken);
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken);
    Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken cancellationToken);
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password, string confirmPassword, string phoneNumber, string origin, CancellationToken cancellationToken);
    Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image, bool deleteCurrentImage, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken);
    Task AdminConfirmEmailAsync(string userId, CancellationToken cancellationToken = default);
    Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken cancellationToken = default);
    Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default);

    // permisions
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    // passwords
    Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken);
    Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken);
    Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId, CancellationToken cancellationToken = default);
    Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken);
    Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);
}