using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Persistence;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Proof tests for the jsonb ILIKE regression: <c>EF.Functions.ILike</c> was applied to the
/// <c>PayloadJson</c> column, which maps to PostgreSQL <c>jsonb</c>. PostgreSQL has no
/// <c>like_escape(jsonb, unknown)</c>, so every payload-backed filter crashed with HTTP 500
/// (and the GetAudits OR-search was fully broken because the jsonb leg poisoned the whole OR).
///
/// Each test seeds an AuditRecord with a known payload, then drives the real endpoint with a
/// filter and asserts the row comes back (no 500). All payloads carry a unique correlation id so
/// the assertions are isolated from audit rows written by the background worker.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class AuditPayloadFilterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        // API serializes enums as string names (global JsonStringEnumConverter); read them back.
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AuditPayloadFilterTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region Happy Path

    [Fact]
    public async Task GetSecurityAudits_Should_ReturnMatchingRow_When_FilteredByActionInJsonbPayload()
    {
        // Arrange
        var correlationId = NewCorrelationId();
        var serializer = new SystemTextJsonAuditSerializer();
        var payload = serializer.SerializePayload(new SecurityEventPayload(
            SecurityAction.PasswordChanged, "subject-1", "client-1", "Password", null, null));
        await SeedAuditRecordAsync(AuditEventType.Security, AuditSeverity.Information, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/security?action={SecurityAction.PasswordChanged}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await ReadListAsync(response);
        rows.ShouldContain(r => r.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetExceptionAudits_Should_ReturnMatchingRow_When_FilteredByAreaInJsonbPayload()
    {
        // Arrange
        var correlationId = NewCorrelationId();
        var payload = SerializeException(ExceptionArea.Worker, "System.TimeoutException", "/jobs/run");
        await SeedAuditRecordAsync(AuditEventType.Exception, AuditSeverity.Error, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/exceptions?area={ExceptionArea.Worker}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await ReadListAsync(response);
        rows.ShouldContain(r => r.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetExceptionAudits_Should_ReturnMatchingRow_When_FilteredByExceptionTypeInJsonbPayload()
    {
        // Arrange
        var correlationId = NewCorrelationId();
        var exceptionType = $"FSH.Test.WidgetException_{Guid.NewGuid():N}";
        var payload = SerializeException(ExceptionArea.Api, exceptionType, "/api/v1/widgets");
        await SeedAuditRecordAsync(AuditEventType.Exception, AuditSeverity.Error, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/exceptions?exceptionType={Uri.EscapeDataString(exceptionType)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await ReadListAsync(response);
        rows.ShouldContain(r => r.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetExceptionAudits_Should_ReturnMatchingRow_When_FilteredByRouteOrLocationInJsonbPayload()
    {
        // Arrange
        var correlationId = NewCorrelationId();
        var route = $"/api/v1/proof/{Guid.NewGuid():N}";
        var payload = SerializeException(ExceptionArea.Api, "System.InvalidOperationException", route);
        await SeedAuditRecordAsync(AuditEventType.Exception, AuditSeverity.Error, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/exceptions?routeOrLocation={Uri.EscapeDataString(route)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await ReadListAsync(response);
        rows.ShouldContain(r => r.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetAudits_Should_ReturnMatchingRow_When_SearchTermOnlyAppearsInsideJsonbPayload()
    {
        // Arrange — the search term lives ONLY in the jsonb payload (not Source/UserName),
        // so it exercises the jsonb leg of the OR that was previously poisoning the query.
        var correlationId = NewCorrelationId();
        var marker = $"needle{Guid.NewGuid():N}";
        var payload = SerializeException(ExceptionArea.Api, "System.Exception", $"/{marker}/endpoint");
        await SeedAuditRecordAsync(AuditEventType.Exception, AuditSeverity.Error, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=50&search={Uri.EscapeDataString(marker)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paged = await ReadPagedAsync(response);
        paged.Items.ShouldContain(r => r.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetAudits_Should_DriveSearchOverRealAsyncWrittenAudit_When_LoggingIn()
    {
        // Arrange — full async write path: a login produces a Security audit via the background
        // channel worker, then GetAudits?search= filters its jsonb payload (end-to-end fix proof).
        await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();

        // Act / Assert — poll until a Security event materializes under the search filter.
        PagedResult<AuditSummaryDto>? paged = null;
        for (int i = 0; i < 40; i++)
        {
            var response = await client.GetAsync(
                $"{TestConstants.AuditsBasePath}?pageNumber=1&pageSize=50&search={Uri.EscapeDataString("LoginSucceeded")}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            paged = await ReadPagedAsync(response);
            if (paged.Items.Any(r => r.EventType == AuditEventType.Security))
            {
                break;
            }

            await Task.Delay(500);
        }

        paged.ShouldNotBeNull();
        paged!.Items.ShouldContain(r => r.EventType == AuditEventType.Security);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetExceptionAudits_Should_ReturnNoSeededRow_When_AreaFilterDoesNotMatch()
    {
        // Arrange — payload area is Worker; filtering by Ui must not return it, and must not 500.
        var correlationId = NewCorrelationId();
        var payload = SerializeException(ExceptionArea.Worker, "System.TimeoutException", "/jobs/x");
        await SeedAuditRecordAsync(AuditEventType.Exception, AuditSeverity.Error, correlationId, payload);

        using var client = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await client.GetAsync(
            $"{TestConstants.AuditsBasePath}/exceptions?area={ExceptionArea.Ui}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await ReadListAsync(response);
        rows.ShouldNotContain(r => r.CorrelationId == correlationId);
    }

    #endregion

    #region Helpers

    private static string NewCorrelationId() => $"proof-{Guid.NewGuid():N}";

    private static string SerializeException(ExceptionArea area, string exceptionType, string routeOrLocation)
    {
        var serializer = new SystemTextJsonAuditSerializer();
        return serializer.SerializePayload(new ExceptionEventPayload(
            area, exceptionType, "proof message", new[] { "frame0" }, null, routeOrLocation));
    }

    /// <summary>
    /// Inserts a single AuditRecord for the root tenant straight into Postgres. The Finbuckle
    /// tenant context is set INLINE in this method (AsyncLocal gotcha: setting it elsewhere does
    /// not flow into the EF call's scope and the tenant filter NREs).
    /// </summary>
    private async Task SeedAuditRecordAsync(
        AuditEventType eventType, AuditSeverity severity, string correlationId, string payloadJson)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var store = sp.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var rootTenant = await store.GetAsync(MultitenancyConstants.Root.Id);
        rootTenant.ShouldNotBeNull();

        sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(rootTenant);

        var db = sp.GetRequiredService<AuditDbContext>();
        db.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            EventType = (int)eventType,
            Severity = (byte)severity,
            TenantId = MultitenancyConstants.Root.Id,
            UserId = "proof-user",
            UserName = "proof-user",
            CorrelationId = correlationId,
            Source = "proof",
            Tags = 0,
            PayloadJson = payloadJson
        });

        await db.SaveChangesAsync();
    }

    private static async Task<IReadOnlyList<AuditSummaryDto>> ReadListAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AuditSummaryDto>>(json, JsonOptions) ?? [];
    }

    private static async Task<PagedResult<AuditSummaryDto>> ReadPagedAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<AuditSummaryDto>>(json, JsonOptions)
            ?? new PagedResult<AuditSummaryDto>();
    }

    #endregion
}
