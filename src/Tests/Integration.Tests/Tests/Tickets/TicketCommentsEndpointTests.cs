using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Exercises the ticket comment flow end-to-end: AddTicketComment +
/// ListTicketComments, plus the CommentCount roll-up surfaced through
/// GetTicketById. The add-comment write path was previously broken by a
/// missing <c>ValueGeneratedNever()</c> on TicketComment.Id (EF tracked the
/// nav-collection child as Modified, not Added, → DbUpdateConcurrencyException).
/// These tests assert the fixed behaviour.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketCommentsEndpointTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TicketCommentsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task AddComment_Should_Persist_And_BumpCommentCount_When_TicketExists()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client, UniqueTitle("Commentable"));
        const string body = "First comment from integration test.";
        #endregion

        #region Act
        var commentResponse = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments",
            new { body });
        #endregion

        #region Assert
        commentResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var commentId = await commentResponse.DeserializeAsync<Guid>();
        commentId.ShouldNotBe(Guid.Empty);

        var comments = await ListCommentsAsync(client, ticketId);
        comments.Length.ShouldBe(1);
        comments[0].Id.ShouldBe(commentId);
        comments[0].Body.ShouldBe(body);
        comments[0].TicketId.ShouldBe(ticketId);
        comments[0].AuthorUserId.ShouldNotBe(Guid.Empty);

        var fetched = await GetTicketAsync(client, ticketId);
        fetched.CommentCount.ShouldBe(1);
        #endregion
    }

    [Fact]
    public async Task ListComments_Should_ReturnInChronologicalOrder_When_MultipleComments()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client, UniqueTitle("MultiComment"));
        #endregion

        #region Act
        await AddCommentAsync(client, ticketId, "first");
        await AddCommentAsync(client, ticketId, "second");
        await AddCommentAsync(client, ticketId, "third");
        #endregion

        #region Assert
        var comments = await ListCommentsAsync(client, ticketId);
        comments.Length.ShouldBe(3);
        comments.Select(c => c.Body).ShouldBe(["first", "second", "third"]);
        // CreatedAtUtc must be non-decreasing — the handler orders by it.
        comments[0].CreatedAtUtc.ShouldBeLessThanOrEqualTo(comments[1].CreatedAtUtc);
        comments[1].CreatedAtUtc.ShouldBeLessThanOrEqualTo(comments[2].CreatedAtUtc);

        var fetched = await GetTicketAsync(client, ticketId);
        fetched.CommentCount.ShouldBe(3);
        #endregion
    }

    [Fact]
    public async Task ListComments_Should_ReturnEmpty_When_TicketHasNoComments()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client, UniqueTitle("NoComments"));
        #endregion

        #region Act
        var comments = await ListCommentsAsync(client, ticketId);
        #endregion

        #region Assert
        comments.ShouldBeEmpty();
        #endregion
    }

    [Fact]
    public async Task AddComment_Should_BeAccepted_When_TicketIsResolved()
    {
        // AddComment only rejects Closed tickets; Resolved still accepts comments.
        // No API path reaches Closed, so this covers the non-throwing branch.
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client, UniqueTitle("ResolvedComment"));
        await ResolveAsync(client, ticketId, "done");
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments",
            new { body = "follow-up after resolution" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var comments = await ListCommentsAsync(client, ticketId);
        comments.Length.ShouldBe(1);
        #endregion
    }

    // ─── validation / errors ─────────────────────────────────────────

    [Fact]
    public async Task AddComment_Should_Return400_When_BodyIsEmpty()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateTicketAsync(client, UniqueTitle("EmptyBody"));
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments",
            new { body = "" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        #endregion
    }

    [Fact]
    public async Task AddComment_Should_Return404_When_TicketDoesNotExist()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{Guid.NewGuid()}/comments",
            new { body = "comment on a ghost" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        #endregion
    }

    [Fact]
    public async Task AddComment_Should_Return401_When_Unauthenticated()
    {
        #region Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{Guid.NewGuid()}/comments",
            new { body = "unauthenticated comment" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        #endregion
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueTitle(string prefix) =>
        $"Ticket-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateTicketAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title,
            description = (string?)null,
            priority = "Medium",
            assignedToUserId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task AddCommentAsync(HttpClient client, Guid ticketId, string body)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments",
            new { body });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TicketCommentDto[]> ListCommentsAsync(HttpClient client, Guid ticketId)
    {
        var response = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments");
        return await response.DeserializeAsync<TicketCommentDto[]>();
    }

    private static async Task<TicketDto> GetTicketAsync(HttpClient client, Guid ticketId)
    {
        var response = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        response.EnsureSuccessStatusCode();
        return await response.DeserializeAsync<TicketDto>();
    }

    private static async Task ResolveAsync(HttpClient client, Guid ticketId, string resolutionNote)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/resolve",
            new { resolutionNote });
        response.EnsureSuccessStatusCode();
    }
}
