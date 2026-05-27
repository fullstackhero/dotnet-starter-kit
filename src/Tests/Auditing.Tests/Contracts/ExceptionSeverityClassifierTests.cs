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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Test-only exception class")]
    private sealed class CustomCanceledException : OperationCanceledException
    {
    }
}