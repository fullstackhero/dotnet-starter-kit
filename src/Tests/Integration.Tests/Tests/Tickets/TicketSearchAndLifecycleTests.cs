using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Covers the SearchTickets filter/sort/paging branches and the remaining
/// lifecycle guards (idempotent resolve/reopen, illegal-transition 409s)
/// plus create-validation 400s — none of which the base suite exercised.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketSearchAndLifecycleTests
{
    private readonly AuthHelper _auth;

    public TicketSearchAndLifecycleTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    // ─── search filters ──────────────────────────────────────────────

    [Fact]
    public async Task SearchTickets_Should_FilterBy_Priority()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var lowId = await CreateAsync(client, UniqueTitle("LowPrio"), priority: "Low");
        var highId = await CreateAsync(client, UniqueTitle("HighPrio"), priority: "High");
        #endregion

        #region Act
        var page = await SearchAsync(client, "priority=Low&pageSize=200");
        #endregion

        #region Assert
        page.Items.ShouldContain(t => t.Id == lowId);
        page.Items.ShouldNotContain(t => t.Id == highId,
            "Priority=Low must not include High-priority tickets.");
        page.Items.ShouldAllBe(t => t.Priority == "Low");
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_FilterBy_AssignedToUserId()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var assignee = Guid.NewGuid();
        var assignedId = await CreateAsync(client, UniqueTitle("AssignedFilter"), assignedToUserId: assignee);
        var otherId = await CreateAsync(client, UniqueTitle("UnassignedFilter"));
        #endregion

        #region Act
        var page = await SearchAsync(client, $"assignedToUserId={assignee}&pageSize=200");
        #endregion

        #region Assert
        page.Items.ShouldContain(t => t.Id == assignedId);
        page.Items.ShouldNotContain(t => t.Id == otherId);
        page.Items.ShouldAllBe(t => t.AssignedToUserId == assignee);
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_FilterBy_ReporterUserId()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        // All tickets in this test share one reporter (the root admin). Create a
        // ticket and read back its reporter, then assert the filter returns it.
        var ticketId = await CreateAsync(client, UniqueTitle("ReporterFilter"));
        var created = await GetAsync(client, ticketId);
        var reporter = created.ReporterUserId;
        reporter.ShouldNotBe(Guid.Empty);
        #endregion

        #region Act
        var page = await SearchAsync(client, $"reporterUserId={reporter}&pageSize=200");
        var miss = await SearchAsync(client, $"reporterUserId={Guid.NewGuid()}&pageSize=200");
        #endregion

        #region Assert
        page.Items.ShouldContain(t => t.Id == ticketId);
        page.Items.ShouldAllBe(t => t.ReporterUserId == reporter);
        miss.Items.ShouldNotContain(t => t.Id == ticketId,
            "A random reporter id must match nothing we created.");
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_MatchOn_TitleSubstring_When_FreeTextSearch()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        // Embed a unique, unguessable token in the title so the ILIKE match
        // is isolated from every other ticket in the shared DB.
        var token = $"ztok{Guid.NewGuid().ToString("N")[..10]}";
        var matchId = await CreateAsync(client, $"Searchable {token} ticket");
        var nonMatchId = await CreateAsync(client, UniqueTitle("Unrelated"));
        #endregion

        #region Act
        var page = await SearchAsync(client, $"search={token}&pageSize=200");
        #endregion

        #region Assert
        page.Items.ShouldContain(t => t.Id == matchId);
        page.Items.ShouldNotContain(t => t.Id == nonMatchId);
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_MatchOn_Number_When_FreeTextSearch()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("ByNumber"));
        var created = await GetAsync(client, ticketId);
        created.Number.ShouldStartWith("TK-");
        #endregion

        #region Act
        // Search by the full ticket number — exercises the Number ILIKE branch.
        var page = await SearchAsync(client, $"search={created.Number}&pageSize=200");
        #endregion

        #region Assert
        page.Items.ShouldContain(t => t.Id == ticketId);
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_HonorSort_When_SortByTitleAscending()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var assignee = Guid.NewGuid(); // isolate this batch via a private assignee
        await CreateAsync(client, "MMM-sort-b", assignedToUserId: assignee);
        await CreateAsync(client, "MMM-sort-a", assignedToUserId: assignee);
        await CreateAsync(client, "MMM-sort-c", assignedToUserId: assignee);
        #endregion

        #region Act
        var page = await SearchAsync(client,
            $"assignedToUserId={assignee}&sortBy=title&sortDir=asc&pageSize=200");
        #endregion

        #region Assert
        var titles = page.Items.Select(t => t.Title).ToList();
        titles.ShouldBe(["MMM-sort-a", "MMM-sort-b", "MMM-sort-c"]);
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_HonorSort_When_SortByNumberDescending()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var assignee = Guid.NewGuid();
        var firstId = await CreateAsync(client, UniqueTitle("NumSortFirst"), assignedToUserId: assignee);
        var secondId = await CreateAsync(client, UniqueTitle("NumSortSecond"), assignedToUserId: assignee);
        #endregion

        #region Act
        // Number sort is lexicographic on the "TK-n" string; the later-created
        // ticket has the higher number, so it sorts first when descending.
        var page = await SearchAsync(client,
            $"assignedToUserId={assignee}&sortBy=number&sortDir=desc&pageSize=200");
        #endregion

        #region Assert
        var ids = page.Items.Select(t => t.Id).ToList();
        ids.ShouldContain(firstId);
        ids.ShouldContain(secondId);
        ids.IndexOf(secondId).ShouldBeLessThan(ids.IndexOf(firstId),
            "Descending Number sort must place the later (higher-numbered) ticket first.");
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_Paginate_When_PageSizeSmallerThanResultSet()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var assignee = Guid.NewGuid();
        for (int i = 0; i < 3; i++)
        {
            await CreateAsync(client, UniqueTitle($"Page{i}"), assignedToUserId: assignee);
        }
        #endregion

        #region Act
        var firstPage = await SearchAsync(client, $"assignedToUserId={assignee}&pageNumber=1&pageSize=2");
        var secondPage = await SearchAsync(client, $"assignedToUserId={assignee}&pageNumber=2&pageSize=2");
        #endregion

        #region Assert
        firstPage.PageNumber.ShouldBe(1);
        firstPage.PageSize.ShouldBe(2);
        firstPage.Items.Count.ShouldBe(2);
        firstPage.TotalCount.ShouldBe(3);
        firstPage.TotalPages.ShouldBe(2);

        secondPage.PageNumber.ShouldBe(2);
        secondPage.Items.Count.ShouldBe(1);

        // Pages must not overlap.
        var firstIds = firstPage.Items.Select(t => t.Id).ToHashSet();
        secondPage.Items.ShouldAllBe(t => !firstIds.Contains(t.Id));
        #endregion
    }

    [Fact]
    public async Task SearchTickets_Should_ClampOutOfRange_PagingArgs_To_Defaults()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var assignee = Guid.NewGuid();
        await CreateAsync(client, UniqueTitle("Clamp"), assignedToUserId: assignee);
        #endregion

        #region Act
        // pageNumber<1 clamps to 1; pageSize>200 clamps to the default of 20.
        var page = await SearchAsync(client, $"assignedToUserId={assignee}&pageNumber=0&pageSize=9999");
        #endregion

        #region Assert
        page.PageNumber.ShouldBe(1);
        page.PageSize.ShouldBe(20);
        page.Items.ShouldContain(t => t.AssignedToUserId == assignee);
        #endregion
    }

    // ─── lifecycle guards ────────────────────────────────────────────

    [Fact]
    public async Task AssignTicket_Should_Return409_When_TicketAlreadyResolved()
    {
        // Resolve closes the assign path: ThrowIfClosedOrResolved rejects it.
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("AssignResolved"));
        await ResolveAsync(client, ticketId, "fixed");
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/assign",
            new { assigneeUserId = Guid.NewGuid() });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        #endregion
    }

    [Fact]
    public async Task ResolveTicket_Should_BeIdempotent_When_AlreadyResolved()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("ResolveTwice"));
        await ResolveAsync(client, ticketId, "first note");
        #endregion

        #region Act
        // Second resolve is a no-op — the aggregate returns early. The first
        // note must survive (the second call must not overwrite it).
        var second = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/resolve",
            new { resolutionNote = "second note" });
        #endregion

        #region Assert
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Resolved");
        fetched.ResolutionNote.ShouldBe("first note");
        #endregion
    }

    [Fact]
    public async Task ReopenTicket_Should_BeNoop_When_TicketIsOpen()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("ReopenOpen"));
        #endregion

        #region Act
        var response = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/reopen", content: null);
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Open");
        #endregion
    }

    [Fact]
    public async Task ReopenTicket_Should_RestoreOpen_When_ResolvedTicketWasUnassigned()
    {
        // Reopen falls back to assignment state: unassigned → Open (the base
        // suite only covered the still-assigned → InProgress branch).
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var ticketId = await CreateAsync(client, UniqueTitle("ReopenUnassigned"));
        await ResolveAsync(client, ticketId, "shipped");
        #endregion

        #region Act
        var response = await client.PostAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/reopen", content: null);
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Open");
        fetched.AssignedToUserId.ShouldBeNull();
        fetched.ResolutionNote.ShouldBeNull();
        fetched.ResolvedAtUtc.ShouldBeNull();
        #endregion
    }

    // ─── create validation ───────────────────────────────────────────

    [Fact]
    public async Task CreateTicket_Should_Return400_When_TitleIsEmpty()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = "",
            priority = "Medium",
        });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        #endregion
    }

    [Fact]
    public async Task CreateTicket_Should_Return400_When_TitleExceedsMaxLength()
    {
        #region Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var tooLong = new string('x', 161); // limit is 160
        #endregion

        #region Act
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = tooLong,
            priority = "Medium",
        });
        #endregion

        #region Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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

    private static async Task<PagedResult<TicketDto>> SearchAsync(HttpClient client, string queryString)
    {
        var response = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets?{queryString}");
        return await response.DeserializeAsync<PagedResult<TicketDto>>();
    }

    private static async Task ResolveAsync(HttpClient client, Guid ticketId, string resolutionNote)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/resolve",
            new { resolutionNote });
        response.EnsureSuccessStatusCode();
    }
}
