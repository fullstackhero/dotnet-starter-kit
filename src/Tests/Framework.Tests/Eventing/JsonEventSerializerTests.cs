using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Serialization;

namespace Framework.Tests.Eventing;

public sealed class JsonEventSerializerTests
{
    #region Test doubles

    public sealed record SampleIntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
        public string? TenantId { get; init; }
        public string CorrelationId { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string Payload { get; init; } = string.Empty;
    }

    #endregion

    private readonly JsonEventSerializer _sut = new();

    #region Happy Path

    [Fact]
    public void SerializeThenDeserialize_Should_RoundTrip_When_TypeResolvable()
    {
        // Arrange
        var original = new SampleIntegrationEvent
        {
            TenantId = "tenant-1",
            CorrelationId = "corr-1",
            Source = "tests",
            Payload = "hello"
        };
        var typeName = original.GetType().AssemblyQualifiedName!;

        // Act
        var json = _sut.Serialize(original);
        var roundTripped = _sut.Deserialize(json, typeName);

        // Assert
        roundTripped.ShouldNotBeNull();
        var typed = roundTripped.ShouldBeOfType<SampleIntegrationEvent>();
        typed.Id.ShouldBe(original.Id);
        typed.TenantId.ShouldBe("tenant-1");
        typed.CorrelationId.ShouldBe("corr-1");
        typed.Payload.ShouldBe("hello");
    }

    [Fact]
    public void Serialize_Should_UseCamelCase_When_Serializing()
    {
        // Arrange
        var @event = new SampleIntegrationEvent { CorrelationId = "c", Source = "s" };

        // Act
        var json = _sut.Serialize(@event);

        // Assert — camelCase naming policy applied (case-sensitive check; PascalCase must be absent).
        json.ShouldContain("\"correlationId\"");
        json.ShouldContain("\"occurredOnUtc\"");
        json.ShouldNotContain("\"CorrelationId\"", Case.Sensitive);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_Should_ReturnNull_When_TypeNameUnresolvable()
    {
        // Arrange
        var @event = new SampleIntegrationEvent { CorrelationId = "c", Source = "s" };
        var json = _sut.Serialize(@event);

        // Act
        var result = _sut.Deserialize(json, "Some.Unknown.Type, Nonexistent.Assembly");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Serialize_Should_Throw_When_EventNull()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Serialize(null!));
    }

    [Fact]
    public void Deserialize_Should_Throw_When_PayloadOrTypeNameNull()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Deserialize(null!, "x"));
        Should.Throw<ArgumentNullException>(() => _sut.Deserialize("{}", null!));
    }

    #endregion
}
