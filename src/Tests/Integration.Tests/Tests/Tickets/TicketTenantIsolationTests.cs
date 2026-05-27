using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

/// <summary>
/// Cross-TENANT isolation for the tickets module. Proves a ticket created in
/// tenant A (root) is invisible to tenant B: B cannot fetch it, list it, or
/// mutate it (assign). Every cross-tenant access returns 404, never a leak.
/// The TicketsDbContext gets tenant isolation via BaseDbContext's auto-apply,
/// so these assert intended behavior. Intra-tenant lifecycle / state-machine
/// coverage lives in <see cref="TicketsEndpointTests"/>.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TicketTenantIsolationTests
{
    private readonly AuthHelper _auth;

    public TicketTenantIsolationTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetTicketById_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange — tenant A (root) creates a ticket; tenant B is freshly provisioned.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"ticket-get-{uniqueId}");

        var ticketId = await CreateTicketAsync(rootClient, $"Ticket-RootOnly-{uniqueId}");

        // Act — tenant B tries to fetch tenant A's ticket.
        using var crossGet = await otherClient.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}");

        // Assert — clean 404, never tenant A's data.
        crossGet.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A still sees its own ticket.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchTickets_Should_NotReturn_OtherTenants_Tickets()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"ticket-list-{uniqueId}");

        var rootTitle = $"Ticket-RootOnly-{uniqueId}";
        var ticketId = await CreateTicketAsync(rootClient, rootTitle);

        // Act — tenant B lists tickets.
        using var listResponse = await otherClient.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets?pageNumber=1&pageSize=200");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await listResponse.DeserializeAsync<PagedResult<TicketDto>>();
        var body = await otherClient.GetStringAsync(
            $"{TestConstants.TicketsBasePath}/tickets?pageNumber=1&pageSize=200");

        // Assert — tenant A's ticket never appears in tenant B's listing.
        page.Items.ShouldNotContain(t => t.Id == ticketId,
            "tenant B's ticket list must not include tenant A's ticket");
        body.ShouldNotContain(rootTitle);
    }

    [Fact]
    public async Task AssignTicket_Should_Return404_When_OwnedByDifferentTenant()
    {
        // Arrange.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        using var otherClient = await ProvisionTenantClientAsync(rootClient, $"ticket-mut-{uniqueId}");

        var ticketId = await CreateTicketAsync(rootClient, $"Ticket-Mutate-{uniqueId}");

        // Act — tenant B tries to mutate (assign) tenant A's ticket.
        using var crossAssign = await otherClient.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/assign",
            new { assigneeUserId = Guid.NewGuid() });

        // Assert — 404: the mutation never reaches tenant A's row.
        crossAssign.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Sanity: tenant A's ticket is untouched — still Open, no assignee.
        using var ownGet = await rootClient.GetAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        ownGet.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await ownGet.DeserializeAsync<TicketDto>();
        fetched.Status.ShouldBe("Open");
        fetched.AssignedToUserId.ShouldBeNull();
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<Guid> CreateTicketAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title,
            description = (string?)null,
            priority = "Medium",
            assignedToUserId = (Guid?)null,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"setup failed to create ticket: {await response.Content.ReadAsStringAsync()}");
        return await response.DeserializeAsync<Guid>();
    }

    private async Task<HttpClient> ProvisionTenantClientAsync(HttpClient rootClient, string tenantId)
    {
        var adminEmail = $"{tenantId}-admin@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await WaitForProvisioningAsync(rootClient, tenantId);
        return await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed: {content}");
                }
            }

            await Task.Delay(1000);
        }

        var finalResponse = await client.GetAsync(
            $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
        var finalContent = finalResponse.IsSuccessStatusCode
            ? await finalResponse.Content.ReadAsStringAsync()
            : $"HTTP {finalResponse.StatusCode}";

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. Last status: {finalContent}");
    }
}
