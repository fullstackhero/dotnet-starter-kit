using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Services;
using Shouldly;

namespace Files.Tests.Services;

public class StorageKeyBuilderTests
{
    [Fact]
    public void Build_Should_ProduceCanonicalShape_UnderThePublicRoot_When_VisibilityIsPublic()
    {
        var now = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var key = StorageKeyBuilder.Build("tenant-a", "Product", id, "shoe photo.png", now, Visibility.Public);

        key.ShouldBe("public/tenants/tenant-a/product/2026/05/11111111222233334444555555555555/shoe_photo.png");
    }

    [Fact]
    public void Build_Should_ProduceCanonicalShape_UnderThePrivateRoot_When_VisibilityIsPrivate()
    {
        var now = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var key = StorageKeyBuilder.Build("tenant-a", "MyFiles", id, "notes.pdf", now, Visibility.Private);

        key.ShouldBe("private/tenants/tenant-a/myfiles/2026/05/11111111222233334444555555555555/notes.pdf");
    }

    [Fact]
    public void Build_Should_LowercaseOwnerType()
    {
        var key = StorageKeyBuilder.Build("t", "TicketComment", Guid.NewGuid(), "x.pdf", DateTimeOffset.UtcNow, Visibility.Private);
        key.ShouldContain("/ticketcomment/");
    }

    [Fact]
    public void Build_Should_RejectUnknownVisibility()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => StorageKeyBuilder.Build("t", "o", Guid.NewGuid(), "x.png", DateTimeOffset.UtcNow, (Visibility)42));
    }

    [Fact]
    public void Sanitize_Should_StripUnsafeCharacters()
    {
        StorageKeyBuilder.Sanitize("ke!llo$.png").ShouldBe("ke_llo_.png");
    }

    [Fact]
    public void Sanitize_Should_PreserveSafeCharacters()
    {
        StorageKeyBuilder.Sanitize("a-b_c.1.png").ShouldBe("a-b_c.1.png");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Build_Should_RejectEmptyFileName(string fileName)
    {
        Should.Throw<ArgumentException>(
            () => StorageKeyBuilder.Build("t", "o", Guid.NewGuid(), fileName, DateTimeOffset.UtcNow, Visibility.Public));
    }

    [Fact]
    public void Build_Should_RejectEmptyTenantId()
    {
        Should.Throw<ArgumentException>(
            () => StorageKeyBuilder.Build("", "Product", Guid.NewGuid(), "x.png", DateTimeOffset.UtcNow, Visibility.Public));
    }

    [Theory]
    // Legacy keys (written before visibility lived in the key) gain the root.
    [InlineData("tenants/t/product/2026/05/abc/x.png", Visibility.Public, "public/tenants/t/product/2026/05/abc/x.png")]
    [InlineData("tenants/t/product/2026/05/abc/x.png", Visibility.Private, "private/tenants/t/product/2026/05/abc/x.png")]
    // A key under one root moves to the other.
    [InlineData("public/tenants/t/myfiles/2026/05/abc/x.png", Visibility.Private, "private/tenants/t/myfiles/2026/05/abc/x.png")]
    [InlineData("private/tenants/t/myfiles/2026/05/abc/x.png", Visibility.Public, "public/tenants/t/myfiles/2026/05/abc/x.png")]
    // Already under the right root: unchanged.
    [InlineData("public/tenants/t/myfiles/2026/05/abc/x.png", Visibility.Public, "public/tenants/t/myfiles/2026/05/abc/x.png")]
    [InlineData("private/tenants/t/myfiles/2026/05/abc/x.png", Visibility.Private, "private/tenants/t/myfiles/2026/05/abc/x.png")]
    public void ForVisibility_Should_PlaceTheKeyUnderTheRootForTheVisibility(string key, Visibility visibility, string expected)
    {
        StorageKeyBuilder.ForVisibility(key, visibility).ShouldBe(expected);
    }

    [Fact]
    public void ForVisibility_Should_OnlyTreatAWholeLeadingSegmentAsTheRoot()
    {
        // "publicity/..." is not under "public/" — it must gain a root, not lose four characters.
        StorageKeyBuilder.ForVisibility("publicity/x.png", Visibility.Private).ShouldBe("private/publicity/x.png");
    }

    [Theory]
    [InlineData("public/tenants/t/x.png", Visibility.Public, true)]
    [InlineData("private/tenants/t/x.png", Visibility.Private, true)]
    [InlineData("private/tenants/t/x.png", Visibility.Public, false)]
    [InlineData("public/tenants/t/x.png", Visibility.Private, false)]
    [InlineData("tenants/t/x.png", Visibility.Public, false)]
    [InlineData("tenants/t/x.png", Visibility.Private, false)]
    public void IsUnderVisibilityRoot_Should_MatchOnlyTheRootForTheVisibility(string key, Visibility visibility, bool expected)
    {
        StorageKeyBuilder.IsUnderVisibilityRoot(key, visibility).ShouldBe(expected);
    }
}
