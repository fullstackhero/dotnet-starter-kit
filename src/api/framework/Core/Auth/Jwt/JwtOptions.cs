using System.ComponentModel.DataAnnotations;

namespace FSH.Framework.Core.Auth.Jwt;
public class JwtOptions : IValidatableObject
{
    public string Key { get; set; } = string.Empty;

    public int TokenExpirationInMinutes { get; set; } = 60;

    public int RefreshTokenExpirationInDays { get; set; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Key))
        {
            yield return new ValidationResult("No Key defined in JwtSettings config", new[] { nameof(Key) });
        }
    }
}
