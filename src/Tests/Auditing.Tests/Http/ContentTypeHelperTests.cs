using System.Reflection;

namespace Auditing.Tests.Http;

/// <summary>
/// Tests for ContentTypeHelper - determines if content types are JSON-like for body capture.
/// </summary>
public sealed class ContentTypeHelperTests
{
    private static readonly HashSet<string> DefaultAllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/problem+json",
        "text/json"
    };

    // Use reflection to access internal static class
    private static bool IsJsonLike(string? contentType, ISet<string> allowed)
    {
        var assembly = typeof(FSH.Modules.Auditing.AuditingModule).Assembly;
        var helperType = assembly.GetType("FSH.Modules.Auditing.ContentTypeHelper");
        helperType.ShouldNotBeNull("ContentTypeHelper type should exist");

        var method = helperType.GetMethod("IsJsonLike", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        method.ShouldNotBeNull("IsJsonLike method should exist");

        return (bool)method.Invoke(null, [contentType, allowed])!;
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_When_ContentTypeIsNull()
    {
        // Act
        var result = IsJsonLike(null, DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_When_ContentTypeIsEmpty()
    {
        // Act
        var result = IsJsonLike(string.Empty, DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_When_ContentTypeIsWhitespace()
    {
        // Act
        var result = IsJsonLike("   ", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnTrue_For_ApplicationJson()
    {
        // Act
        var result = IsJsonLike("application/json", DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnTrue_For_ApplicationProblemJson()
    {
        // Act
        var result = IsJsonLike("application/problem+json", DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnTrue_For_TextJson()
    {
        // Act
        var result = IsJsonLike("text/json", DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_IgnoreCharset_In_ContentType()
    {
        // Act
        var result = IsJsonLike("application/json; charset=utf-8", DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_IgnoreMultipleParameters_In_ContentType()
    {
        // Act
        var result = IsJsonLike("application/json; charset=utf-8; boundary=something", DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_For_TextHtml()
    {
        // Act
        var result = IsJsonLike("text/html", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_For_TextPlain()
    {
        // Act
        var result = IsJsonLike("text/plain", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_For_ApplicationXml()
    {
        // Act
        var result = IsJsonLike("application/xml", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_For_MultipartFormData()
    {
        // Act
        var result = IsJsonLike("multipart/form-data", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_For_ApplicationOctetStream()
    {
        // Act
        var result = IsJsonLike("application/octet-stream", DefaultAllowedTypes);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("APPLICATION/JSON")]
    [InlineData("Application/Json")]
    [InlineData("application/JSON")]
    public void IsJsonLike_Should_BeCaseInsensitive(string contentType)
    {
        // Act
        var result = IsJsonLike(contentType, DefaultAllowedTypes);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnTrue_For_CustomAllowedType()
    {
        // Arrange
        var customAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/vnd.api+json"
        };

        // Act
        var result = IsJsonLike("application/vnd.api+json", customAllowed);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsJsonLike_Should_ReturnFalse_When_TypeNotInAllowedSet()
    {
        // Arrange
        var limitedAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/json"
        };

        // Act - text/json is not in the limited set
        var result = IsJsonLike("text/json", limitedAllowed);

        // Assert
        result.ShouldBeFalse();
    }
}