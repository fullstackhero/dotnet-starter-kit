namespace FSH.Modules.Identity.Constants;

using FSH.Framework.Shared.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

/// <summary>
/// Validation message helper for the Identity module.
/// Uses SharedResource.resx for localized validation messages.
/// Methods accept an optional IStringLocalizer for runtime localization, falling back to English defaults.
/// </summary>
internal static class IdentityValidationMessages
{
    // ── Required — method uses localized format or falls back to default ─────────
    public static string Required(string fieldName, IStringLocalizer<SharedResource>? localizer = null)
    {
        var format = localizer?[nameof(Required)] ?? "{0} is required.";
        return string.Format(CultureInfo.InvariantCulture, format, fieldName);
    }

    // ── Required — non-standard messages that don't fit the field-name pattern ─
    public static string UserIdsRequired(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(UserIdsRequired)] ?? "At least one user ID is required.";

    public static string UserIdsInvalid(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(UserIdsInvalid)] ?? "User IDs cannot be empty or whitespace.";

    // ── Length — methods use localized format '{0} must not exceed {1} characters.' when localized ──
    public static string MaxLength(string fieldName, int max, IStringLocalizer<SharedResource>? localizer = null)
    {
        var format = localizer?[nameof(MaxLength)] ?? "{0} must not exceed {1} characters.";
        return string.Format(CultureInfo.InvariantCulture, format, fieldName, max);
    }

    public static string MinLength(string fieldName, int min, IStringLocalizer<SharedResource>? localizer = null)
    {
        var format = localizer?[nameof(MinLength)] ?? "{0} must be at least {1} characters.";
        return string.Format(CultureInfo.InvariantCulture, format, fieldName, min);
    }

    // ── Format & rules ────────────────────────────────────────────────────────
    public static string InvalidEmail(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(InvalidEmail)] ?? "A valid email address is required.";

    public static string PasswordsMustMatch(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(PasswordsMustMatch)] ?? "Passwords do not match.";

    public static string NewPasswordMustDiffer(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(NewPasswordMustDiffer)] ?? "New password must be different from the current password.";

    public static string PasswordInHistory(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(PasswordInHistory)] ?? "This password has been used recently. Please choose a different password.";

    public static string ImageConflict(IStringLocalizer<SharedResource>? localizer = null) =>
        localizer?[nameof(ImageConflict)] ?? "You cannot upload a new image and delete the current one simultaneously.";
}