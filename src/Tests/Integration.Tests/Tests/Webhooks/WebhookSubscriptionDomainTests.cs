using FSH.Modules.Webhooks.Domain;
using Integration.Tests.Infrastructure;
#pragma warning disable CA1707 // Test method names use underscores by convention

namespace Integration.Tests.Tests.Webhooks;

/// <summary>
/// Pure-domain edge cases for <see cref="WebhookSubscription"/> — the CSV event store, wildcard
/// matching, case-insensitivity, trimming, and the deactivation transition. These are the branches
/// the HTTP/dispatch tests don't exercise directly (they always use a single, exact-match event).
/// No web host is required; the entity is constructed in-process.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookSubscriptionDomainTests
{
    public WebhookSubscriptionDomainTests(FshWebApplicationFactory factory)
    {
        // Factory is injected to satisfy the shared collection fixture; these tests are
        // pure-domain and don't touch the host.
        _ = factory;
    }

    #region Happy Path

    [Fact]
    public void MatchesEvent_Should_ReturnTrue_When_EventIsInList()
    {
        // Arrange
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "user.created", "user.updated" },
            secretHash: null);

        // Act & Assert
        subscription.MatchesEvent("user.updated").ShouldBeTrue();
    }

    [Fact]
    public void GetEvents_Should_ReturnAllConfiguredEvents_When_MultipleSubscribed()
    {
        // Arrange
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "a.one", "b.two", "c.three" },
            secretHash: null);

        // Act
        var events = subscription.GetEvents();

        // Assert
        events.ShouldBe(new[] { "a.one", "b.two", "c.three" });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MatchesEvent_Should_BeCaseInsensitive_When_CasingDiffers()
    {
        // Arrange
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "User.Created" },
            secretHash: null);

        // Act & Assert — exact-name matching ignores case.
        subscription.MatchesEvent("user.created").ShouldBeTrue();
        subscription.MatchesEvent("USER.CREATED").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_MatchAnything_When_WildcardSubscribed()
    {
        // Arrange — a "*" subscription is a catch-all.
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "*" },
            secretHash: null);

        // Act & Assert
        subscription.MatchesEvent("any.unconfigured.event").ShouldBeTrue();
        subscription.MatchesEvent("another").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_ReturnFalse_When_EventNotSubscribed()
    {
        // Arrange
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "user.created" },
            secretHash: null);

        // Act & Assert
        subscription.MatchesEvent("user.deleted").ShouldBeFalse();
    }

    [Fact]
    public void GetEvents_Should_TrimAndDropEmptyEntries_When_CsvHasWhitespaceAndBlanks()
    {
        // Arrange — events with surrounding whitespace and a blank token. Create joins with ',',
        // so we pass tokens that, when stored and re-split, exercise the Trim/RemoveEmpty options.
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "  spaced.event  ", "", "  ", "tight.event" },
            secretHash: null);

        // Act
        var events = subscription.GetEvents();

        // Assert — blanks dropped, surviving tokens trimmed.
        events.ShouldContain("spaced.event");
        events.ShouldContain("tight.event");
        events.ShouldNotContain(string.Empty);
        events.Length.ShouldBe(2);
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse_When_Called()
    {
        // Arrange
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "user.created" },
            secretHash: null);
        subscription.IsActive.ShouldBeTrue();

        // Act
        subscription.Deactivate();

        // Assert
        subscription.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_PopulateFields_And_GenerateId_When_Valid()
    {
        // Arrange & Act
        var subscription = WebhookSubscription.Create(
            "https://example.com/hook",
            new[] { "user.created" },
            secretHash: "hashed-secret");

        // Assert
        subscription.Id.ShouldNotBe(Guid.Empty);
        subscription.Url.ShouldBe("https://example.com/hook");
        subscription.SecretHash.ShouldBe("hashed-secret");
        subscription.IsActive.ShouldBeTrue();
        subscription.CreatedAtUtc.ShouldNotBe(default);
    }

    #endregion
}
