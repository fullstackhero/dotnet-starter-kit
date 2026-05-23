using FSH.Framework.Storage.Local;

namespace Framework.Tests.Storage;

public sealed class LocalPresignTokenStoreTests
{
    #region Happy Path

    [Fact]
    public void IssueThenConsume_Should_ReturnToken_When_NotExpired()
    {
        // Arrange
        var store = new LocalPresignTokenStore();

        // Act
        var token = store.Issue("uploads/probe/file.png", "image/png", 2048, TimeSpan.FromMinutes(5));
        var consumed = store.Consume(token);

        // Assert
        token.ShouldNotBeNullOrWhiteSpace();
        consumed.ShouldNotBeNull();
        consumed!.StorageKey.ShouldBe("uploads/probe/file.png");
        consumed.ContentType.ShouldBe("image/png");
        consumed.MaxBytes.ShouldBe(2048);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Consume_Should_ReturnNull_When_TokenAlreadyConsumed()
    {
        // Arrange — tokens are one-shot.
        var store = new LocalPresignTokenStore();
        var token = store.Issue("k", "text/plain", 1, TimeSpan.FromMinutes(5));

        // Act
        var first = store.Consume(token);
        var second = store.Consume(token);

        // Assert
        first.ShouldNotBeNull();
        second.ShouldBeNull();
    }

    [Fact]
    public void Consume_Should_ReturnNull_When_TokenUnknown()
    {
        // Arrange
        var store = new LocalPresignTokenStore();

        // Act & Assert
        store.Consume("does-not-exist").ShouldBeNull();
    }

    [Fact]
    public void Consume_Should_ReturnNull_When_TokenExpired()
    {
        // Arrange — negative ttl makes the token expire immediately.
        var store = new LocalPresignTokenStore();
        var token = store.Issue("k", "text/plain", 1, TimeSpan.FromMinutes(-5));

        // Act & Assert
        store.Consume(token).ShouldBeNull();
    }

    #endregion
}
