using System.Text.Json;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Framework.Shared.Persistence;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// Shared polling helpers for the Auditing query-coverage tests.
///
/// Audit writes are asynchronous: the request thread publishes an
/// <c>AuditEnvelope</c> onto an in-memory channel and a background worker
/// drains it into the <c>AuditRecords</c> table. Reads therefore have to
/// poll until the row materializes, otherwise the tests are flaky.
///
/// Correlation/trace ids are NOT caller-controllable — they are derived from
/// <c>HttpContext.TraceIdentifier</c> / <c>Activity.Current</c> per request.
/// So the reliable strategy is: perform an auditable action, poll the list
/// endpoint until a fresh row appears, then read its real Id / CorrelationId
/// / TraceId and query the by-id / by-correlation / by-trace endpoints with
/// those concrete values.
/// </summary>
internal static class AuditTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public const int DefaultPollAttempts = 40;
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Polls the paged list endpoint until <paramref name="predicate"/> matches
    /// at least one row, returning the first match. Throws if nothing matches
    /// within the budget so a missing audit surfaces as a clear failure.
    /// </summary>
    public static async Task<AuditSummaryDto> PollForAuditAsync(
        HttpClient client,
        Func<AuditSummaryDto, bool> predicate,
        int attempts = DefaultPollAttempts)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(predicate);

        for (int i = 0; i < attempts; i++)
        {
            var page = await GetAuditsPageAsync(client, pageSize: 100).ConfigureAwait(false);
            var match = page.Items.FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(PollInterval).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"No audit row matched the predicate within {attempts} attempts.");
    }

    public static async Task<PagedResponse<AuditSummaryDto>> GetAuditsPageAsync(
        HttpClient client, int pageNumber = 1, int pageSize = 100, string? extraQuery = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        string url = $"{TestConstants.AuditsBasePath}?pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(extraQuery))
        {
            url += $"&{extraQuery}";
        }

        var response = await client.GetAsync(url).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<PagedResponse<AuditSummaryDto>>(json, JsonOptions)!;
    }

    public static async Task<IReadOnlyList<AuditSummaryDto>> GetListAsync(HttpClient client, string relativeUrl)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}{relativeUrl}").ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<AuditSummaryDto>>(json, JsonOptions)!;
    }

    public static async Task<AuditDetailDto?> GetByIdAsync(HttpClient client, Guid id)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.GetAsync($"{TestConstants.AuditsBasePath}/{id}").ConfigureAwait(false);
        if (await IsNotFoundAsync(response).ConfigureAwait(false))
        {
            return null;
        }

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<AuditDetailDto>(json, JsonOptions)!;
    }

    /// <summary>
    /// True when the by-id endpoint reported the record as missing.
    ///
    /// The handler raises <c>KeyNotFoundException</c>, which the production
    /// <c>GlobalExceptionHandler</c> maps to 404. The integration-test host
    /// swaps in <c>DetailedTestExceptionHandler</c> (shared infra we must not
    /// edit) which only special-cases ValidationException / CustomException, so
    /// KeyNotFoundException surfaces as 500 with the exception type in the body.
    /// We accept either rendering so the test asserts the genuine not-found
    /// throw path rather than an infra quirk.
    /// </summary>
    public static async Task<bool> IsNotFoundAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return body.Contains("KeyNotFoundException", StringComparison.Ordinal)
                   || body.Contains("not found", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Triggers an auditable HTTP activity and returns a real audit row that
    /// carries a populated CorrelationId + TraceId, so by-correlation/by-trace
    /// queries have concrete values to look up.
    /// </summary>
    public static async Task<AuditSummaryDto> GenerateActivityAuditAsync(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        // The summary endpoint is cheap and always audited as an Activity.
        var marker = await client.GetAsync($"{TestConstants.AuditsBasePath}/summary").ConfigureAwait(false);
        marker.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await PollForAuditAsync(
            client,
            a => a.EventType == AuditEventType.Activity
                 && !string.IsNullOrEmpty(a.CorrelationId)
                 && !string.IsNullOrEmpty(a.TraceId))
            .ConfigureAwait(false);
    }
}
