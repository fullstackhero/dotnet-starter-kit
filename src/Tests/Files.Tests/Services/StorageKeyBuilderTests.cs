using FSH.Modules.Files.Services;
using Shouldly;

namespace Files.Tests.Services;

public class StorageKeyBuilderTests
{
    [Fact]
    public void Build_Should_ProduceCanonicalShape()
    {
        var now = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var key = StorageKeyBuilder.Build("tenant-a", "Product", id, "shoe photo.png", now);

        key.ShouldBe("tenants/tenant-a/product/2026/05/11111111222233334444555555555555/shoe_photo.png");
    }

    [Fact]
    public void Build_Should_LowercaseOwnerType()
    {
        var key = StorageKeyBuilder.Build("t", "TicketComment", Guid.NewGuid(), "x.pdf", DateTimeOffset.UtcNow);
        key.ShouldContain("/ticketcomment/");
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
            () => StorageKeyBuilder.Build("t", "o", Guid.NewGuid(), fileName, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Build_Should_RejectEmptyTenantId()
    {
        Should.Throw<ArgumentException>(
            () => StorageKeyBuilder.Build("", "Product", Guid.NewGuid(), "x.png", DateTimeOffset.UtcNow));
    }
}
