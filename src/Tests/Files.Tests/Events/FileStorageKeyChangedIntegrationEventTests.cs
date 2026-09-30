using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Contracts.v1.DTOs;

namespace Files.Tests.Events;

public class FileStorageKeyChangedIntegrationEventTests
{
    private const string OldKey = "tenants/t/user/2026/05/abc/avatar.png";
    private const string NewKey = "public/tenants/t/user/2026/05/abc/avatar.png";

    private static FileStorageKeyChangedIntegrationEvent NewEvent() => new(
        Id: Guid.NewGuid(),
        OccurredOnUtc: DateTime.UtcNow,
        TenantId: "t",
        CorrelationId: "c",
        Source: "Files",
        FileAssetId: Guid.NewGuid(),
        OwnerType: "User",
        OwnerId: Guid.NewGuid(),
        OldStorageKey: OldKey,
        NewStorageKey: NewKey,
        Visibility: Visibility.Public);

    [Theory]
    // Whatever base the URL was built with (PublicBaseUrl, path-style endpoint, CloudFront, local path) is kept.
    [InlineData("http://localhost:9000/fsh-uploads/" + OldKey, "http://localhost:9000/fsh-uploads/" + NewKey)]
    [InlineData("https://d111.cloudfront.net/" + OldKey, "https://d111.cloudfront.net/" + NewKey)]
    [InlineData("/" + OldKey, "/" + NewKey)]
    [InlineData(OldKey, NewKey)]
    public void RewriteUrl_Should_PointTheUrlAtTheNewKey_When_ItAddressesTheOldKey(string url, string expected)
    {
        NewEvent().RewriteUrl(url).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://cdn.example.com/logo.svg")]
    [InlineData("https://cdn.example.com/x" + OldKey)]
    public void RewriteUrl_Should_ReturnNull_When_TheUrlIsForAnotherObject(string? url)
    {
        NewEvent().RewriteUrl(url).ShouldBeNull();
        (NewEvent() with { NewPublicUrl = "https://s3.example.com/fsh/" + NewKey }).ResolveUrl(url).ShouldBeNull();
    }

    [Fact]
    public void ResolveUrl_Should_UseTheCurrentPublicUrl_When_TheStoredUrlHasAnOutdatedBase()
    {
        // Compose stored http://rustfs:9000/... before PublicBaseUrl existed; browsers can't reach that host.
        var @event = NewEvent() with { NewPublicUrl = "https://s3.example.com/fsh/" + NewKey };

        @event.ResolveUrl("http://rustfs:9000/fsh/" + OldKey).ShouldBe("https://s3.example.com/fsh/" + NewKey);
    }

    [Fact]
    public void ResolveUrl_Should_FallBackToTheRewrite_When_TheFileIsNotPublic()
    {
        var @event = NewEvent() with { Visibility = Visibility.Private, NewPublicUrl = null };

        @event.ResolveUrl("http://rustfs:9000/fsh/" + OldKey).ShouldBe("http://rustfs:9000/fsh/" + NewKey);
    }
}
