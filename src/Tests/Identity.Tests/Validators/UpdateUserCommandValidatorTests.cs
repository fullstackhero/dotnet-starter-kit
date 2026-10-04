using System.Globalization;
using System.Linq;
using FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;
using FSH.Modules.Identity.Features.v1.Users.UpdateUser;
using Identity.Tests.Support;
using Shouldly;
using Xunit;

namespace Identity.Tests.Validators;

public sealed class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _sut = new(SharedResourcesLocalizerFactory.Create());

    private static TResult WithCulture<TResult>(string culture, Func<TResult> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Validate_Should_Pass_When_ValidMinimalCommand()
    {
        // Arrange
        var command = new UpdateUserCommand { Id = "user-123" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_IdIsEmpty()
    {
        // Arrange
        var command = new UpdateUserCommand { Id = "" };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_Should_Fail_When_FirstNameExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateUserCommand
        {
            Id = "user-123",
            FirstName = new string('a', 51)
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        // Arrange
        var command = new UpdateUserCommand
        {
            Id = "user-123",
            Email = "not-an-email"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_Should_Fail_When_DeleteImageAndUploadImage_Simultaneously()
    {
        // Arrange
        var command = new UpdateUserCommand
        {
            Id = "user-123",
            DeleteCurrentImage = true,
            Image = new FSH.Framework.Shared.Storage.FileUploadRequest { FileName = "test.png", Data = [0] }
        };

        // Act — pin en-US so the localized message resolves to the neutral (English) catalog.
        var result = WithCulture("en-US", () => _sut.Validate(command));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "You cannot upload a new image and delete the current one simultaneously.");
    }

    [Theory]
    [InlineData("pt-BR", true)]
    [InlineData("en-US", true)]
    [InlineData(null, true)]
    // Empty/whitespace is treated as "not provided": the .When(!IsNullOrWhiteSpace) guard skips the
    // rule, so an empty locale is valid (the caller simply isn't changing it).
    [InlineData("", true)]
    // Case matters: SupportedCultures.Tags.Contains is ordinal, so a wrong-case tag is rejected —
    // pins the ordinal comparison against an accidental case-insensitive refactor.
    [InlineData("pt-br", false)]
    [InlineData("PT-BR", false)]
    [InlineData("xx-YY", false)]
    [InlineData("notaculture", false)]
    public void Locale_Must_Be_Supported_Or_Null(string? locale, bool expectedValid)
    {
        // Arrange
        var command = new UpdateUserCommand { Id = "user-123", Locale = locale };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.Any(e => e.PropertyName == nameof(UpdateUserCommand.Locale)).ShouldBe(!expectedValid);
    }

    [Fact]
    public void UserId_required_message_is_localized_under_ptBR()
    {
        // Act
        var result = WithCulture("pt-BR", () => _sut.Validate(new UpdateUserCommand { Id = "" }));

        // Assert — custom WithMessage resolves from the .pt-BR catalog.
        result.Errors.Single(e => e.PropertyName == "Id").ErrorMessage
            .ShouldBe("O ID do usuário é obrigatório.");
    }

    [Fact]
    public void Builtin_validation_message_is_localized_under_ptBR()
    {
        // Act — FluentValidation resolves built-in messages via CurrentUICulture (ships a pt catalog).
        var result = WithCulture("pt-BR", () =>
            _sut.Validate(new UpdateUserCommand { Id = "user-123", Email = "not-an-email" }));

        // Assert — pin the actual Portuguese text, not merely the absence of the English one.
        // "does not contain the English sentence" is satisfied by a blank message, by a raw
        // resource key leaking through, and by any wrong-but-non-English string, so it stayed
        // green through exactly the failures it existed to catch.
        var message = result.Errors.Single(e => e.PropertyName == "Email").ErrorMessage;
        message.ShouldBe("'Email' é um endereço de email inválido.");
    }
}
