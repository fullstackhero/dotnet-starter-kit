using FSH.Framework.Shared.Storage;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using System.Security.Claims;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Facade service that delegates to focused single-responsibility services.
/// Maintained for backward compatibility with existing consumers.
/// </summary>
internal sealed class UserService(
    IUserRegistrationService registrationService,
    IUserProfileService profileService,
    IUserStatusService statusService,
    IUserRoleService roleService,
    IUserPasswordService passwordService,
    IUserPermissionService permissionService) : IUserService
{
    // Registration operations (delegated to IUserRegistrationService)
    public Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string confirmPassword,
        string phoneNumber,
        string origin,
        CancellationToken cancellationToken)
        => registrationService.RegisterAsync(firstName, lastName, email, userName, password, confirmPassword, phoneNumber, origin, cancellationToken);

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
        => registrationService.GetOrCreateFromPrincipalAsync(principal);

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken)
        => registrationService.ConfirmEmailAsync(userId, code, tenant, cancellationToken);

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
        => registrationService.ConfirmPhoneNumberAsync(userId, code);

    // Profile operations (delegated to IUserProfileService)
    public Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken)
        => profileService.GetAsync(userId, cancellationToken);

    public Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken)
        => profileService.GetListAsync(cancellationToken);

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
        => profileService.GetCountAsync(cancellationToken);

    public Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image, bool deleteCurrentImage)
        => profileService.UpdateAsync(userId, firstName, lastName, phoneNumber, image, deleteCurrentImage);

    public Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
        => profileService.ExistsWithEmailAsync(email, exceptId);

    public Task<bool> ExistsWithNameAsync(string name)
        => profileService.ExistsWithNameAsync(name);

    public Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
        => profileService.ExistsWithPhoneNumberAsync(phoneNumber, exceptId);

    // Status operations (delegated to IUserStatusService)
    public Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken cancellationToken)
        => statusService.ToggleStatusAsync(activateUser, userId, cancellationToken);

    public Task DeleteAsync(string userId)
        => statusService.DeleteAsync(userId);

    // Role operations (delegated to IUserRoleService)
    public Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
        => roleService.AssignRolesAsync(userId, userRoles, cancellationToken);

    public Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
        => roleService.GetUserRolesAsync(userId, cancellationToken);

    // Password operations (delegated to IUserPasswordService)
    public Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
        => passwordService.ForgotPasswordAsync(email, origin, cancellationToken);

    public Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken)
        => passwordService.ResetPasswordAsync(email, password, token, cancellationToken);

    public Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId)
        => passwordService.ChangePasswordAsync(password, newPassword, confirmNewPassword, userId);

    // Permission operations (delegated to IUserPermissionService)
    public Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
        => permissionService.GetPermissionsAsync(userId, cancellationToken);

    public Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
        => permissionService.HasPermissionAsync(userId, permission, cancellationToken);
}
