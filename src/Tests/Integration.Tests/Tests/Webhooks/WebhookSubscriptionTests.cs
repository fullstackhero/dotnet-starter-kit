using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Webhooks;

[Collection(FshCollectionDefinition.Name)]
public sealed class WebhookSubscriptionTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public WebhookSubscriptionTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CreateWebhookSubscription_Should_ReturnCreated_When_DataIsValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = "https://example.com/webhook",
                events = new[] { "user.created", "user.updated" },
                secret = "test-webhook-secret-123"
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetWebhookSubscriptions_Should_ReturnOk_When_SubscriptionsExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        // Create a subscription first
        await client.PostAsJsonAsync($"{TestConstants.WebhooksBasePath}/subscriptions", new
        {
            url = "https://example.com/webhook-list",
            events = new[] { "tenant.created" },
            secret = "list-secret-123"
        });

        var response = await client.GetAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions?pageNumber=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteWebhookSubscription_Should_ReturnNoContent_When_SubscriptionExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var createResponse = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = "https://example.com/webhook-delete",
                events = new[] { "user.deleted" },
                secret = "delete-secret-123"
            });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var subscriptionId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await client.DeleteAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions/{subscriptionId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateWebhookSubscription_Should_Return401_When_NotAuthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.WebhooksBasePath}/subscriptions", new
            {
                url = "https://example.com/noauth-webhook",
                events = new[] { "user.created" },
                secret = "noauth-secret"
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
