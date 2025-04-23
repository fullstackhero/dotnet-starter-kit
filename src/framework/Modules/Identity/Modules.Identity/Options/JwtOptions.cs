using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Identity.Options;
public class JwtOptions : IValidatableObject
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public int TokenExpirationInMinutes { get; set; } = 60;

    public int RefreshTokenExpirationInDays { get; set; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Key))
        {
            yield return new ValidationResult("No Key defined in JwtOptions config", [nameof(Key)]);
        }

        if (string.IsNullOrEmpty(Issuer))
        {
            yield return new ValidationResult("No Issuer defined in JwtOptions config", [nameof(Key)]);
        }

        if (string.IsNullOrEmpty(Audience))
        {
            yield return new ValidationResult("No Audience defined in JwtOptions config", [nameof(Key)]);
        }
    }
}