using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;
using FSH.Modules.Auditing.Contracts.v1.GetAuditsByTrace;
using FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;
using FSH.Modules.Auditing.Contracts.v1.GetExceptionAudits;
using FSH.Modules.Auditing.Contracts.v1.GetSecurityAudits;
using FSH.Modules.Auditing.Features.v1.GetAudits;
using FSH.Modules.Auditing.Features.v1.GetAuditsByCorrelation;
using FSH.Modules.Auditing.Features.v1.GetAuditsByTrace;
using FSH.Modules.Auditing.Features.v1.GetAuditSummary;
using FSH.Modules.Auditing.Features.v1.GetExceptionAudits;
using FSH.Modules.Auditing.Features.v1.GetSecurityAudits;

namespace Generic.Tests.Validators;

/// <summary>
/// Tests for generic date range validation rules (FromUtc less than or equal to ToUtc)
/// that are shared across queries with date filtering.
/// </summary>
public sealed class DateRangeValidatorTests
{
    private static readonly DateTime BaseDate = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DateRange_Should_Pass_When_BothNull_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { FromUtc = null, ToUtc = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_BothNull_GetAuditsByCorrelation()
    {
        // Arrange
        var validator = new GetAuditsByCorrelationQueryValidator();
        var query = new GetAuditsByCorrelationQuery { CorrelationId = "test-id", FromUtc = null, ToUtc = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_BothNull_GetAuditsByTrace()
    {
        // Arrange
        var validator = new GetAuditsByTraceQueryValidator();
        var query = new GetAuditsByTraceQuery { TraceId = "test-trace", FromUtc = null, ToUtc = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_BothNull_GetAuditSummary()
    {
        // Arrange
        var validator = new GetAuditSummaryQueryValidator();
        var query = new GetAuditSummaryQuery { FromUtc = null, ToUtc = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_OnlyFromUtcSet_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { FromUtc = BaseDate, ToUtc = null };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_OnlyToUtcSet_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { FromUtc = null, ToUtc = BaseDate };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_FromUtcEqualsToUtc_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery { FromUtc = BaseDate, ToUtc = BaseDate };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Pass_When_FromUtcBeforeToUtc_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery
        {
            FromUtc = BaseDate,
            ToUtc = BaseDate.AddDays(7)
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetAudits()
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery
        {
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetAuditsByCorrelation()
    {
        // Arrange
        var validator = new GetAuditsByCorrelationQueryValidator();
        var query = new GetAuditsByCorrelationQuery
        {
            CorrelationId = "test-id",
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetAuditsByTrace()
    {
        // Arrange
        var validator = new GetAuditsByTraceQueryValidator();
        var query = new GetAuditsByTraceQuery
        {
            TraceId = "test-trace",
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetAuditSummary()
    {
        // Arrange
        var validator = new GetAuditSummaryQueryValidator();
        var query = new GetAuditSummaryQuery
        {
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetExceptionAudits()
    {
        // Arrange
        var validator = new GetExceptionAuditsQueryValidator();
        var query = new GetExceptionAuditsQuery
        {
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Fact]
    public void DateRange_Should_Fail_When_FromUtcAfterToUtc_GetSecurityAudits()
    {
        // Arrange
        var validator = new GetSecurityAuditsQueryValidator();
        var query = new GetSecurityAuditsQuery
        {
            FromUtc = BaseDate.AddDays(7),
            ToUtc = BaseDate
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("FromUtc must be less than or equal to ToUtc"));
    }

    [Theory]
    [InlineData(1)]    // 1 second apart
    [InlineData(60)]   // 1 minute apart
    [InlineData(3600)] // 1 hour apart
    public void DateRange_Should_Pass_When_FromUtcSlightlyBeforeToUtc(int secondsDiff)
    {
        // Arrange
        var validator = new GetAuditsQueryValidator();
        var query = new GetAuditsQuery
        {
            FromUtc = BaseDate,
            ToUtc = BaseDate.AddSeconds(secondsDiff)
        };

        // Act
        var result = validator.Validate(query);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}