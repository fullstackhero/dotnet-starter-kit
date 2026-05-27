using FSH.Modules.Auditing.Contracts;

namespace Auditing.Tests.Contracts;

/// <summary>
/// Tests for AuditEnvelope - the concrete event instance for audit persistence.
/// </summary>
public sealed class AuditEnvelopeTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly DateTime TestOccurredAt = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TestReceivedAt = new(2024, 1, 15, 12, 0, 1, DateTimeKind.Utc);

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_PayloadIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.Activity,
            AuditSeverity.Information,
            "tenant1",
            "user1",
            "John Doe",
            "trace-123",
            "span-456",
            "correlation-789",
            "request-abc",
            "TestSource",
            AuditTag.None,
            null!));
    }

    [Fact]
    public void Constructor_Should_SetAllProperties_Correctly()
    {
        // Arrange
        var payload = new { action = "test" };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.Security,
            AuditSeverity.Warning,
            "tenant1",
            "user1",
            "John Doe",
            "trace-123",
            "span-456",
            "correlation-789",
            "request-abc",
            "TestSource",
            AuditTag.PiiMasked | AuditTag.Authentication,
            payload);

        // Assert
        envelope.Id.ShouldBe(TestId);
        envelope.OccurredAtUtc.ShouldBe(TestOccurredAt);
        envelope.ReceivedAtUtc.ShouldBe(TestReceivedAt);
        envelope.EventType.ShouldBe(AuditEventType.Security);
        envelope.Severity.ShouldBe(AuditSeverity.Warning);
        envelope.TenantId.ShouldBe("tenant1");
        envelope.UserId.ShouldBe("user1");
        envelope.UserName.ShouldBe("John Doe");
        envelope.TraceId.ShouldBe("trace-123");
        envelope.SpanId.ShouldBe("span-456");
        envelope.CorrelationId.ShouldBe("correlation-789");
        envelope.RequestId.ShouldBe("request-abc");
        envelope.Source.ShouldBe("TestSource");
        envelope.Tags.ShouldBe(AuditTag.PiiMasked | AuditTag.Authentication);
        envelope.Payload.ShouldBe(payload);
    }

    [Fact]
    public void Constructor_Should_ConvertToUtc_When_OccurredAtNotUtc()
    {
        // Arrange
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);
        var payload = new { action = "test" };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            localTime,
            TestReceivedAt,
            AuditEventType.Activity,
            AuditSeverity.Information,
            null, null, null, null, null, null, null, null,
            AuditTag.None,
            payload);

        // Assert
        envelope.OccurredAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_Should_ConvertToUtc_When_ReceivedAtNotUtc()
    {
        // Arrange
        var localTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);
        var payload = new { action = "test" };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            localTime,
            AuditEventType.Activity,
            AuditSeverity.Information,
            null, null, null, null, null, null, null, null,
            AuditTag.None,
            payload);

        // Assert
        envelope.ReceivedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_Should_PreserveUtc_When_OccurredAtIsUtc()
    {
        // Arrange
        var payload = new { action = "test" };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.Activity,
            AuditSeverity.Information,
            null, null, null, null, null, null, null, null,
            AuditTag.None,
            payload);

        // Assert
        envelope.OccurredAtUtc.ShouldBe(TestOccurredAt);
        envelope.OccurredAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_Should_AllowNullOptionalFields()
    {
        // Arrange
        var payload = new { action = "test" };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.Activity,
            AuditSeverity.Information,
            tenantId: null,
            userId: null,
            userName: null,
            traceId: null,
            spanId: null,
            correlationId: null,
            requestId: null,
            source: null,
            AuditTag.None,
            payload);

        // Assert
        envelope.TenantId.ShouldBeNull();
        envelope.UserId.ShouldBeNull();
        envelope.UserName.ShouldBeNull();
        envelope.CorrelationId.ShouldBeNull();
        envelope.RequestId.ShouldBeNull();
        envelope.Source.ShouldBeNull();
        // TraceId and SpanId may be populated from Activity.Current if null
    }

    [Fact]
    public void Constructor_Should_AcceptAllEventTypes()
    {
        // Arrange
        var payload = new { action = "test" };

        foreach (var eventType in Enum.GetValues<AuditEventType>())
        {
            // Act
            var envelope = new AuditEnvelope(
                Guid.NewGuid(),
                TestOccurredAt,
                TestReceivedAt,
                eventType,
                AuditSeverity.Information,
                null, null, null, null, null, null, null, null,
                AuditTag.None,
                payload);

            // Assert
            envelope.EventType.ShouldBe(eventType);
        }
    }

    [Fact]
    public void Constructor_Should_AcceptAllSeverityLevels()
    {
        // Arrange
        var payload = new { action = "test" };

        foreach (var severity in Enum.GetValues<AuditSeverity>())
        {
            // Act
            var envelope = new AuditEnvelope(
                Guid.NewGuid(),
                TestOccurredAt,
                TestReceivedAt,
                AuditEventType.Activity,
                severity,
                null, null, null, null, null, null, null, null,
                AuditTag.None,
                payload);

            // Assert
            envelope.Severity.ShouldBe(severity);
        }
    }

    [Fact]
    public void Constructor_Should_AcceptCombinedTags()
    {
        // Arrange
        var payload = new { action = "test" };
        var combinedTags = AuditTag.PiiMasked | AuditTag.Authentication | AuditTag.RetainedLong;

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.Security,
            AuditSeverity.Information,
            null, null, null, null, null, null, null, null,
            combinedTags,
            payload);

        // Assert
        envelope.Tags.ShouldBe(combinedTags);
        envelope.Tags.HasFlag(AuditTag.PiiMasked).ShouldBeTrue();
        envelope.Tags.HasFlag(AuditTag.Authentication).ShouldBeTrue();
        envelope.Tags.HasFlag(AuditTag.RetainedLong).ShouldBeTrue();
        envelope.Tags.HasFlag(AuditTag.HealthCheck).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_Should_AcceptComplexPayload()
    {
        // Arrange
        var complexPayload = new
        {
            Users = new[]
            {
                new { Id = 1, Name = "John" },
                new { Id = 2, Name = "Jane" }
            },
            Metadata = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 123
            },
            Timestamp = DateTime.UtcNow
        };

        // Act
        var envelope = new AuditEnvelope(
            TestId,
            TestOccurredAt,
            TestReceivedAt,
            AuditEventType.EntityChange,
            AuditSeverity.Information,
            null, null, null, null, null, null, null, null,
            AuditTag.None,
            complexPayload);

        // Assert
        envelope.Payload.ShouldBe(complexPayload);
    }
}