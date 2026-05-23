using System.Reflection;
using System.Security.Cryptography;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Jobs;
using Hangfire;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Coverage for the two Hangfire purge jobs. They are NOT run by the scheduler in tests, so we
/// resolve each from a DI scope (with the Finbuckle tenant context set INLINE in that scope to
/// avoid the AsyncLocal NRE in the tenant filter) and invoke <c>RunAsync</c> directly after seeding
/// a purgeable precondition. The real S3/MinIO RemoveAsync runs against bytes that were genuinely
/// PUT, so this also exercises S3StorageService.RemoveAsync (DeleteObject) end-to-end.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class PurgeJobsTests
{
    private const string FilesBasePath = "/api/v1/files";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public PurgeJobsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region PurgeDeletedFilesJob

    [Fact]
    public async Task PurgeDeletedFilesJob_Should_HardDelete_Row_And_Remove_Bytes_When_Past_Retention()
    {
        // Arrange — upload + finalize a real object, soft-delete it, then backdate DeletedOnUtc
        // past the retention window so the daily purge picks it up.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (id, storageKey) = await UploadFinalizeAndDeleteAsync(client, "purge-me.pdf", 256);

        await BackdateDeletedOnUtcAsync(id, DateTimeOffset.UtcNow.AddDays(-90));

        // Sanity: bytes are still in storage before the purge runs.
        (await ObjectExistsAsync(storageKey)).ShouldBeTrue("bytes should exist until the purge job runs");

        // Act — resolve and run the job directly.
        await RunJobAsync<PurgeDeletedFilesJob>(j => j.RunAsync(CancellationToken.None));

        // Assert — row is hard-deleted (even ignoring query filters) and bytes are gone.
        (await RowExistsIgnoringFiltersAsync(id)).ShouldBeFalse("soft-deleted row past retention must be hard-purged");
        (await ObjectExistsAsync(storageKey)).ShouldBeFalse("storage object must be removed by the purge job");
    }

    [Fact]
    public async Task PurgeDeletedFilesJob_Should_Leave_Recently_Deleted_File_Intact()
    {
        // A file soft-deleted just now (within retention) must survive the purge.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (id, _) = await UploadFinalizeAndDeleteAsync(client, "keep-me.pdf", 256);

        await RunJobAsync<PurgeDeletedFilesJob>(j => j.RunAsync(CancellationToken.None));

        (await RowExistsIgnoringFiltersAsync(id)).ShouldBeTrue("recently-deleted file is within retention and must survive");
    }

    [Fact]
    public void PurgeDeletedFilesJob_RunAsync_Should_Be_Annotated_With_AutomaticRetry()
    {
        var method = typeof(PurgeDeletedFilesJob).GetMethod(
            nameof(PurgeDeletedFilesJob.RunAsync), BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();
        method!.GetCustomAttribute<AutomaticRetryAttribute>().ShouldNotBeNull();
    }

    #endregion

    #region PurgeOrphanedFilesJob

    [Fact]
    public async Task PurgeOrphanedFilesJob_Should_HardDelete_Pending_Row_And_Remove_Bytes_When_Deadline_Passed()
    {
        // Arrange — request an upload URL and PUT bytes, but DON'T finalize. The row stays in
        // PendingUpload. Backdate UploadDeadline so the hourly orphan purge picks it up.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (id, storageKey) = await RequestUploadAndPutBytesAsync(client, "orphan.pdf", 256);

        (await ObjectExistsAsync(storageKey)).ShouldBeTrue("orphan bytes made it to storage");

        await BackdateUploadDeadlineAsync(id, DateTimeOffset.UtcNow.AddHours(-2));

        // Act
        await RunJobAsync<PurgeOrphanedFilesJob>(j => j.RunAsync(CancellationToken.None));

        // Assert — pending row gone, bytes removed.
        (await RowExistsIgnoringFiltersAsync(id)).ShouldBeFalse("expired PendingUpload row must be hard-purged");
        (await ObjectExistsAsync(storageKey)).ShouldBeFalse("orphan storage object must be removed");
    }

    [Fact]
    public async Task PurgeOrphanedFilesJob_Should_Leave_Pending_File_With_Future_Deadline_Intact()
    {
        // A freshly-requested upload (deadline in the future) must not be purged.
        using var client = await _auth.CreateRootAdminClientAsync();
        var (id, _) = await RequestUploadAndPutBytesAsync(client, "still-pending.pdf", 256);

        await RunJobAsync<PurgeOrphanedFilesJob>(j => j.RunAsync(CancellationToken.None));

        (await RowExistsIgnoringFiltersAsync(id)).ShouldBeTrue("pending file within its upload window must survive");
    }

    [Fact]
    public void PurgeOrphanedFilesJob_RunAsync_Should_Be_Annotated_With_AutomaticRetry()
    {
        var method = typeof(PurgeOrphanedFilesJob).GetMethod(
            nameof(PurgeOrphanedFilesJob.RunAsync), BindingFlags.Public | BindingFlags.Instance);
        method.ShouldNotBeNull();
        method!.GetCustomAttribute<AutomaticRetryAttribute>().ShouldNotBeNull();
    }

    #endregion

    // ─── helpers ─────────────────────────────────────────────────────

    private async Task RunJobAsync<TJob>(Func<TJob, Task> invoke) where TJob : notnull
    {
        using var scope = _factory.Services.CreateScope();
        SetTenantContext(scope);
        // The jobs are scheduled via IRecurringJobManager (Hangfire's JobActivator constructs them
        // through ActivatorUtilities) rather than registered in the container, so we mirror that
        // here instead of GetRequiredService.
        var job = ActivatorUtilities.CreateInstance<TJob>(scope.ServiceProvider);
        await invoke(job);
    }

    private static void SetTenantContext(IServiceScope scope)
    {
        // Set the Finbuckle tenant context INLINE in this scope — the FilesDbContext tenant filter
        // dereferences it, so a missing context surfaces as an NRE.
        var tenant = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId).GetAwaiter().GetResult();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
    }

    private async Task<bool> ObjectExistsAsync(string storageKey)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        return await storage.ExistsAsync(storageKey);
    }

    private async Task<bool> RowExistsIgnoringFiltersAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenantContext(scope);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        return await db.FileAssets.IgnoreQueryFilters().AnyAsync(f => f.Id == id);
    }

    private async Task BackdateDeletedOnUtcAsync(Guid id, DateTimeOffset when)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenantContext(scope);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.DeletedOnUtc, when));
    }

    private async Task BackdateUploadDeadlineAsync(Guid id, DateTimeOffset when)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenantContext(scope);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.UploadDeadline, when));
    }

    private async Task<(Guid Id, string StorageKey)> UploadFinalizeAndDeleteAsync(
        HttpClient client, string fileName, int sizeBytes)
    {
        var (id, storageKey) = await RequestUploadAndPutBytesAsync(client, fileName, sizeBytes);

        using var finalize = await client.PostAsync($"{FilesBasePath}/{id}/finalize", null);
        finalize.EnsureSuccessStatusCode();

        using var del = await client.DeleteAsync($"{FilesBasePath}/{id}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return (id, storageKey);
    }

    private async Task<(Guid Id, string StorageKey)> RequestUploadAndPutBytesAsync(
        HttpClient client, string fileName, int sizeBytes)
    {
        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName,
            contentType = "application/pdf",
            sizeBytes,
            visibility = 1,
            category = "Document",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var presigned = await response.DeserializeAsync<PresignedUploadResponse>();

        byte[] bytes = new byte[sizeBytes];
        RandomNumberGenerator.Fill(bytes);
        using var raw = new HttpClient();
        using var put = new HttpRequestMessage(HttpMethod.Put, presigned.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
            }
        };
        using var putResp = await raw.SendAsync(put);
        putResp.EnsureSuccessStatusCode();

        var storageKey = await ReadStorageKeyAsync(presigned.FileAssetId);
        return (presigned.FileAssetId, storageKey);
    }

    private async Task<string> ReadStorageKeyAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        SetTenantContext(scope);
        var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        var key = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Id == id)
            .Select(f => f.StorageKey)
            .FirstOrDefaultAsync();
        key.ShouldNotBeNull();
        return key!;
    }
}
