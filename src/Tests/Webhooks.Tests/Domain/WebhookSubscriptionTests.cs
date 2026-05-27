using FSH.Modules.Webhooks.Domain;

namespace Webhooks.Tests.Domain;

public sealed class WebhookSubscriptionTests
{
    #region Happy Path

    [Fact]
    public void Create_Should_Populate_Fields_And_Default_Active_When_Valid()
    {
        var sub = WebhookSubscription.Create(
            "https://example.com/hook",
            ["user.created", "user.deleted"],
            "hashed-secret");

        sub.Url.ShouldBe("https://example.com/hook");
        sub.EventsCsv.ShouldBe("user.created,user.deleted");
        sub.SecretHash.ShouldBe("hashed-secret");
        sub.IsActive.ShouldBeTrue();
        sub.Id.ShouldNotBe(Guid.Empty);
        sub.CreatedAtUtc.ShouldNotBe(default);
    }

    [Fact]
    public void Create_Should_Allow_Null_SecretHash()
    {
        var sub = WebhookSubscription.Create("https://example.com", ["a"], secretHash: null);

        sub.SecretHash.ShouldBeNull();
    }

    [Fact]
    public void GetEvents_Should_Roundtrip_All_Event_Types()
    {
        var sub = WebhookSubscription.Create("https://x", ["one", "two", "three"], null);

        sub.GetEvents().ShouldBe(["one", "two", "three"]);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var sub = WebhookSubscription.Create("https://x", ["a"], null);

        sub.Deactivate();

        sub.IsActive.ShouldBeFalse();
    }

    #endregion

    #region Exceptions

    [Fact]
    public void Create_Should_Throw_When_Url_Is_Null()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookSubscription.Create(null!, ["a"], null));
    }

    [Fact]
    public void Create_Should_Throw_When_Events_Is_Null()
    {
        Should.Throw<ArgumentNullException>(() =>
            WebhookSubscription.Create("https://x", null!, null));
    }

    #endregion

    #region Event Matching

    [Fact]
    public void MatchesEvent_Should_Return_True_When_Exact_Match()
    {
        var sub = WebhookSubscription.Create("https://x", ["user.created", "order.placed"], null);

        sub.MatchesEvent("order.placed").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_Be_Case_Insensitive()
    {
        var sub = WebhookSubscription.Create("https://x", ["User.Created"], null);

        sub.MatchesEvent("user.created").ShouldBeTrue();
        sub.MatchesEvent("USER.CREATED").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_Return_True_For_Any_Event_When_Wildcard_Subscribed()
    {
        var sub = WebhookSubscription.Create("https://x", ["*"], null);

        sub.MatchesEvent("anything.at.all").ShouldBeTrue();
        sub.MatchesEvent("user.created").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_Return_True_When_Wildcard_Mixed_With_Specific_Events()
    {
        var sub = WebhookSubscription.Create("https://x", ["user.created", "*"], null);

        sub.MatchesEvent("totally.unlisted").ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_Should_Return_False_When_No_Match_And_No_Wildcard()
    {
        var sub = WebhookSubscription.Create("https://x", ["user.created"], null);

        sub.MatchesEvent("user.deleted").ShouldBeFalse();
    }

    [Fact]
    public void MatchesEvent_Should_Return_False_When_No_Events_Subscribed()
    {
        // string.Join over an empty array yields "", which splits to an empty set.
        var sub = WebhookSubscription.Create("https://x", [], null);

        sub.MatchesEvent("user.created").ShouldBeFalse();
    }

    #endregion

    #region EventsCsv Parsing Edge Cases

    [Fact]
    public void GetEvents_Should_Trim_Whitespace_Around_Event_Types()
    {
        var sub = WebhookSubscription.Create("https://x", ["  user.created  ", " order.placed "], null);

        sub.GetEvents().ShouldBe(["user.created", "order.placed"]);
    }

    [Fact]
    public void GetEvents_Should_Drop_Blank_Entries()
    {
        // Empty/whitespace-only entries collapse to a single CSV with empty segments.
        var sub = WebhookSubscription.Create("https://x", ["user.created", "", "   ", "user.deleted"], null);

        sub.GetEvents().ShouldBe(["user.created", "user.deleted"]);
    }

    [Fact]
    public void GetEvents_Should_Return_Empty_When_All_Entries_Blank()
    {
        var sub = WebhookSubscription.Create("https://x", ["", "   "], null);

        sub.GetEvents().ShouldBeEmpty();
    }

    [Fact]
    public void MatchesEvent_Should_Match_After_Trimming_Subscribed_Event()
    {
        var sub = WebhookSubscription.Create("https://x", ["  user.created  "], null);

        sub.MatchesEvent("user.created").ShouldBeTrue();
    }

    #endregion
}
