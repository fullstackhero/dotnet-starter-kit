using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Covers the by-id, by-correlation and by-trace read endpoints
/// (<c>GetAuditByIdQueryHandler</c>, <c>GetAuditsByCorrelationQueryHandler</c>,
/// <c>GetAuditsByTraceQueryHandler</c> and their validators). Audit writes are
/// async, so each happy-path test first polls the list endpoint for a real row
/// and then queries by its concrete Id / CorrelationId / TraceId.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditQueryByKeyTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditQueryByKeyTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region GetAuditById - Happy Path

    [Fact]
    public async Task GetAuditById_Should_ReturnFullDetail_When_RecordExists()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var seed = await AuditTestHelper.GenerateActivityAuditAsync(client);

        // Act
        var detail = await AuditTestHelper.GetByIdAsync(client, seed.Id);

        // Assert
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(seed.Id);
        detail.EventType.ShouldBe(seed.EventType);
        detail.TenantId.ShouldBe(TestConstants.RootTenantId);
        detail.OccurredAtUtc.ShouldBe(seed.OccurredAtUtc);
    }

    [Fact]
    public async Task GetAuditById_Should_IncludePayload_When_RecordExists()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var seed = await AuditTestHelper.GenerateActivityAuditAsync(client);

        // Act
        var detail = await AuditTestHelper.GetByIdAsync(client, seed.Id);

        // Assert — the handler parses PayloadJson into a JsonElement; an Activity
        // payload always carries the "kind" discriminator.
        detail.ShouldNotBeNull();
        detail.Payload.ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Object);
    }

    #endregion

    #region GetAuditById - Exception / Edge

    [Fact]
    public async Task GetAuditById_Should_ReportNotFound_When_RecordDoesNotExist()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}/{Guid.NewGuid()}");

        // Assert — handler raises KeyNotFoundException (404 in prod; rendered as
        // 500+KeyNotFoundException by the test exception handler — see helper).
        (await AuditTestHelper.IsNotFoundAsync(response)).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAuditById_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetAuditsByCorrelation

    [Fact]
    public async Task GetAuditsByCorrelation_Should_ReturnMatchingRows_When_CorrelationExists()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var seed = await AuditTestHelper.GenerateActivityAuditAsync(client);

        // Act
        var rows = await AuditTestHelper.GetListAsync(
            client, $"/by-correlation/{Uri.EscapeDataString(seed.CorrelationId!)}");

        // Assert
        rows.ShouldNotBeEmpty();
        rows.ShouldAllBe(r => r.CorrelationId == seed.CorrelationId);
        rows.ShouldContain(r => r.Id == seed.Id);
    }

    [Fact]
    public async Task GetAuditsByCorrelation_Should_ReturnEmpty_When_CorrelationUnknown()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var rows = await AuditTestHelper.GetListAsync(
            client, $"/by-correlation/{Guid.NewGuid():N}-unknown");

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAuditsByCorrelation_Should_HonorDateWindow_When_FromInFuture()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var seed = await AuditTestHelper.GenerateActivityAuditAsync(client);
        string future = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act — fromUtc beyond the row's OccurredAt excludes it.
        var rows = await AuditTestHelper.GetListAsync(
            client, $"/by-correlation/{Uri.EscapeDataString(seed.CorrelationId!)}?fromUtc={Uri.EscapeDataString(future)}");

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAuditsByCorrelation_Should_Return400_When_FromAfterTo()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        string from = DateTime.UtcNow.ToString("o");
        string to = DateTime.UtcNow.AddDays(-1).ToString("o");

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/by-correlation/abc?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetAuditsByTrace

    [Fact]
    public async Task GetAuditsByTrace_Should_ReturnMatchingRows_When_TraceExists()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        var seed = await AuditTestHelper.GenerateActivityAuditAsync(client);

        // Act
        var rows = await AuditTestHelper.GetListAsync(
            client, $"/by-trace/{Uri.EscapeDataString(seed.TraceId!)}");

        // Assert
        rows.ShouldNotBeEmpty();
        rows.ShouldAllBe(r => r.TraceId == seed.TraceId);
        rows.ShouldContain(r => r.Id == seed.Id);
    }

    [Fact]
    public async Task GetAuditsByTrace_Should_ReturnEmpty_When_TraceUnknown()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var rows = await AuditTestHelper.GetListAsync(
            client, $"/by-trace/{Guid.NewGuid():N}");

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAuditsByTrace_Should_Return400_When_FromAfterTo()
    {
        // Arrange
        using var client = await _auth.CreateRootAdminClientAsync();
        string from = DateTime.UtcNow.ToString("o");
        string to = DateTime.UtcNow.AddDays(-1).ToString("o");

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/by-trace/sometrace?fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}
