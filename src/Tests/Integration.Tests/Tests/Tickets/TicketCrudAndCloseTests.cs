using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Covers the lifecycle/CRUD operations added for the pre-release hardening pass:
/// Close (Resolved → Closed), Update (edit details), and Delete (soft-delete → trash → restore).
/// These honor the previously-dangling Tickets.Close/Update/Delete permissions and complete
/// the trash/restore round-trip that ListTrashed + Restore implied but had no entry point for.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketCrudAndCloseTests
{
    private readonly AuthHelper _auth;

    public TicketCrudAndCloseTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    // ─── close ───────────────────────────────────────────────────────

    [Fact]
    public async Task CloseTicket_Should_TransitionResolvedToClosed_And_StampClosedAt()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("CloseHappy"));
        await ResolveAsync(client, ticketId, "shipped");
        #endregion

        #region Act
        var response = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Closed");
        fetched.ClosedAtUtc.ShouldNotBeNull("Closing a ticket must stamp ClosedAtUtc.");
        #endregion
    }

    [Fact]
    public async Task CloseTicket_Should_Return409_When_TicketNotResolved()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("CloseOpen"));
        #endregion

        #region Act
        var response = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "Only a resolved ticket may be closed.");
        #endregion
    }

    [Fact]
    public async Task CloseTicket_Should_BeIdempotent_When_AlreadyClosed()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("CloseTwice"));
        await ResolveAsync(client, ticketId, "done");
        var first = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        #endregion

        #region Act
        var second = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        #endregion

        #region Assert
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Closed");
        #endregion
    }

    [Fact]
    public async Task ReopenTicket_Should_Succeed_After_Close()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("CloseThenReopen"));
        await ResolveAsync(client, ticketId, "done");
        await client.PostAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        #endregion

        #region Act
        var reopen = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/reopen", content: null);
        #endregion

        #region Assert
        reopen.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Open");
        fetched.ClosedAtUtc.ShouldBeNull("Reopening must clear the close stamp.");
        #endregion
    }

    // ─── update ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTicket_Should_EditTitleDescriptionAndPriority()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("BeforeEdit"), priority: "Low");
        var newTitle = UniqueTitle("AfterEdit");
        #endregion

        #region Act
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}",
            new { title = newTitle, description = "edited body", priority = "High" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Title.ShouldBe(newTitle);
        fetched.Description.ShouldBe("edited body");
        fetched.Priority.ShouldBe("High");
        #endregion
    }

    [Fact]
    public async Task UpdateTicket_Should_Return400_When_TitleEmpty()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("EditValidation"));
        #endregion

        #region Act
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}",
            new { title = "", description = (string?)null, priority = "Medium" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        #endregion
    }

    [Fact]
    public async Task UpdateTicket_Should_Return409_When_TicketClosed()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("EditClosed"));
        await ResolveAsync(client, ticketId, "done");
        await client.PostAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/close", content: null);
        #endregion

        #region Act
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}",
            new { title = UniqueTitle("NoEdit"), description = (string?)null, priority = "Medium" });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "A closed ticket is frozen until reopened.");
        #endregion
    }

    // ─── delete / restore round-trip ─────────────────────────────────

    [Fact]
    public async Task DeleteTicket_Should_SoftDelete_And_HideFromGet()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("DeleteHide"));
        #endregion

        #region Act
        var delete = await client.DeleteAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        var get = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        #endregion

        #region Assert
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Soft-deleted tickets must be hidden by the query filter.");
        #endregion
    }

    [Fact]
    public async Task DeleteTicket_Then_Restore_Should_BringItBack_WithComments()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("DeleteRestore"));
        await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments",
            new { body = "a comment that must survive the round-trip" });
        await client.DeleteAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        #endregion

        #region Act
        var restore = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/restore", content: null);
        #endregion

        #region Assert
        restore.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldNotBe("Closed");

        var comments = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments");
        comments.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Comments must survive a delete/restore round-trip (not cascade-hard-deleted).");
        #endregion
    }

    [Fact]
    public async Task DeleteTicket_Should_Return404_When_TicketDoesNotExist()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        #endregion

        #region Act
        var response = await client.DeleteAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{Guid.NewGuid()}");
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        #endregion
    }

    [Fact]
    public async Task ListTicketComments_Should_Return404_When_TicketDoesNotExist()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        #endregion

        #region Act
        // A missing ticket must 404 (not a misleading empty 200 that also leaks id existence).
        var response = await client.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{Guid.NewGuid()}/comments");
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        #endregion
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueTitle(string prefix) =>
        $"Ticket-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateAsync(
        HttpClient client,
        string title,
        string priority = "Medium",
        Guid? assignedToUserId = null)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title,
            description = (string?)null,
            priority,
            assignedToUserId,
        });
        response.EnsureSuccessStatusCode();
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<TicketDto> GetAsync(HttpClient client, Guid ticketId)
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
