using FSH.Framework.Core.Exceptions;
using FSH.Modules.Auditing.Contracts;

namespace Auditing.Tests.Contracts;

/// <summary>
/// Tests for ExceptionSeverityClassifier - maps exception types to audit severity levels.
/// </summary>
public sealed class ExceptionSeverityClassifierTests
{
    [Fact]
    public void Classify_Should_ReturnInformation_For_OperationCanceledException()
    {
        // Arrange
        var exception = new OperationCanceledException();

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Information);
    }

    [Fact]
    public void Classify_Should_ReturnInformation_For_TaskCanceledException()
    {
        // Arrange
        var exception = new TaskCanceledException();

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        // TaskCanceledException inherits from OperationCanceledException
        result.ShouldBe(AuditSeverity.Information);
    }

    [Fact]
    public void Classify_Should_ReturnWarning_For_UnauthorizedAccessException()
    {
        // Arrange
        var exception = new UnauthorizedAccessException();

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Warning);
    }

    [Fact]
    public void Classify_Should_ReturnError_For_ArgumentException()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    public void Classify_Should_ReturnError_For_InvalidOperationException()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Testing exception classification requires specific exception types")]
    public void Classify_Should_ReturnError_For_NullReferenceException()
    {
        // Arrange
        var exception = new NullReferenceException();

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Testing exception classification requires generic exception")]
    public void Classify_Should_ReturnError_For_GenericException()
    {
        // Arrange
        var exception = new Exception("Generic error");

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    public void Classify_Should_ReturnError_For_IOException()
    {
        // Arrange
        var exception = new IOException("IO error");

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    public void Classify_Should_ReturnError_For_TimeoutException()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [Fact]
    public void Classify_Should_ReturnInformation_For_DerivedOperationCanceledException()
    {
        // Arrange - Custom exception derived from OperationCanceledException
        var exception = new CustomCanceledException();

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Information);
    }

    // The localization work introduced LocalizedUnauthorizedAccessException specifically so that
    // subclassing the BCL type — rather than swapping it for a CustomException — keeps this
    // classifier mapping unauthorized access to Warning. That intent lived only in a code
    // comment: changing the base type would silently reclassify every unauthorized access as
    // Error and no test would have noticed. This is the test that notices.
    [Fact]
    public void Classify_Should_ReturnWarning_For_LocalizedUnauthorizedAccessException()
    {
        // Arrange
        var exception = new LocalizedUnauthorizedAccessException("Authentication failed.")
        {
            MessageKey = "Error.AuthenticationFailed",
        };

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Warning);
    }

    // The KeyNotFound counterpart lands on Error either way; pinned so the classification is
    // stated rather than left to be derived from the switch's default arm.
    [Fact]
    public void Classify_Should_ReturnError_For_LocalizedKeyNotFoundException()
    {
        // Arrange
        var exception = new LocalizedKeyNotFoundException("Not found.")
        {
            MessageKey = "Error.NotFound",
        };

        // Act
        var result = ExceptionSeverityClassifier.Classify(exception);

        // Assert
        result.ShouldBe(AuditSeverity.Error);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Test-only exception class")]
    private sealed class CustomCanceledException : OperationCanceledException
    {
    }
}