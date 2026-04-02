using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using FSH.Framework.Shared.Localization;
using System.Globalization;

namespace FSH.Playground.Blazor.Components.Validation;

/// <summary>
/// Provides localized validation error messages for DataAnnotations.
/// This class formats validation messages using IStringLocalizer with SharedResource.
/// </summary>
internal static class LocalizedValidationMessages
{
    /// <summary>
    /// Gets a localized Required field error message.
    /// </summary>
    public static string GetRequiredMessage(IStringLocalizer<SharedResource> L, string fieldName)
    {
        return string.Format(CultureInfo.InvariantCulture, L["Required"], fieldName);
    }

    /// <summary>
    /// Gets a localized MaxLength error message.
    /// </summary>
    public static string GetMaxLengthMessage(IStringLocalizer<SharedResource> L, string fieldName, int maxLength)
    {
        return string.Format(CultureInfo.InvariantCulture, L["MaxLength"], fieldName, maxLength);
    }

    /// <summary>
    /// Gets a localized MinLength error message.
    /// </summary>
    public static string GetMinLengthMessage(IStringLocalizer<SharedResource> L, string fieldName, int minLength)
    {
        return string.Format(CultureInfo.InvariantCulture, L["MinLength"], fieldName, minLength);
    }

    /// <summary>
    /// Gets a localized Invalid Email error message.
    /// </summary>
    public static string GetInvalidEmailMessage(IStringLocalizer<SharedResource> L)
    {
        return L["InvalidEmail"];
    }

    /// <summary>
    /// Gets a localized Passwords Must Match error message.
    /// </summary>
    public static string GetPasswordsMustMatchMessage(IStringLocalizer<SharedResource> L)
    {
        return L["PasswordsMustMatch"];
    }
}
