using FSH.Modules.Identity.Authorization.Jwt;
using System.ComponentModel.DataAnnotations;

namespace Identity.Tests.Authorization;

/// <summary>
/// Tests for JwtOptions validation - security critical configuration.
/// </summary>
public sealed class JwtOptionsTests
{
    [Fact]
    public void Validate_Should_ReturnNoErrors_When_AllFieldsValid()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = "ThisIsAVeryLongSecretKeyForJwtSigning", // 40 chars
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_ReturnError_When_SigningKeyIsEmpty()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = string.Empty,
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.SigningKey)));
        results.ShouldContain(r => r.ErrorMessage!.Contains("No Key defined"));
    }

    [Fact]
    public void Validate_Should_ReturnError_When_SigningKeyIsNull()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = null!,
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.SigningKey)));
    }

    [Fact]
    public void Validate_Should_ReturnError_When_SigningKeyTooShort()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = "ShortKey", // Only 8 chars
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.SigningKey)));
        results.ShouldContain(r => r.ErrorMessage!.Contains("at least 32 characters"));
    }

    [Fact]
    public void Validate_Should_Pass_When_SigningKeyExactly32Chars()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = "12345678901234567890123456789012", // Exactly 32 chars
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_ReturnError_When_IssuerIsEmpty()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = "ThisIsAVeryLongSecretKeyForJwtSigning",
            Issuer = string.Empty,
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Issuer)));
        results.ShouldContain(r => r.ErrorMessage!.Contains("No Issuer defined"));
    }

    [Fact]
    public void Validate_Should_ReturnError_When_AudienceIsEmpty()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = "ThisIsAVeryLongSecretKeyForJwtSigning",
            Issuer = "https://example.com",
            Audience = string.Empty
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Audience)));
        results.ShouldContain(r => r.ErrorMessage!.Contains("No Audience defined"));
    }

    [Fact]
    public void Validate_Should_ReturnMultipleErrors_When_AllFieldsInvalid()
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = string.Empty,
            Issuer = string.Empty,
            Audience = string.Empty
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.SigningKey)));
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Issuer)));
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Audience)));
    }

    [Fact]
    public void DefaultValues_Should_BeSet()
    {
        // Arrange & Act
        var options = new JwtOptions();

        // Assert
        options.AccessTokenMinutes.ShouldBe(30);
        options.RefreshTokenDays.ShouldBe(7);
        options.Issuer.ShouldBe(string.Empty);
        options.Audience.ShouldBe(string.Empty);
        options.SigningKey.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(256)]
    public void Validate_Should_Pass_When_SigningKeyLengthIsValid(int length)
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = new string('x', length),
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        if (length >= 32)
        {
            results.ShouldBeEmpty();
        }
        else
        {
            results.ShouldNotBeEmpty();
        }
    }

    [Theory]
    [InlineData("replace-with-256-bit-secret-min-32-chars")]
    [InlineData("REPLACE-WITH-something-long-enough-to-pass-32")]
    [InlineData("prefixed-replace-with-something-suffixed-here")]
    public void Validate_Should_Reject_Placeholder_SigningKey(string placeholder)
    {
        // Arrange — values that pass the length check but are clearly
        // sample placeholders carried over from appsettings.json.
        var options = new JwtOptions
        {
            SigningKey = placeholder,
            Issuer = "https://example.com",
            Audience = "https://api.example.com",
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r =>
            r.MemberNames.Contains(nameof(JwtOptions.SigningKey)) &&
            r.ErrorMessage!.Contains("placeholder", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(31)]
    public void Validate_Should_Fail_When_SigningKeyLengthIsInsufficient(int length)
    {
        // Arrange
        var options = new JwtOptions
        {
            SigningKey = new string('x', length),
            Issuer = "https://example.com",
            Audience = "https://api.example.com"
        };
        var context = new ValidationContext(options);

        // Act
        var results = options.Validate(context).ToList();

        // Assert
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.SigningKey)));
    }
}