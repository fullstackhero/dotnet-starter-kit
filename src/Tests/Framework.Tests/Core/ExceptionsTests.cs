using System.Net;
using FSH.Framework.Core.Exceptions;

namespace Framework.Tests.Core;

public sealed class ExceptionsTests
{
    #region CustomException

    [Fact]
    public void Ctor_Should_DefaultToInternalServerError_When_ParameterlessUsed()
    {
        // Arrange & Act
        var exception = new CustomException();

        // Assert
        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.Message.ShouldBe("An error occurred.");
        exception.ErrorMessages.ShouldBeEmpty();
    }

    [Fact]
    public void Ctor_Should_SetMessage_When_MessageOnlyProvided()
    {
        // Arrange & Act
        var exception = new CustomException("boom");

        // Assert
        exception.Message.ShouldBe("boom");
        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        exception.ErrorMessages.ShouldBeEmpty();
    }

    [Fact]
    public void Ctor_Should_SetErrorsAndStatusCode_When_FullArgsProvided()
    {
        // Arrange
        var errors = new[] { "first", "second" };

        // Act
        var exception = new CustomException("bad request", errors, HttpStatusCode.BadRequest);

        // Assert
        exception.Message.ShouldBe("bad request");
        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.ErrorMessages.Count.ShouldBe(2);
        exception.ErrorMessages.ShouldContain("first");
        exception.ErrorMessages.ShouldContain("second");
    }

    [Fact]
    public void Ctor_Should_DefaultToEmptyErrors_When_NullErrorsProvided()
    {
        // Arrange & Act
        var exception = new CustomException("msg", errors: null, HttpStatusCode.Conflict);

        // Assert
        exception.ErrorMessages.ShouldNotBeNull();
        exception.ErrorMessages.ShouldBeEmpty();
        exception.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public void Ctor_Should_PreserveInnerException_When_InnerProvided()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");

        // Act
        var exception = new CustomException("outer", inner, HttpStatusCode.ServiceUnavailable);

        // Assert
        exception.InnerException.ShouldBeSameAs(inner);
        exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        exception.ErrorMessages.ShouldBeEmpty();
    }

    #endregion

    #region NotFoundException

    [Fact]
    public void NotFoundException_Should_Map404_When_Constructed()
    {
        // Arrange & Act
        var defaultException = new NotFoundException();
        var messageException = new NotFoundException("missing user");
        var errorsException = new NotFoundException("missing", new[] { "id=1" });

        // Assert
        defaultException.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        defaultException.Message.ShouldBe("Resource not found.");
        messageException.Message.ShouldBe("missing user");
        messageException.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        errorsException.ErrorMessages.ShouldContain("id=1");
    }

    [Fact]
    public void NotFoundException_Should_BeCustomException_When_TypeChecked()
    {
        // Arrange & Act
        var exception = new NotFoundException();

        // Assert
        exception.ShouldBeAssignableTo<CustomException>();
    }

    #endregion

    #region ForbiddenException

    [Fact]
    public void ForbiddenException_Should_Map403_When_Constructed()
    {
        // Arrange & Act
        var defaultException = new ForbiddenException();
        var messageException = new ForbiddenException("no access");

        // Assert
        defaultException.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        defaultException.Message.ShouldBe("Unauthorized access.");
        messageException.Message.ShouldBe("no access");
        messageException.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UnauthorizedException

    [Fact]
    public void UnauthorizedException_Should_Map401_When_Constructed()
    {
        // Arrange & Act
        var defaultException = new UnauthorizedException();
        var errorsException = new UnauthorizedException("login failed", new[] { "expired token" });

        // Assert
        defaultException.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        defaultException.Message.ShouldBe("Authentication failed.");
        errorsException.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        errorsException.ErrorMessages.ShouldContain("expired token");
    }

    #endregion

    #region Localized factory

    [Fact]
    public void Localized_Should_SetKeyArgsAndCatalog_When_ModuleCatalogNamed()
    {
        // Arrange
        var exception = new NotFoundException("Item 42 not found.");

        // Act
        var localized = exception.Localized<ExceptionsTests>("Module.ItemNotFound", 42, "x");

        // Assert: the same instance comes back, so `throw` keeps the subclass and its status code.
        localized.ShouldBeSameAs(exception);
        localized.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        localized.MessageKey.ShouldBe("Module.ItemNotFound");
        localized.MessageArgs.ShouldBe(new object[] { 42, "x" });
        localized.ResourceSource.ShouldBe(typeof(ExceptionsTests));
        localized.Message.ShouldBe("Item 42 not found.");
    }

    [Fact]
    public void Localized_Should_UseSharedCatalog_When_NoCatalogNamed()
    {
        // Arrange & Act
        var exception = new NotFoundException("gone").Localized("Error.NotFound");

        // Assert
        exception.MessageKey.ShouldBe("Error.NotFound");
        exception.MessageArgs.ShouldBeEmpty();
        exception.ResourceSource.ShouldBeNull();
    }

    [Fact]
    public void DefaultCtors_Should_CarrySharedKeys_When_NoMessageGiven()
    {
        // Arrange & Act
        var forbidden = new ForbiddenException();
        var unauthorized = new UnauthorizedException();

        // Assert
        forbidden.MessageKey.ShouldBe("Error.ForbiddenAccess");
        forbidden.ResourceSource.ShouldBeNull();
        unauthorized.MessageKey.ShouldBe("Error.AuthenticationFailed");
        unauthorized.ResourceSource.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Localized_Should_Throw_When_KeyIsBlank(string key)
    {
        // Arrange
        var exception = new NotFoundException("gone");

        // Act & Assert
        Should.Throw<ArgumentException>(() => exception.Localized<ExceptionsTests>(key));
        Should.Throw<ArgumentException>(() => exception.Localized(key));
        exception.MessageKey.ShouldBeNull();
    }

    #endregion
}
