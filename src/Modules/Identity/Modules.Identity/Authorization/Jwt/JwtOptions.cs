using System.ComponentModel.DataAnnotations;

namespace FSH.Modules.Identity.Authorization.Jwt;

public class JwtOptions : IValidatableObject
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(SigningKey))
        {
            yield return new ValidationResult("No Key defined in JwtOptions config", [nameof(SigningKey)]);
        }

        if (!string.IsNullOrEmpty(SigningKey) && SigningKey.Length < 32)
        {
            yield return new ValidationResult("SigningKey must be at least 32 characters long.", [nameof(SigningKey)]);
        }

        // Reject obvious placeholder strings shipped in sample configs. The framework's
        // own appsettings.json carries a "replace-with-..." sample value; if an operator
        // forgets to override it in their deployment, tokens are forgeable by anyone
        // who has read this repo. Better to refuse to start than to silently issue them.
        if (!string.IsNullOrEmpty(SigningKey) &&
            SigningKey.Contains("replace-with", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "SigningKey looks like a sample placeholder ('replace-with-…'). Set a real secret via environment variable or user-secrets before starting the host.",
                [nameof(SigningKey)]);
        }

        if (string.IsNullOrEmpty(Issuer))
        {
            yield return new ValidationResult("No Issuer defined in JwtOptions config", [nameof(Issuer)]);
        }

        if (string.IsNullOrEmpty(Audience))
        {
            yield return new ValidationResult("No Audience defined in JwtOptions config", [nameof(Audience)]);
        }
    }
}