namespace FSH.Modules.Identity.Constants;

/// <summary>
/// Validation message string constants for the Identity module.
/// These keys are used directly as messages now, and will become .resx lookup keys
/// when localization is added.
/// </summary>
internal static class IdentityValidationMessages
{
    // ── Required ─────────────────────────────────────────────────────────────
    public const string GroupIdRequired = "Group ID is required.";
    public const string GroupNameRequired = "Group name is required.";
    public const string UserIdRequired = "User ID is required.";
    public const string UserIdsRequired = "At least one user ID is required.";
    public const string UserIdsInvalid = "User IDs cannot be empty or whitespace.";
    public const string RoleIdRequired = "Role ID is required.";
    public const string RoleNameRequired = "Role name is required.";
    public const string SessionIdRequired = "Session ID is required.";
    public const string EmailRequired = "Email is required.";
    public const string FirstNameRequired = "First name is required.";
    public const string LastNameRequired = "Last name is required.";
    public const string PasswordRequired = "Password is required.";
    public const string PasswordConfirmationRequired = "Password confirmation is required.";
    public const string UsernameRequired = "Username is required.";
    public const string ConfirmationCodeRequired = "Confirmation code is required.";
    public const string TenantRequired = "Tenant is required.";
    public const string CurrentPasswordRequired = "Current password is required.";
    public const string NewPasswordRequired = "New password is required.";
    public const string UserRolesRequired = "User roles list is required.";

    // ── Length ────────────────────────────────────────────────────────────────
    public const string GroupNameMaxLength = "Group name must not exceed 256 characters.";
    public const string DescriptionMaxLength = "Description must not exceed 1024 characters.";
    public const string ReasonMaxLength = "Reason must not exceed 500 characters.";
    public const string PhoneNumberMaxLength = "Phone number must not exceed 20 characters.";
    public const string UsernameMinLength = "Username must be at least 3 characters.";
    public const string UsernameMaxLength = "Username must not exceed 50 characters.";
    public const string PasswordMinLength = "Password must be at least 6 characters.";
    public const string FirstNameMaxLength = "First name must not exceed 100 characters.";
    public const string LastNameMaxLength = "Last name must not exceed 100 characters.";

    // ── Format & rules ────────────────────────────────────────────────────────
    public const string InvalidEmail = "A valid email address is required.";
    public const string PasswordsMustMatch = "Passwords do not match.";
    public const string NewPasswordMustDiffer = "New password must be different from the current password.";
    public const string PasswordInHistory = "This password has been used recently. Please choose a different password.";
    public const string ImageConflict = "You cannot upload a new image and delete the current one simultaneously.";
}
