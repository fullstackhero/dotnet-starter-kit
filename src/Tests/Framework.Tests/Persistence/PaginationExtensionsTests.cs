using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;

namespace Framework.Tests.Persistence;

public sealed class PaginationExtensionsTests
{
    #region Test doubles

    private sealed class Item
    {
        public int Value { get; init; }
    }

    private sealed class PagedQuery : IPagedQuery
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? Sort { get; set; }
    }

    private static TestAsyncEnumerable<Item> Source(int count)
        => new(Enumerable.Range(1, count).Select(i => new Item { Value = i }));

    #endregion

    #region Happy Path

    [Fact]
    public async Task ToPagedResponseAsync_Should_ReturnRequestedPage_When_ValidParameters()
    {
        // Arrange
        var query = new PagedQuery { PageNumber = 2, PageSize = 10 };

        // Act
        var response = await Source(25).ToPagedResponseAsync(query);

        // Assert
        response.PageNumber.ShouldBe(2);
        response.PageSize.ShouldBe(10);
        response.TotalCount.ShouldBe(25);
        response.TotalPages.ShouldBe(3);
        response.Items.Count.ShouldBe(10);
        response.Items.First().Value.ShouldBe(11);
        response.HasNext.ShouldBeTrue();
        response.HasPrevious.ShouldBeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ToPagedResponseAsync_Should_DefaultPageSizeTo20_When_NotProvided()
    {
        // Arrange
        var query = new PagedQuery { PageNumber = null, PageSize = null };

        // Act
        var response = await Source(50).ToPagedResponseAsync(query);

        // Assert
        response.PageNumber.ShouldBe(1);
        response.PageSize.ShouldBe(20);
        response.Items.Count.ShouldBe(20);
    }

    [Fact]
    public async Task ToPagedResponseAsync_Should_NormalizeNonPositivePage_To1()
    {
        // Arrange
        var query = new PagedQuery { PageNumber = -5, PageSize = -3 };

        // Act
        var response = await Source(50).ToPagedResponseAsync(query);

        // Assert — page normalized to 1, size to default 20.
        response.PageNumber.ShouldBe(1);
        response.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task ToPagedResponseAsync_Should_CapPageSizeAt100_When_Exceeded()
    {
        // Arrange
        var query = new PagedQuery { PageNumber = 1, PageSize = 500 };

        // Act
        var response = await Source(120).ToPagedResponseAsync(query);

        // Assert
        response.PageSize.ShouldBe(100);
        response.Items.Count.ShouldBe(100);
    }

    [Fact]
    public async Task ToPagedResponseAsync_Should_ClampPageToLastPage_When_BeyondRange()
    {
        // Arrange — 25 items, size 10 => 3 pages; ask for page 99.
        var query = new PagedQuery { PageNumber = 99, PageSize = 10 };

        // Act
        var response = await Source(25).ToPagedResponseAsync(query);

        // Assert
        response.PageNumber.ShouldBe(3);
        response.Items.Count.ShouldBe(5);
        response.HasNext.ShouldBeFalse();
    }

    [Fact]
    public async Task ToPagedResponseAsync_Should_ReturnEmpty_When_NoItems()
    {
        // Arrange
        var query = new PagedQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var response = await Source(0).ToPagedResponseAsync(query);

        // Assert
        response.TotalCount.ShouldBe(0);
        response.TotalPages.ShouldBe(0);
        response.Items.ShouldBeEmpty();
        response.HasNext.ShouldBeFalse();
        response.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public async Task ToPagedResponseAsync_Should_Throw_When_SourceOrPaginationNull()
    {
        // Arrange
        IQueryable<Item> source = Source(1);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => source.ToPagedResponseAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() =>
            PaginationExtensions.ToPagedResponseAsync<Item>(null!, new PagedQuery()));
    }

    #endregion
}
