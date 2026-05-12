# Files Module — Phase A Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the Files module skeleton + Storage block presigning extensions + integration test infrastructure so the kit has a working, tested file-upload API. No owning-feature wiring (Catalog/Tickets), no avatar/logo migration, no UI integration — those are Phases B–F per the spec.

**Architecture:** New `Modules/Files/Modules.Files` (+ `.Contracts`) peer to Catalog/Tickets. `FileAsset` aggregate, `FilesDbContext`, pre-signed PUT upload flow against MinIO (S3 in prod). `BuildingBlocks/Storage` grows three methods (`GenerateUploadUrlAsync`, `GenerateDownloadUrlAsync`, `HeadObjectAsync`); existing surface preserved. `IFileAccessPolicy` registry for per-`OwnerType` authorization; `IFileScanner` no-op default. Soft-delete with named SoftDelete filter; two Hangfire jobs for orphan + retention purges.

**Tech Stack:** .NET 10, EF Core 10 + Postgres, Finbuckle.MultiTenant, Mediator 3.0, FluentValidation 12, AWSSDK.S3 (MinIO-compatible), Hangfire, xUnit + Shouldly + Testcontainers MinIO.

**Spec reference:** `docs/superpowers/specs/2026-05-12-files-module-design.md`

**Module order:** Files = **350** (after Auditing=300, before Webhooks=400). Owning modules (Catalog=600, Tickets=700) register `IFileAccessPolicy` implementations in their `ConfigureServices` — execution order doesn't matter for DI registration since all `ConfigureServices` run before the service provider is built.

---

## Task 1 — Scaffold Files module projects

**Files:**
- Create: `src/Modules/Files/Modules.Files/Modules.Files.csproj`
- Create: `src/Modules/Files/Modules.Files.Contracts/Modules.Files.Contracts.csproj`
- Modify: `src/FSH.Starter.slnx` — add the two projects under a new `/Modules/Files/` folder
- Modify: `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` — reference both new projects

- [ ] **Step 1.1: Create Modules.Files.Contracts.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>FSH.Modules.Files.Contracts</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Mediator.Abstractions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Shared\Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 1.2: Create Modules.Files.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>FSH.Modules.Files</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Hangfire.Core" />
    <PackageReference Include="Mediator.Abstractions" />
    <PackageReference Include="Mediator.SourceGenerator" PrivateAssets="all" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Modules.Files.Contracts\Modules.Files.Contracts.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Core\Core.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Persistence\Persistence.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Storage\Storage.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Web\Web.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Jobs\Jobs.csproj" />
    <ProjectReference Include="..\..\..\Modules\Identity\Modules.Identity.Contracts\Modules.Identity.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 1.3: Add to solution and Api references**

Insert into `src/FSH.Starter.slnx` after the Catalog folder block:

```xml
<Folder Name="/Modules/Files/">
  <Project Path="Modules/Files/Modules.Files.Contracts/Modules.Files.Contracts.csproj" />
  <Project Path="Modules/Files/Modules.Files/Modules.Files.csproj" />
</Folder>
```

Add to `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` ItemGroup with module references:

```xml
<ProjectReference Include="..\..\Modules\Files\Modules.Files\Modules.Files.csproj" />
<ProjectReference Include="..\..\Modules\Files\Modules.Files.Contracts\Modules.Files.Contracts.csproj" />
```

- [ ] **Step 1.4: Build to verify empty projects compile**

Run: `dotnet build src/FSH.Starter.slnx --nologo`
Expected: Build succeeds, 2 new projects compiled (Modules.Files + Modules.Files.Contracts).

- [ ] **Step 1.5: Commit**

```
git add src/Modules/Files src/FSH.Starter.slnx src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj
git commit -m "feat(files): scaffold Modules.Files + .Contracts projects"
```

---

## Task 2 — Shared Storage DTOs

**Files:**
- Create: `src/BuildingBlocks/Shared/Storage/PresignedUploadUrl.cs`
- Create: `src/BuildingBlocks/Shared/Storage/StoredObjectMetadata.cs`

- [ ] **Step 2.1: Create PresignedUploadUrl.cs**

```csharp
namespace FSH.Framework.Shared.Storage;

public sealed record PresignedUploadUrl(
    Uri Url,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);
```

- [ ] **Step 2.2: Create StoredObjectMetadata.cs**

```csharp
namespace FSH.Framework.Shared.Storage;

public sealed record StoredObjectMetadata(
    long SizeBytes,
    string ContentType,
    DateTimeOffset LastModified,
    string? ETag);
```

- [ ] **Step 2.3: Build + commit**

```
dotnet build src/BuildingBlocks/Shared/Shared.csproj --nologo
git add src/BuildingBlocks/Shared/Storage
git commit -m "feat(storage): shared DTOs for presigned URL + HEAD metadata"
```

---

## Task 3 — Extend IStorageService interface

**Files:**
- Modify: `src/BuildingBlocks/Storage/Services/IStorageService.cs`

- [ ] **Step 3.1: Add three new methods to the interface (existing methods unchanged)**

Append to the interface body in `src/BuildingBlocks/Storage/Services/IStorageService.cs`:

```csharp
    /// <summary>
    /// Mint a short-lived presigned PUT URL for direct browser upload to S3-compatible storage.
    /// Returns the URL plus any required headers the browser MUST include in its PUT request
    /// (e.g., Content-Type when the signature constrains it).
    /// </summary>
    Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mint a short-lived presigned GET URL. Optionally sets response-content-disposition so the
    /// download surfaces with the original filename rather than the storage key.
    /// </summary>
    Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HEAD the object at <paramref name="storageKey"/>. Returns null when the object does not exist.
    /// Used by the Files module on finalize to verify size + content-type vs declared values.
    /// </summary>
    Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
```

Also add `using FSH.Framework.Shared.Storage;` at the top.

- [ ] **Step 3.2: Verify build fails on the three implementations**

Run: `dotnet build src/BuildingBlocks/Storage/Storage.csproj --nologo`
Expected: FAIL with CS0535 (3 errors: `S3StorageService`, `LocalStorageService`, `QuotaMeteredStorageService` don't implement new members).

---

## Task 4 — Implement presigning on S3StorageService

**Files:**
- Modify: `src/BuildingBlocks/Storage/S3/S3StorageService.cs`

- [ ] **Step 4.1: Add new method implementations**

Append inside the `S3StorageService` class (before the `private string BuildKey<T>` helpers):

```csharp
    public async Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var key = NormalizeKey(storageKey);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType
        };

        var url = await _s3.GetPreSignedURLAsync(request).ConfigureAwait(false);

        var requiredHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Content-Type"] = contentType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Issued presigned PUT URL for bucket {Bucket} key {Key} expires {ExpiresAt}",
                _options.Bucket, key, expiresAt);
        }

        return new PresignedUploadUrl(new Uri(url), requiredHeaders, expiresAt);
    }

    public async Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var key = NormalizeKey(storageKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl)
        };

        if (!string.IsNullOrWhiteSpace(responseContentDisposition))
        {
            request.ResponseHeaderOverrides.ContentDisposition = responseContentDisposition;
        }

        var url = await _s3.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    public async Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        try
        {
            var key = NormalizeKey(storageKey);
            var metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken).ConfigureAwait(false);

            return new StoredObjectMetadata(
                metadata.ContentLength,
                string.IsNullOrWhiteSpace(metadata.Headers.ContentType) ? "application/octet-stream" : metadata.Headers.ContentType,
                metadata.LastModified == default ? DateTimeOffset.UtcNow : new DateTimeOffset(metadata.LastModified.ToUniversalTime(), TimeSpan.Zero),
                metadata.ETag);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 HEAD failed for {Key}: {StatusCode}", storageKey, ex.StatusCode);
            return null;
        }
    }
```

Also add `using FSH.Framework.Shared.Storage;` to the top of the file if not already imported.

- [ ] **Step 4.2: Build S3 project**

Run: `dotnet build src/BuildingBlocks/Storage/Storage.csproj --nologo`
Expected: S3 errors gone; `LocalStorageService` + `QuotaMeteredStorageService` still failing.

---

## Task 5 — Implement presigning on LocalStorageService

**Files:**
- Modify: `src/BuildingBlocks/Storage/Local/LocalStorageService.cs`
- Create: `src/BuildingBlocks/Storage/Local/LocalPresignTokenStore.cs` (in-memory token store)

- [ ] **Step 5.1: Create LocalPresignTokenStore.cs**

```csharp
using System.Collections.Concurrent;

namespace FSH.Framework.Storage.Local;

/// <summary>
/// In-memory store of short-lived upload tokens for the local-storage development fallback.
/// Production deployments use S3 — this is here so dev/test without MinIO still works.
/// </summary>
public sealed class LocalPresignTokenStore
{
    private readonly ConcurrentDictionary<string, LocalPresignToken> _tokens = new(StringComparer.Ordinal);

    public string Issue(string storageKey, string contentType, long maxBytes, TimeSpan ttl)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new LocalPresignToken(storageKey, contentType, maxBytes, DateTimeOffset.UtcNow.Add(ttl));
        return token;
    }

    public LocalPresignToken? Consume(string token)
    {
        if (!_tokens.TryRemove(token, out var entry)) return null;
        return entry.ExpiresAt < DateTimeOffset.UtcNow ? null : entry;
    }
}

public sealed record LocalPresignToken(string StorageKey, string ContentType, long MaxBytes, DateTimeOffset ExpiresAt);
```

- [ ] **Step 5.2: Add three new method implementations to LocalStorageService**

Append inside the class:

```csharp
    private static LocalPresignTokenStore? _staticTokenStore;
    private static LocalPresignTokenStore StaticTokenStore => _staticTokenStore ??= new LocalPresignTokenStore();

    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey, string contentType, long maxBytes, TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var token = StaticTokenStore.Issue(storageKey, contentType, maxBytes, ttl);
        var url = new Uri($"local://upload/{token}", UriKind.Absolute);
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Content-Type"] = contentType };
        return Task.FromResult(new PresignedUploadUrl(url, headers, DateTimeOffset.UtcNow.Add(ttl)));
    }

    public Task<Uri> GenerateDownloadUrlAsync(
        string storageKey, TimeSpan ttl, string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        // Local mode: serve via /wwwroot, no signing.
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult(new Uri($"/{normalized}", UriKind.Relative));
    }

    public Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return Task.FromResult<StoredObjectMetadata?>(null);
        var normalizedPath = storageKey.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);
        if (!File.Exists(fullPath)) return Task.FromResult<StoredObjectMetadata?>(null);
        var info = new FileInfo(fullPath);
        if (!_contentTypeProvider.TryGetContentType(info.Name, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return Task.FromResult<StoredObjectMetadata?>(new StoredObjectMetadata(
            info.Length,
            contentType!,
            new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            ETag: null));
    }
```

Add `using FSH.Framework.Shared.Storage;` to the top of the file.

- [ ] **Step 5.3: Build storage project**

Run: `dotnet build src/BuildingBlocks/Storage/Storage.csproj --nologo`
Expected: Local errors gone; `QuotaMeteredStorageService` still failing.

---

## Task 6 — Pass-through new methods on QuotaMeteredStorageService

**Files:**
- Modify: `src/BuildingBlocks/Storage/QuotaMeteredStorageService.cs`

- [ ] **Step 6.1: Add three pass-through implementations**

Append inside the class:

```csharp
    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(string storageKey, string contentType, long maxBytes, TimeSpan ttl, CancellationToken cancellationToken = default)
        => _inner.GenerateUploadUrlAsync(storageKey, contentType, maxBytes, ttl, cancellationToken);

    public Task<Uri> GenerateDownloadUrlAsync(string storageKey, TimeSpan ttl, string? responseContentDisposition = null, CancellationToken cancellationToken = default)
        => _inner.GenerateDownloadUrlAsync(storageKey, ttl, responseContentDisposition, cancellationToken);

    public Task<StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default)
        => _inner.HeadObjectAsync(storageKey, cancellationToken);
```

Quota debit on finalize and refund on hard purge happen in Files module code (Tasks 16, 20), NOT in this decorator — the Files module already knows `actualBytes` after the HEAD call.

- [ ] **Step 6.2: Full solution build**

Run: `dotnet build src/FSH.Starter.slnx --nologo`
Expected: Build succeeds across all projects.

- [ ] **Step 6.3: Commit Storage block changes**

```
git add src/BuildingBlocks/Storage src/BuildingBlocks/Shared/Storage
git commit -m "feat(storage): presigned URL + HEAD methods on IStorageService"
```

---

## Task 7 — MinIO CORS in AppHost

**Files:**
- Modify: `src/Host/FSH.Starter.AppHost/AppHost.cs`

- [ ] **Step 7.1: Replace the minioInitScript block + minioInit container args**

In `AppHost.cs`, replace the existing `minioInitScript` and `minioInit` definitions with:

```csharp
// CORS allow-origin: comma-separated list of admin + dashboard dev origins.
const string AdminOrigin = "http://localhost:5173";
const string DashboardOrigin = "http://localhost:5174";

var minioInitScript = ($$"""
until mc alias set local http://minio:9000 "$MC_USER" "$MC_PASS"; do
  echo "waiting for minio...";
  sleep 2;
done;
mc mb --ignore-existing local/{{MinioBucket}};
mc anonymous set download local/{{MinioBucket}};
mc admin config set local cors_allow_origin="$ADMIN_ORIGIN,$DASHBOARD_ORIGIN";
mc admin config set local cors_allow_methods="GET,PUT,HEAD,POST";
mc admin config set local cors_allow_headers="Content-Type,Authorization,x-amz-*";
mc admin service restart local;
""").ReplaceLineEndings("\n");

var minioInit = builder.AddContainer("minio-init", "minio/mc")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", minioInitScript)
    .WithEnvironment("MC_USER", minioUser)
    .WithEnvironment("MC_PASS", minioPassword)
    .WithEnvironment("ADMIN_ORIGIN", AdminOrigin)
    .WithEnvironment("DASHBOARD_ORIGIN", DashboardOrigin)
    .WaitFor(minio);
```

- [ ] **Step 7.2: Commit**

```
git add src/Host/FSH.Starter.AppHost/AppHost.cs
git commit -m "feat(apphost): MinIO CORS config for browser presigned PUT uploads"
```

---

## Task 8 — FilesOptions + appsettings

**Files:**
- Create: `src/Modules/Files/Modules.Files/FilesOptions.cs`
- Modify: `src/Host/FSH.Starter.Api/appsettings.json` — add `Files` section

- [ ] **Step 8.1: Create FilesOptions.cs**

```csharp
namespace FSH.Modules.Files;

public sealed class FilesOptions
{
    public int UploadUrlTtlMinutes { get; set; } = 15;
    public int DownloadUrlTtlMinutes { get; set; } = 5;
    public int OrphanRetentionMinutes { get; set; } = 60;
    public int SoftDeleteRetentionDays { get; set; } = 30;
    public Dictionary<string, FileCategoryOptions> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FileCategoryOptions
{
    public List<string> AllowedExtensions { get; set; } = [];
    public long MaxBytes { get; set; }
}
```

- [ ] **Step 8.2: Add Files section to appsettings.json**

Add this top-level entry to `src/Host/FSH.Starter.Api/appsettings.json` (preserve existing entries):

```json
"Files": {
  "UploadUrlTtlMinutes": 15,
  "DownloadUrlTtlMinutes": 5,
  "OrphanRetentionMinutes": 60,
  "SoftDeleteRetentionDays": 30,
  "Categories": {
    "Image":    { "AllowedExtensions": [".jpg",".jpeg",".png",".webp",".gif",".ico"], "MaxBytes": 10485760 },
    "Document": { "AllowedExtensions": [".pdf",".docx",".xlsx",".pptx",".txt",".csv"], "MaxBytes": 26214400 },
    "Archive":  { "AllowedExtensions": [".zip"], "MaxBytes": 52428800 }
  }
}
```

- [ ] **Step 8.3: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/FilesOptions.cs src/Host/FSH.Starter.Api/appsettings.json
git commit -m "feat(files): FilesOptions + appsettings defaults"
```

---

## Task 9 — FileAsset domain model

**Files:**
- Create: `src/Modules/Files/Modules.Files/Domain/Visibility.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/FileAssetStatus.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/ScanStatus.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/FileAsset.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/Events/FileFinalizedDomainEvent.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/Events/FileSoftDeletedDomainEvent.cs`
- Create: `src/Modules/Files/Modules.Files/Domain/Events/FilePurgedDomainEvent.cs`

- [ ] **Step 9.1: Create enums**

`src/Modules/Files/Modules.Files/Domain/Visibility.cs`:
```csharp
namespace FSH.Modules.Files.Domain;
public enum Visibility { Public = 0, Private = 1 }
```

`src/Modules/Files/Modules.Files/Domain/FileAssetStatus.cs`:
```csharp
namespace FSH.Modules.Files.Domain;
public enum FileAssetStatus { PendingUpload = 0, Available = 1, Quarantined = 2 }
```

`src/Modules/Files/Modules.Files/Domain/ScanStatus.cs`:
```csharp
namespace FSH.Modules.Files.Domain;
public enum ScanStatus { NotScanned = 0, Clean = 1, Infected = 2, ScanFailed = 3 }
```

- [ ] **Step 9.2: Create FileAsset aggregate**

`src/Modules/Files/Modules.Files/Domain/FileAsset.cs`:
```csharp
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Domain.Events;

namespace FSH.Modules.Files.Domain;

public sealed class FileAsset : AggregateRoot
{
    public string OwnerType { get; private set; } = default!;
    public Guid? OwnerId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = default!;
    public string? Sha256 { get; private set; }
    public Visibility Visibility { get; private set; }
    public FileAssetStatus Status { get; private set; }
    public ScanStatus ScanStatus { get; private set; }
    public DateTimeOffset? UploadDeadline { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = default!;

    private FileAsset() { }

    public static FileAsset CreatePending(
        string ownerType,
        Guid? ownerId,
        string originalFileName,
        string sanitizedFileName,
        string contentType,
        long declaredSizeBytes,
        string storageKey,
        Visibility visibility,
        string createdByUserId,
        DateTimeOffset uploadDeadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);

        return new FileAsset
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            OriginalFileName = originalFileName,
            FileName = sanitizedFileName,
            ContentType = contentType,
            SizeBytes = declaredSizeBytes,
            StorageKey = storageKey,
            Visibility = visibility,
            Status = FileAssetStatus.PendingUpload,
            ScanStatus = ScanStatus.NotScanned,
            UploadDeadline = uploadDeadline,
            CreatedByUserId = createdByUserId
        };
    }

    public void MarkAvailable(long actualSize, ScanStatus scanStatus)
    {
        if (Status != FileAssetStatus.PendingUpload)
        {
            throw new CustomException($"Cannot finalize file in status {Status}.", null, System.Net.HttpStatusCode.Conflict);
        }

        SizeBytes = actualSize;
        ScanStatus = scanStatus;
        Status = scanStatus == ScanStatus.Infected ? FileAssetStatus.Quarantined : FileAssetStatus.Available;
        UploadDeadline = null;
        QueueDomainEvent(new FileFinalizedDomainEvent(Id, TenantId!, OwnerType, OwnerId, Status));
    }

    public void SoftDelete(string actorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        QueueDomainEvent(new FileSoftDeletedDomainEvent(Id, TenantId!, actorUserId));
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
    }
}
```

> **Note:** `AggregateRoot` (from `BuildingBlocks/Core/Domain`) provides `Id`, `TenantId`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, and `QueueDomainEvent`. Verify the exact signature in `src/BuildingBlocks/Core/Domain/AggregateRoot.cs` and adjust the field setter visibility if needed.

- [ ] **Step 9.3: Create domain events**

`src/Modules/Files/Modules.Files/Domain/Events/FileFinalizedDomainEvent.cs`:
```csharp
using FSH.Framework.Core.Domain.Events;

namespace FSH.Modules.Files.Domain.Events;

public sealed record FileFinalizedDomainEvent(
    Guid FileAssetId,
    string TenantId,
    string OwnerType,
    Guid? OwnerId,
    FileAssetStatus FinalStatus) : DomainEvent;
```

`src/Modules/Files/Modules.Files/Domain/Events/FileSoftDeletedDomainEvent.cs`:
```csharp
using FSH.Framework.Core.Domain.Events;

namespace FSH.Modules.Files.Domain.Events;

public sealed record FileSoftDeletedDomainEvent(
    Guid FileAssetId,
    string TenantId,
    string ActorUserId) : DomainEvent;
```

`src/Modules/Files/Modules.Files/Domain/Events/FilePurgedDomainEvent.cs`:
```csharp
using FSH.Framework.Core.Domain.Events;

namespace FSH.Modules.Files.Domain.Events;

public sealed record FilePurgedDomainEvent(
    Guid FileAssetId,
    string TenantId,
    string StorageKey) : DomainEvent;
```

> **Note:** verify `DomainEvent` lives at `FSH.Framework.Core.Domain.Events.DomainEvent` (per CLAUDE.md). If at a different namespace, adjust the using.

- [ ] **Step 9.4: Build + commit**

```
dotnet build src/Modules/Files/Modules.Files/Modules.Files.csproj --nologo
git add src/Modules/Files/Modules.Files/Domain
git commit -m "feat(files): FileAsset aggregate + enums + domain events"
```

---

## Task 10 — FilesDbContext + EF configuration + DbInitializer

**Files:**
- Create: `src/Modules/Files/Modules.Files/Data/FilesDbContext.cs`
- Create: `src/Modules/Files/Modules.Files/Data/FileAssetConfiguration.cs`
- Create: `src/Modules/Files/Modules.Files/Data/FilesDbInitializer.cs`

- [ ] **Step 10.1: Look up the DbContext base + IDbInitializer pattern Catalog uses**

Read these to confirm conventions before writing:
- `src/Modules/Catalog/Modules.Catalog/Data/CatalogDbContext.cs`
- `src/Modules/Catalog/Modules.Catalog/Data/CatalogDbInitializer.cs`

Match their pattern (schema name, base class, ApplyConfigurationsFromAssembly).

- [ ] **Step 10.2: Create FilesDbContext.cs**

```csharp
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Files.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Data;

public sealed class FilesDbContext : FshDbContext
{
    public const string SchemaName = "files";

    public FilesDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        DbContextOptions<FilesDbContext> options,
        IPublisher publisher,
        IOptions<DatabaseOptions> dbOptions)
        : base(tenantAccessor, options, publisher, dbOptions)
    {
    }

    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

> **Note:** the exact base class name (`FshDbContext` here) — check `src/BuildingBlocks/Persistence` for the actual name; CatalogDbContext is the canonical reference. Adjust if the codebase uses a different base name.

- [ ] **Step 10.3: Create FileAssetConfiguration.cs**

```csharp
using FSH.Modules.Files.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Files.Data;

internal sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> b)
    {
        ArgumentNullException.ThrowIfNull(b);
        b.ToTable("FileAssets");
        b.HasKey(x => x.Id);

        b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        b.Property(x => x.OwnerType).HasMaxLength(64).IsRequired();
        b.Property(x => x.OwnerId);
        b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        b.Property(x => x.SizeBytes).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        b.Property(x => x.Sha256).HasMaxLength(64);
        b.Property(x => x.Visibility).HasConversion<int>().IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.ScanStatus).HasConversion<int>().IsRequired();
        b.Property(x => x.UploadDeadline);
        b.Property(x => x.DeletedAt);
        b.Property(x => x.CreatedByUserId).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.Property(x => x.IsDeleted).IsRequired();

        // Use the NAMED SoftDelete filter per decisions/2026-04-30-named-query-filters.md.
        // Multi-tenant filter is applied anonymously by Finbuckle.
        b.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        b.HasIndex(x => new { x.TenantId, x.OwnerType, x.OwnerId })
            .HasDatabaseName("IX_FileAsset_Tenant_Owner");
        b.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_FileAsset_Tenant_Status");
        b.HasIndex(x => new { x.IsDeleted, x.DeletedAt })
            .HasDatabaseName("IX_FileAsset_Deletion");
        b.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("UX_FileAsset_StorageKey");
    }
}
```

> **Note:** `HasQueryFilter("SoftDelete", ...)` — named query filters in EF Core 10 use the `HasQueryFilter(filterKey, expression)` overload. If the project uses a different convention for the named SoftDelete filter, follow the existing pattern (check `src/Modules/Catalog/Modules.Catalog/Data/ProductConfiguration.cs` for the exact API).

- [ ] **Step 10.4: Create FilesDbInitializer.cs**

```csharp
using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Data;

internal sealed class FilesDbInitializer(FilesDbContext dbContext) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

> **Note:** match the exact `IDbInitializer` signature from `CatalogDbInitializer` — adjust if `MigrateAsync` / `SeedAsync` have different signatures in this codebase.

- [ ] **Step 10.5: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/Data
git commit -m "feat(files): FilesDbContext + entity configuration + DbInitializer"
```

---

## Task 11 — EF migration for FileAssets table

**Files:**
- Will create: `src/Host/FSH.Starter.Migrations.PostgreSQL/Migrations/Files/*` (generated)
- Modify: `src/Host/FSH.Starter.Migrations.PostgreSQL/FSH.Starter.Migrations.PostgreSQL.csproj` (likely auto)

- [ ] **Step 11.1: Examine how an existing module's migration assembly is wired**

Run: `Get-ChildItem src/Host/FSH.Starter.Migrations.PostgreSQL/Migrations -Recurse -Filter *.cs | Select-Object FullName -First 20` to find conventions for nested folders per module.

- [ ] **Step 11.2: Add the migration**

From repo root:

```powershell
dotnet ef migrations add CreateFileAssets `
  --project src/Host/FSH.Starter.Migrations.PostgreSQL `
  --startup-project src/Host/FSH.Starter.Api `
  --context FilesDbContext `
  --output-dir Migrations/Files
```

Verify the generated `CreateFileAssets.cs` matches `FileAssetConfiguration` (Up creates table, indexes, soft-delete filter omitted at SQL level).

- [ ] **Step 11.3: Apply migration to a scratch DB and verify**

From repo root with AppHost running (or pointed at a dev Postgres):

```powershell
dotnet ef database update `
  --project src/Host/FSH.Starter.Migrations.PostgreSQL `
  --startup-project src/Host/FSH.Starter.Api `
  --context FilesDbContext
```

Expected: `files.FileAssets` table exists. Use pgAdmin or `psql` to verify columns + indexes.

- [ ] **Step 11.4: Commit**

```
git add src/Host/FSH.Starter.Migrations.PostgreSQL/Migrations/Files
git commit -m "feat(files): EF migration for FileAssets table"
```

---

## Task 12 — StorageKeyBuilder + unit tests

**Files:**
- Create: `src/Modules/Files/Modules.Files/Services/StorageKeyBuilder.cs`
- Create: `src/Tests/Files.Tests/Files.Tests.csproj` (also covers Tasks 22–25)
- Create: `src/Tests/Files.Tests/Services/StorageKeyBuilderTests.cs`

- [ ] **Step 12.1: Create StorageKeyBuilder.cs**

```csharp
using System.Text.RegularExpressions;

namespace FSH.Modules.Files.Services;

public static class StorageKeyBuilder
{
    /// <summary>
    /// Canonical storage key:
    ///   tenants/{tenantId}/{ownerType-lower}/{yyyy}/{MM}/{fileAssetId:N}/{sanitized-filename}
    /// </summary>
    public static string Build(string tenantId, string ownerType, Guid fileAssetId, string fileName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
#pragma warning disable CA1308
        var lowerOwner = ownerType.ToLowerInvariant();
#pragma warning restore CA1308
        var safe = Sanitize(fileName);
        return $"tenants/{tenantId}/{lowerOwner}/{now:yyyy}/{now:MM}/{fileAssetId:N}/{safe}";
    }

    public static string Sanitize(string fileName) =>
        Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
}
```

- [ ] **Step 12.2: Create Files.Tests project**

`src/Tests/Files.Tests/Files.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AutoFixture" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Modules\Files\Modules.Files\Modules.Files.csproj" />
    <ProjectReference Include="..\..\Modules\Files\Modules.Files.Contracts\Modules.Files.Contracts.csproj" />
  </ItemGroup>
</Project>
```

Add to `src/FSH.Starter.slnx` under `/Tests/`:
```xml
<Project Path="Tests/Files.Tests/Files.Tests.csproj" />
```

- [ ] **Step 12.3: Write StorageKeyBuilderTests.cs (TDD: this is the test layer for StorageKeyBuilder)**

```csharp
using FSH.Modules.Files.Services;
using Shouldly;

namespace Files.Tests.Services;

public class StorageKeyBuilderTests
{
    [Fact]
    public void Build_Should_ProduceCanonicalShape()
    {
        var now = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero);
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var key = StorageKeyBuilder.Build("tenant-a", "Product", id, "shoe photo.png", now);

        key.ShouldBe("tenants/tenant-a/product/2026/05/11111111222233334444555555555555/shoe_photo.png");
    }

    [Fact]
    public void Sanitize_Should_StripUnsafeCharacters()
    {
        StorageKeyBuilder.Sanitize("ke!llo$.png").ShouldBe("ke_llo_.png");
    }

    [Theory]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(" ", typeof(ArgumentException))]
    public void Build_Should_RejectInvalidInput(string fileName, Type exceptionType)
    {
        Should.Throw(
            () => StorageKeyBuilder.Build("t", "o", Guid.NewGuid(), fileName, DateTimeOffset.UtcNow),
            exceptionType);
    }
}
```

- [ ] **Step 12.4: Run unit tests**

Run: `dotnet test src/Tests/Files.Tests/Files.Tests.csproj --nologo`
Expected: 4 tests pass.

- [ ] **Step 12.5: Commit**

```
git add src/Modules/Files/Modules.Files/Services/StorageKeyBuilder.cs src/Tests/Files.Tests src/FSH.Starter.slnx
git commit -m "feat(files): StorageKeyBuilder + unit tests"
```

---

## Task 13 — FileAsset state-machine unit tests

**Files:**
- Create: `src/Tests/Files.Tests/Domain/FileAssetTests.cs`

- [ ] **Step 13.1: Write FileAssetTests.cs**

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Domain;
using Shouldly;

namespace Files.Tests.Domain;

public class FileAssetTests
{
    private static FileAsset NewPending() => FileAsset.CreatePending(
        ownerType: "Product",
        ownerId: Guid.NewGuid(),
        originalFileName: "x.png",
        sanitizedFileName: "x.png",
        contentType: "image/png",
        declaredSizeBytes: 1024,
        storageKey: "tenants/t/product/2026/05/abc/x.png",
        visibility: Visibility.Public,
        createdByUserId: "user-1",
        uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(15));

    [Fact]
    public void CreatePending_Should_StartInPendingUpload()
    {
        var f = NewPending();
        f.Status.ShouldBe(FileAssetStatus.PendingUpload);
        f.ScanStatus.ShouldBe(ScanStatus.NotScanned);
        f.UploadDeadline.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAvailable_Should_TransitionToAvailable_When_ScanClean()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);
        f.Status.ShouldBe(FileAssetStatus.Available);
        f.SizeBytes.ShouldBe(2048);
        f.UploadDeadline.ShouldBeNull();
    }

    [Fact]
    public void MarkAvailable_Should_TransitionToQuarantined_When_ScanInfected()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Infected);
        f.Status.ShouldBe(FileAssetStatus.Quarantined);
        f.ScanStatus.ShouldBe(ScanStatus.Infected);
    }

    [Fact]
    public void MarkAvailable_Should_Reject_When_NotPendingUpload()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);
        Should.Throw<CustomException>(() => f.MarkAvailable(4096, ScanStatus.Clean));
    }

    [Fact]
    public void SoftDelete_Should_BeIdempotent()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);
        f.SoftDelete("user-2");
        f.IsDeleted.ShouldBeTrue();
        f.DeletedAt.ShouldNotBeNull();

        var firstDeletedAt = f.DeletedAt;
        f.SoftDelete("user-2"); // should noop
        f.DeletedAt.ShouldBe(firstDeletedAt);
    }

    [Fact]
    public void Restore_Should_ClearIsDeleted()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);
        f.SoftDelete("user-2");
        f.Restore();
        f.IsDeleted.ShouldBeFalse();
        f.DeletedAt.ShouldBeNull();
    }
}
```

- [ ] **Step 13.2: Run + commit**

```
dotnet test src/Tests/Files.Tests/Files.Tests.csproj --nologo
git add src/Tests/Files.Tests/Domain
git commit -m "test(files): FileAsset state-machine unit tests"
```

---

## Task 14 — IFileScanner + NoOpFileScanner

**Files:**
- Create: `src/Modules/Files/Modules.Files/Services/IFileScanner.cs`
- Create: `src/Modules/Files/Modules.Files/Services/NoOpFileScanner.cs`

- [ ] **Step 14.1: Create IFileScanner.cs**

```csharp
using FSH.Modules.Files.Domain;

namespace FSH.Modules.Files.Services;

public interface IFileScanner
{
    ValueTask<ScanStatus> ScanAsync(string storageKey, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 14.2: Create NoOpFileScanner.cs**

```csharp
using FSH.Modules.Files.Domain;

namespace FSH.Modules.Files.Services;

internal sealed class NoOpFileScanner : IFileScanner
{
    public ValueTask<ScanStatus> ScanAsync(string storageKey, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ScanStatus.Clean);
}
```

- [ ] **Step 14.3: Build + commit (no tests yet — covered by handler integration tests)**

```
dotnet build src/Modules/Files/Modules.Files/Modules.Files.csproj --nologo
git add src/Modules/Files/Modules.Files/Services
git commit -m "feat(files): IFileScanner + NoOpFileScanner default"
```

---

## Task 15 — Contracts: IFileAccessPolicy + registry + DTOs/commands/queries

**Files:**
- Create: `src/Modules/Files/Modules.Files.Contracts/IFileAccessPolicy.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/FileAssetReference.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/DTOs/FileAssetDto.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/DTOs/PresignedUploadResponse.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/DTOs/PresignedDownloadResponse.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Commands/RequestUploadUrlCommand.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Commands/FinalizeUploadCommand.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Commands/DeleteFileCommand.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Commands/RestoreFileCommand.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Queries/GetFileMetadataQuery.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Queries/GetFileDownloadUrlQuery.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Queries/ListMyFilesQuery.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/v1/Queries/ListTrashedFilesQuery.cs`
- Create: `src/Modules/Files/Modules.Files.Contracts/Events/FileFinalizedIntegrationEvent.cs`

- [ ] **Step 15.1: Create IFileAccessPolicy.cs**

```csharp
using System.Security.Claims;

namespace FSH.Modules.Files.Contracts;

public interface IFileAccessPolicy
{
    /// <summary>Owner type this policy handles. Must be unique across registered policies.</summary>
    string OwnerType { get; }

    Task<bool> CanAttachAsync(Guid? ownerId, ClaimsPrincipal user, CancellationToken cancellationToken);

    /// <summary>file is the FileAsset row; passed as DTO so module isn't exposed.</summary>
    Task<bool> CanReadAsync(FileAccessContext context, ClaimsPrincipal user, CancellationToken cancellationToken);

    Task<bool> CanDeleteAsync(FileAccessContext context, ClaimsPrincipal user, CancellationToken cancellationToken);
}

public sealed record FileAccessContext(
    Guid FileAssetId,
    string TenantId,
    string OwnerType,
    Guid? OwnerId,
    string CreatedByUserId,
    int Visibility);  // matches Visibility enum int value
```

- [ ] **Step 15.2: Create FileAssetReference.cs**

```csharp
namespace FSH.Modules.Files.Contracts;

public sealed record FileAssetReference(Guid Id, string OwnerType, Guid? OwnerId);
```

- [ ] **Step 15.3: Create FileAssetDto.cs**

```csharp
namespace FSH.Modules.Files.Contracts.v1.DTOs;

public sealed record FileAssetDto(
    Guid Id,
    string OwnerType,
    Guid? OwnerId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int Visibility,
    int Status,
    int ScanStatus,
    DateTimeOffset CreatedAt,
    string? PublicUrl);
```

- [ ] **Step 15.4: Create PresignedUploadResponse.cs**

```csharp
namespace FSH.Modules.Files.Contracts.v1.DTOs;

public sealed record PresignedUploadResponse(
    Guid FileAssetId,
    Uri UploadUrl,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);
```

- [ ] **Step 15.5: Create PresignedDownloadResponse.cs**

```csharp
namespace FSH.Modules.Files.Contracts.v1.DTOs;

public sealed record PresignedDownloadResponse(Uri Url, DateTimeOffset ExpiresAt);
```

- [ ] **Step 15.6: Create commands + queries**

`RequestUploadUrlCommand.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record RequestUploadUrlCommand(
    string OwnerType,
    Guid? OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    int Visibility,
    string Category) : IRequest<PresignedUploadResponse>;
```

`FinalizeUploadCommand.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record FinalizeUploadCommand(Guid FileAssetId) : IRequest<FileAssetDto>;
```

`DeleteFileCommand.cs`:
```csharp
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record DeleteFileCommand(Guid FileAssetId) : IRequest<Unit>;
```

`RestoreFileCommand.cs`:
```csharp
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

public sealed record RestoreFileCommand(Guid FileAssetId) : IRequest<Unit>;
```

`GetFileMetadataQuery.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record GetFileMetadataQuery(Guid FileAssetId) : IRequest<FileAssetDto>;
```

`GetFileDownloadUrlQuery.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record GetFileDownloadUrlQuery(Guid FileAssetId) : IRequest<PresignedDownloadResponse>;
```

`ListMyFilesQuery.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record ListMyFilesQuery(int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<FileAssetDto>>;
```

`ListTrashedFilesQuery.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Queries;

public sealed record ListTrashedFilesQuery(int Page = 1, int PageSize = 50) : IRequest<IReadOnlyList<FileAssetDto>>;
```

- [ ] **Step 15.7: Create FileFinalizedIntegrationEvent.cs**

```csharp
using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Files.Contracts.Events;

public sealed record FileFinalizedIntegrationEvent(
    Guid FileAssetId,
    string TenantId,
    string OwnerType,
    Guid? OwnerId,
    string ContentType,
    long SizeBytes) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
```

> **Note:** Verify `IIntegrationEvent`'s actual interface members from `src/BuildingBlocks/Eventing.Abstractions`. Adjust property names/types to match.

- [ ] **Step 15.8: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files.Contracts
git commit -m "feat(files): contracts (IFileAccessPolicy, commands, queries, DTOs, integration event)"
```

---

## Task 16 — FileAccessPolicyRegistry + DI extension + DefaultUploaderOnlyPolicy

**Files:**
- Create: `src/Modules/Files/Modules.Files/Services/FileAccessPolicyRegistry.cs`
- Create: `src/Modules/Files/Modules.Files/Services/FileAccessPolicyExtensions.cs` (this lives in Modules.Files to keep DI surface module-scoped; re-export from Contracts if needed by Phase B+)
- Create: `src/Modules/Files/Modules.Files/Authorization/DefaultUploaderOnlyPolicy.cs`
- Create: `src/Tests/Files.Tests/Services/FileAccessPolicyRegistryTests.cs`

- [ ] **Step 16.1: Create the registry**

```csharp
using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Files.Services;

internal sealed class FileAccessPolicyRegistry
{
    private readonly Dictionary<string, IFileAccessPolicy> _policies;

    public FileAccessPolicyRegistry(IEnumerable<IFileAccessPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        _policies = policies.ToDictionary(p => p.OwnerType, StringComparer.OrdinalIgnoreCase);
    }

    public IFileAccessPolicy? Resolve(string ownerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        return _policies.TryGetValue(ownerType, out var policy) ? policy : null;
    }
}
```

- [ ] **Step 16.2: Create the DI extension**

```csharp
using FSH.Modules.Files.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Files.Services;

public static class FileAccessPolicyExtensions
{
    public static IServiceCollection AddFileAccessPolicy<TPolicy>(this IServiceCollection services)
        where TPolicy : class, IFileAccessPolicy
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IFileAccessPolicy, TPolicy>();
        return services;
    }
}
```

- [ ] **Step 16.3: Create DefaultUploaderOnlyPolicy.cs**

This policy is registered twice (once per `OwnerType` it serves) by `FilesModule`. Implementation:

```csharp
using System.Security.Claims;
using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Files.Authorization;

internal sealed class DefaultUploaderOnlyPolicy : IFileAccessPolicy
{
    private readonly string _ownerType;
    public DefaultUploaderOnlyPolicy(string ownerType) => _ownerType = ownerType;
    public string OwnerType => _ownerType;

    public Task<bool> CanAttachAsync(Guid? ownerId, ClaimsPrincipal user, CancellationToken ct)
        => Task.FromResult(user.Identity?.IsAuthenticated == true);

    public Task<bool> CanReadAsync(FileAccessContext ctx, ClaimsPrincipal user, CancellationToken ct)
    {
        if (user.Identity?.IsAuthenticated != true) return Task.FromResult(false);
        // Public-within-tenant or uploader-only-private.
        if (ctx.Visibility == 0 /*Public*/) return Task.FromResult(true);
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Task.FromResult(string.Equals(sub, ctx.CreatedByUserId, StringComparison.Ordinal));
    }

    public Task<bool> CanDeleteAsync(FileAccessContext ctx, ClaimsPrincipal user, CancellationToken ct)
    {
        if (user.Identity?.IsAuthenticated != true) return Task.FromResult(false);
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Task.FromResult(string.Equals(sub, ctx.CreatedByUserId, StringComparison.Ordinal));
    }
}
```

- [ ] **Step 16.4: Write registry tests first (TDD)**

```csharp
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Services;
using NSubstitute;
using Shouldly;

namespace Files.Tests.Services;

public class FileAccessPolicyRegistryTests
{
    [Fact]
    public void Resolve_Should_ReturnPolicy_When_Registered()
    {
        var p = Substitute.For<IFileAccessPolicy>();
        p.OwnerType.Returns("Product");
        var reg = new FileAccessPolicyRegistry([p]);
        reg.Resolve("Product").ShouldBe(p);
    }

    [Fact]
    public void Resolve_Should_BeCaseInsensitive()
    {
        var p = Substitute.For<IFileAccessPolicy>();
        p.OwnerType.Returns("MyFiles");
        var reg = new FileAccessPolicyRegistry([p]);
        reg.Resolve("MYFILES").ShouldBe(p);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_When_NotRegistered()
    {
        var reg = new FileAccessPolicyRegistry([]);
        reg.Resolve("Unknown").ShouldBeNull();
    }
}
```

`FileAccessPolicyRegistry` is internal — add `[assembly: InternalsVisibleTo("Files.Tests")]` to `Modules.Files/AssemblyInfo.cs` (create it if missing).

- [ ] **Step 16.5: Run tests + commit**

```
dotnet test src/Tests/Files.Tests/Files.Tests.csproj --nologo
git add src/Modules/Files/Modules.Files/Services src/Modules/Files/Modules.Files/Authorization src/Tests/Files.Tests
git commit -m "feat(files): IFileAccessPolicy registry + default uploader-only policy"
```

---

## Task 17 — Permissions constants

**Files:**
- Create: `src/Modules/Files/Modules.Files/Authorization/FilesPermissionConstants.cs`
- Create: `src/Modules/Files/Modules.Files/Authorization/FilesPermissions.cs` (the array consumed by `PermissionConstants.Register`)

- [ ] **Step 17.1: Look at Catalog's permission pattern**

Read `src/Modules/Catalog/Modules.Catalog.Contracts/Authorization/CatalogPermissions.cs` (or the equivalent path) to match the existing style.

- [ ] **Step 17.2: Create FilesPermissionConstants.cs**

```csharp
namespace FSH.Modules.Files.Authorization;

public static class FilesPermissionConstants
{
    public const string Upload    = "Permissions.Files.Upload";
    public const string DeleteOwn = "Permissions.Files.DeleteOwn";
    public const string DeleteAny = "Permissions.Files.DeleteAny";
    public const string ViewTrash = "Permissions.Files.ViewTrash";
    public const string Restore   = "Permissions.Files.Restore";
}
```

- [ ] **Step 17.3: Create FilesPermissions.cs (the list to register)**

```csharp
namespace FSH.Modules.Files.Authorization;

internal static class FilesPermissions
{
    public static readonly string[] All =
    [
        FilesPermissionConstants.Upload,
        FilesPermissionConstants.DeleteOwn,
        FilesPermissionConstants.DeleteAny,
        FilesPermissionConstants.ViewTrash,
        FilesPermissionConstants.Restore
    ];
}
```

- [ ] **Step 17.4: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/Authorization
git commit -m "feat(files): permission constants"
```

---

## Task 18 — RequestUploadUrl: endpoint + handler + validator

**Files:**
- Create: `src/Modules/Files/Modules.Files/Features/v1/RequestUploadUrl/RequestUploadUrlEndpoint.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/RequestUploadUrl/RequestUploadUrlCommandHandler.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/RequestUploadUrl/RequestUploadUrlCommandValidator.cs`

- [ ] **Step 18.1: Endpoint**

```csharp
using FSH.Framework.Web.Authorization;
using FSH.Modules.Files.Authorization;
using FSH.Modules.Files.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.RequestUploadUrl;

public static class RequestUploadUrlEndpoint
{
    internal static RouteHandlerBuilder MapRequestUploadUrlEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/upload-url", (
                RequestUploadUrlCommand command,
                IMediator mediator,
                CancellationToken cancellationToken) =>
                mediator.Send(command, cancellationToken))
            .WithName("RequestFileUploadUrl")
            .WithSummary("Mint a presigned PUT URL for a file upload")
            .RequirePermission(FilesPermissionConstants.Upload);
}
```

> **Note:** `RequirePermission` extension lives in `BuildingBlocks/Web/Authorization`. Verify the namespace.

- [ ] **Step 18.2: Validator**

```csharp
using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Commands;

namespace FSH.Modules.Files.Features.v1.RequestUploadUrl;

public sealed class RequestUploadUrlCommandValidator : AbstractValidator<RequestUploadUrlCommand>
{
    public RequestUploadUrlCommandValidator()
    {
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Visibility).InclusiveBetween(0, 1);
    }
}
```

- [ ] **Step 18.3: Handler**

```csharp
using System.Net;
using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Features.v1.RequestUploadUrl;

internal sealed class RequestUploadUrlCommandHandler(
    FilesDbContext db,
    IStorageService storage,
    FileAccessPolicyRegistry policies,
    IQuotaService quotas,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContext,
    IOptions<FilesOptions> options) : ICommandHandler<RequestUploadUrlCommand, PresignedUploadResponse>
{
    public async ValueTask<PresignedUploadResponse> Handle(RequestUploadUrlCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
                       ?? throw new UnauthorizedException("invalid tenant");
        var userId = currentUser.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId) || userId == Guid.Empty.ToString())
            throw new UnauthorizedException("no current user");

        // Category lookup + extension/size validation
        if (!options.Value.Categories.TryGetValue(cmd.Category, out var category))
            throw new CustomException($"Unknown category '{cmd.Category}'.", null, HttpStatusCode.BadRequest);

        var ext = Path.GetExtension(cmd.FileName);
        if (string.IsNullOrWhiteSpace(ext) ||
            !category.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new CustomException($"Extension '{ext}' not allowed for category '{cmd.Category}'.", null, HttpStatusCode.BadRequest);

        if (cmd.SizeBytes > category.MaxBytes)
            throw new CustomException($"File exceeds max size {category.MaxBytes} bytes.", null, HttpStatusCode.BadRequest);

        // Authorization
        var principal = httpContext.HttpContext?.User
                        ?? throw new UnauthorizedException("no http context");
        var policy = policies.Resolve(cmd.OwnerType)
                     ?? throw new ForbiddenException($"No policy registered for owner type {cmd.OwnerType}.");
        if (!await policy.CanAttachAsync(cmd.OwnerId, principal, ct).ConfigureAwait(false))
            throw new ForbiddenException("Not allowed to attach files to this owner.");

        // Quota precheck (don't debit yet — debit on finalize when we know actual bytes)
        var quotaCheck = await quotas.CheckAsync(tenantId, QuotaResource.StorageBytes, cmd.SizeBytes, ct).ConfigureAwait(false);
        if (!quotaCheck.Allowed)
            throw new CustomException($"Storage quota exceeded ({quotaCheck.CurrentUsage}/{quotaCheck.Limit}).", null, (HttpStatusCode)507);

        var id = Guid.NewGuid();
        var safeName = StorageKeyBuilder.Sanitize(cmd.FileName);
        var storageKey = StorageKeyBuilder.Build(tenantId, cmd.OwnerType, id, cmd.FileName, DateTimeOffset.UtcNow);
        var ttl = TimeSpan.FromMinutes(options.Value.UploadUrlTtlMinutes);

        var presigned = await storage.GenerateUploadUrlAsync(storageKey, cmd.ContentType, category.MaxBytes, ttl, ct).ConfigureAwait(false);

        var asset = FileAsset.CreatePending(
            ownerType: cmd.OwnerType,
            ownerId: cmd.OwnerId,
            originalFileName: cmd.FileName,
            sanitizedFileName: safeName,
            contentType: cmd.ContentType,
            declaredSizeBytes: cmd.SizeBytes,
            storageKey: storageKey,
            visibility: (Visibility)cmd.Visibility,
            createdByUserId: userId,
            uploadDeadline: DateTimeOffset.UtcNow.Add(ttl));

        // Force id to the one we used for storage key (so they match).
        typeof(FSH.Framework.Core.Domain.BaseEntity)
            .GetProperty("Id")!
            .SetValue(asset, id);

        db.FileAssets.Add(asset);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new PresignedUploadResponse(asset.Id, presigned.Url, presigned.RequiredHeaders, presigned.ExpiresAt);
    }
}
```

> **Note:** the `typeof(BaseEntity).GetProperty("Id").SetValue(...)` hack is needed because `AggregateRoot` likely auto-generates `Id` in its constructor. **Verify the actual `Id` setter visibility** in `src/BuildingBlocks/Core/Domain/BaseEntity.cs` and use the cleaner path if a protected setter is available — adjust `CreatePending` to accept an `id` parameter instead.

> **Note:** `IQuotaService.CheckAsync` (read-only) and `CheckAndRecordAsync` (debits) — verify exact signatures in `src/BuildingBlocks/Quota`. If `CheckAsync` doesn't exist as a separate method, use a probe + manual cleanup or accept the debit and refund on finalize-failure path.

- [ ] **Step 18.4: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/Features/v1/RequestUploadUrl
git commit -m "feat(files): RequestUploadUrl endpoint + handler + validator"
```

---

## Task 19 — FinalizeUpload: endpoint + handler

**Files:**
- Create: `src/Modules/Files/Modules.Files/Features/v1/FinalizeUpload/FinalizeUploadEndpoint.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/FinalizeUpload/FinalizeUploadCommandHandler.cs`

- [ ] **Step 19.1: Endpoint**

```csharp
using FSH.Modules.Files.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.FinalizeUpload;

public static class FinalizeUploadEndpoint
{
    internal static RouteHandlerBuilder MapFinalizeUploadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/{id:guid}/finalize", (
                Guid id,
                IMediator mediator,
                CancellationToken cancellationToken) =>
                mediator.Send(new FinalizeUploadCommand(id), cancellationToken))
            .WithName("FinalizeFileUpload")
            .WithSummary("Finalize a file upload after the browser PUT completes")
            .RequireAuthorization();
}
```

- [ ] **Step 19.2: Handler**

```csharp
using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.FinalizeUpload;

internal sealed class FinalizeUploadCommandHandler(
    FilesDbContext db,
    IStorageService storage,
    IFileScanner scanner,
    IQuotaService quotas,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ICurrentUser currentUser,
    IEventBus events) : ICommandHandler<FinalizeUploadCommand, FileAssetDto>
{
    public async ValueTask<FileAssetDto> Handle(FinalizeUploadCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
                       ?? throw new UnauthorizedException("invalid tenant");
        var userId = currentUser.GetUserId().ToString();

        var asset = await db.FileAssets.FirstOrDefaultAsync(f => f.Id == cmd.FileAssetId, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException("file not found");

        if (!string.Equals(asset.CreatedByUserId, userId, StringComparison.Ordinal))
            throw new ForbiddenException("not your pending file");

        if (asset.Status != FileAssetStatus.PendingUpload)
            throw new CustomException("file already finalized", null, HttpStatusCode.Conflict);

        var head = await storage.HeadObjectAsync(asset.StorageKey, ct).ConfigureAwait(false)
                   ?? throw new CustomException("upload not received", null, HttpStatusCode.Conflict);

        // Size: allow declared+1% slack (S3 may report exact bytes for multipart).
        var maxAllowed = asset.SizeBytes + Math.Max(1024, asset.SizeBytes / 100);
        if (head.SizeBytes > maxAllowed)
        {
            await storage.RemoveAsync(asset.StorageKey, ct).ConfigureAwait(false);
            db.FileAssets.Remove(asset);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            throw new CustomException($"uploaded size ({head.SizeBytes}) exceeds declared ({asset.SizeBytes})", null, HttpStatusCode.BadRequest);
        }
        if (!string.Equals(head.ContentType, asset.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            await storage.RemoveAsync(asset.StorageKey, ct).ConfigureAwait(false);
            db.FileAssets.Remove(asset);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            throw new CustomException($"uploaded content-type mismatch", null, HttpStatusCode.BadRequest);
        }

        var scanResult = await scanner.ScanAsync(asset.StorageKey, ct).ConfigureAwait(false);
        asset.MarkAvailable(head.SizeBytes, scanResult);

        // Quota debit (refund on hard purge by background job).
        await quotas.RecordAsync(tenantId, QuotaResource.StorageBytes, head.SizeBytes, ct).ConfigureAwait(false);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await events.PublishAsync(new FileFinalizedIntegrationEvent(
            asset.Id, tenantId, asset.OwnerType, asset.OwnerId, asset.ContentType, asset.SizeBytes), ct).ConfigureAwait(false);

        return MapToDto(asset, publicUrl: null);
    }

    internal static FileAssetDto MapToDto(FileAsset f, string? publicUrl) =>
        new(f.Id, f.OwnerType, f.OwnerId, f.OriginalFileName, f.ContentType, f.SizeBytes,
            (int)f.Visibility, (int)f.Status, (int)f.ScanStatus, f.CreatedAt, publicUrl);
}
```

> **Note:** `IEventBus` interface for publishing integration events — verify name + method in `src/BuildingBlocks/Eventing.Abstractions`.

- [ ] **Step 19.3: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/Features/v1/FinalizeUpload
git commit -m "feat(files): FinalizeUpload endpoint + handler"
```

---

## Task 20 — Read endpoints (GetMetadata, GetDownloadUrl, GetContent)

**Files:**
- Create: `src/Modules/Files/Modules.Files/Features/v1/GetFileMetadata/{Endpoint,Handler}.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/GetFileDownloadUrl/{Endpoint,Handler}.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/GetFileContent/{Endpoint,Handler}.cs`

- [ ] **Step 20.1: GetFileMetadata endpoint + handler**

`GetFileMetadataEndpoint.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.GetFileMetadata;

public static class GetFileMetadataEndpoint
{
    internal static RouteHandlerBuilder MapGetFileMetadataEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/{id:guid}", (Guid id, IMediator m, CancellationToken ct) =>
                m.Send(new GetFileMetadataQuery(id), ct))
            .WithName("GetFileMetadata").RequireAuthorization();
}
```

`GetFileMetadataQueryHandler.cs`:
```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Features.v1.FinalizeUpload;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FSH.Framework.Storage.Services;

namespace FSH.Modules.Files.Features.v1.GetFileMetadata;

internal sealed class GetFileMetadataQueryHandler(
    FilesDbContext db,
    FileAccessPolicyRegistry policies,
    IHttpContextAccessor httpContext,
    IStorageService storage,
    IOptions<FilesOptions> options) : IQueryHandler<GetFileMetadataQuery, FileAssetDto>
{
    public async ValueTask<FileAssetDto> Handle(GetFileMetadataQuery q, CancellationToken ct)
    {
        var f = await db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.FileAssetId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var user = httpContext.HttpContext?.User ?? throw new UnauthorizedException("no user");
        var policy = policies.Resolve(f.OwnerType) ?? throw new ForbiddenException("no policy");
        var ctxObj = new FileAccessContext(f.Id, f.TenantId!, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctxObj, user, ct).ConfigureAwait(false))
            throw new NotFoundException("file not found"); // don't leak existence

        string? publicUrl = null;
        if (f.Visibility == Visibility.Public)
        {
            var uri = await storage.GenerateDownloadUrlAsync(
                f.StorageKey,
                TimeSpan.FromMinutes(options.Value.DownloadUrlTtlMinutes),
                cancellationToken: ct).ConfigureAwait(false);
            publicUrl = uri.ToString();
        }

        return FinalizeUploadCommandHandler.MapToDto(f, publicUrl);
    }
}
```

- [ ] **Step 20.2: GetFileDownloadUrl endpoint + handler**

`GetFileDownloadUrlEndpoint.cs`:
```csharp
using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.GetFileDownloadUrl;

public static class GetFileDownloadUrlEndpoint
{
    internal static RouteHandlerBuilder MapGetFileDownloadUrlEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/{id:guid}/url", (Guid id, IMediator m, CancellationToken ct) =>
                m.Send(new GetFileDownloadUrlQuery(id), ct))
            .WithName("GetFileDownloadUrl").RequireAuthorization();
}
```

`GetFileDownloadUrlQueryHandler.cs`:
```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Services;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Features.v1.GetFileDownloadUrl;

internal sealed class GetFileDownloadUrlQueryHandler(
    FilesDbContext db,
    IStorageService storage,
    FileAccessPolicyRegistry policies,
    IHttpContextAccessor httpContext,
    IOptions<FilesOptions> options) : IQueryHandler<GetFileDownloadUrlQuery, PresignedDownloadResponse>
{
    public async ValueTask<PresignedDownloadResponse> Handle(GetFileDownloadUrlQuery q, CancellationToken ct)
    {
        var f = await db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.FileAssetId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var user = httpContext.HttpContext?.User ?? throw new UnauthorizedException("no user");
        var policy = policies.Resolve(f.OwnerType) ?? throw new ForbiddenException("no policy");
        var ctxObj = new FileAccessContext(f.Id, f.TenantId!, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctxObj, user, ct).ConfigureAwait(false))
            throw new NotFoundException("file not found");

        var ttl = TimeSpan.FromMinutes(options.Value.DownloadUrlTtlMinutes);
        var disposition = $"attachment; filename=\"{f.OriginalFileName}\"";
        var url = await storage.GenerateDownloadUrlAsync(f.StorageKey, ttl, disposition, ct).ConfigureAwait(false);
        return new PresignedDownloadResponse(url, DateTimeOffset.UtcNow.Add(ttl));
    }
}
```

- [ ] **Step 20.3: GetFileContent (proxy fallback) endpoint + handler**

`GetFileContentEndpoint.cs`:
```csharp
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace FSH.Modules.Files.Features.v1.GetFileContent;

public static class GetFileContentEndpoint
{
    internal static RouteHandlerBuilder MapGetFileContentEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/{id:guid}/content", async (
                Guid id,
                [FromServices] GetFileContentService svc,
                CancellationToken ct) =>
            {
                var (stream, contentType, fileName, length) = await svc.OpenAsync(id, ct).ConfigureAwait(false);
                return Results.File(stream, contentType, fileName, enableRangeProcessing: false);
            })
            .WithName("GetFileContent").RequireAuthorization();
}
```

`GetFileContentService.cs` (service rather than handler because it returns a stream):
```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.GetFileContent;

internal sealed class GetFileContentService(
    FilesDbContext db,
    IStorageService storage,
    FileAccessPolicyRegistry policies,
    IHttpContextAccessor httpContext)
{
    public async Task<(Stream Stream, string ContentType, string FileName, long? Length)> OpenAsync(Guid id, CancellationToken ct)
    {
        var f = await db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var user = httpContext.HttpContext?.User ?? throw new UnauthorizedException("no user");
        var policy = policies.Resolve(f.OwnerType) ?? throw new ForbiddenException("no policy");
        var ctxObj = new FileAccessContext(f.Id, f.TenantId!, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctxObj, user, ct).ConfigureAwait(false))
            throw new NotFoundException("file not found");

        var dl = await storage.DownloadAsync(f.StorageKey, ct).ConfigureAwait(false)
                 ?? throw new NotFoundException("bytes missing");
        return (dl.Stream, dl.ContentType, f.OriginalFileName, dl.ContentLength);
    }
}
```

- [ ] **Step 20.4: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/Features/v1/GetFileMetadata src/Modules/Files/Modules.Files/Features/v1/GetFileDownloadUrl src/Modules/Files/Modules.Files/Features/v1/GetFileContent
git commit -m "feat(files): read endpoints (metadata, download URL, content proxy)"
```

---

## Task 21 — Lifecycle endpoints (ListMy, Delete, Restore, ListTrash)

**Files:**
- Create: `src/Modules/Files/Modules.Files/Features/v1/ListMyFiles/{Endpoint,Handler}.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/DeleteFile/{Endpoint,Handler}.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/RestoreFile/{Endpoint,Handler}.cs`
- Create: `src/Modules/Files/Modules.Files/Features/v1/ListTrashedFiles/{Endpoint,Handler}.cs`

For each: endpoint follows the `/mine`, `/{id:guid}` DELETE, `/{id:guid}/restore` POST, `/trash` GET pattern. Handlers use:
- `ListMyFilesQueryHandler`: filter by `CreatedByUserId == currentUser`, exclude `IsDeleted` (default filter does this), order by `CreatedAt desc`, paginate.
- `DeleteFileCommandHandler`: load file, resolve policy, check `CanDeleteAsync`, call `asset.SoftDelete(userId)`, save.
- `RestoreFileCommandHandler`: requires `Files.Restore` permission (admin). Use `IgnoreQueryFilters()` to find soft-deleted row. Call `asset.Restore()`, save.
- `ListTrashedFilesQueryHandler`: requires `Files.ViewTrash` perm. Use `IgnoreQueryFilters().Where(f => f.IsDeleted && f.TenantId == currentTenant)`. Order by `DeletedAt desc`.

Follow the exact code patterns from Task 18–20. Each handler is ~30–40 lines.

- [ ] **Step 21.1: ListMyFiles**
- [ ] **Step 21.2: DeleteFile**
- [ ] **Step 21.3: RestoreFile**
- [ ] **Step 21.4: ListTrashedFiles**
- [ ] **Step 21.5: Build + commit**

```
git commit -m "feat(files): lifecycle endpoints (list-mine, delete, restore, list-trash)"
```

---

## Task 22 — Hangfire purge jobs

**Files:**
- Create: `src/Modules/Files/Modules.Files/Jobs/PurgeOrphanedFilesJob.cs`
- Create: `src/Modules/Files/Modules.Files/Jobs/PurgeDeletedFilesJob.cs`

- [ ] **Step 22.1: PurgeOrphanedFilesJob**

```csharp
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Domain;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Files.Jobs;

public sealed class PurgeOrphanedFilesJob(
    FilesDbContext db,
    IStorageService storage,
    ILogger<PurgeOrphanedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var orphans = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.Status == FileAssetStatus.PendingUpload
                        && f.UploadDeadline != null
                        && f.UploadDeadline < DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (orphans.Count == 0) return;

        foreach (var f in orphans)
        {
            try
            {
                await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove orphan storage object {Key}", f.StorageKey);
            }
            db.FileAssets.Remove(f);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Purged {Count} orphaned file assets", orphans.Count);
    }
}
```

- [ ] **Step 22.2: PurgeDeletedFilesJob**

```csharp
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Quota;
using FSH.Framework.Storage.Services;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Files.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Files.Jobs;

public sealed class PurgeDeletedFilesJob(
    FilesDbContext db,
    IStorageService storage,
    IQuotaService quotas,
    IEventBus events,
    IOptions<FilesOptions> options,
    ILogger<PurgeDeletedFilesJob> logger)
{
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 1800 })]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.SoftDeleteRetentionDays);
        var candidates = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted && f.DeletedAt != null && f.DeletedAt < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (candidates.Count == 0) return;

        foreach (var f in candidates)
        {
            try { await storage.RemoveAsync(f.StorageKey, cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) { logger.LogWarning(ex, "Storage remove failed for {Key}", f.StorageKey); }

            // Refund quota.
            await quotas.RecordAsync(f.TenantId!, QuotaResource.StorageBytes, -f.SizeBytes, cancellationToken).ConfigureAwait(false);

            db.FileAssets.Remove(f);
            await events.PublishAsync(new FilePurgedIntegrationEvent(f.Id, f.TenantId!, f.StorageKey), cancellationToken).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Hard-purged {Count} soft-deleted file assets", candidates.Count);
    }
}
```

Create `src/Modules/Files/Modules.Files.Contracts/Events/FilePurgedIntegrationEvent.cs` (matching `FileFinalizedIntegrationEvent`'s shape).

- [ ] **Step 22.3: Build + commit**

```
git commit -m "feat(files): purge jobs (orphans hourly, soft-deleted after retention)"
```

---

## Task 23 — FilesModule.cs registration

**Files:**
- Create: `src/Modules/Files/Modules.Files/FilesModule.cs`
- Create: `src/Modules/Files/Modules.Files/AssemblyInfo.cs` (for InternalsVisibleTo + FshModule attribute)

- [ ] **Step 23.1: AssemblyInfo.cs**

```csharp
using FSH.Framework.Web.Modules;
using System.Runtime.CompilerServices;

[assembly: FshModule(typeof(FSH.Modules.Files.FilesModule), 350)]
[assembly: InternalsVisibleTo("Files.Tests")]
[assembly: InternalsVisibleTo("Integration.Tests")]
```

- [ ] **Step 23.2: FilesModule.cs**

```csharp
using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Files.Authorization;
using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Features.v1.DeleteFile;
using FSH.Modules.Files.Features.v1.FinalizeUpload;
using FSH.Modules.Files.Features.v1.GetFileContent;
using FSH.Modules.Files.Features.v1.GetFileDownloadUrl;
using FSH.Modules.Files.Features.v1.GetFileMetadata;
using FSH.Modules.Files.Features.v1.ListMyFiles;
using FSH.Modules.Files.Features.v1.ListTrashedFiles;
using FSH.Modules.Files.Features.v1.RequestUploadUrl;
using FSH.Modules.Files.Features.v1.RestoreFile;
using FSH.Modules.Files.Jobs;
using FSH.Modules.Files.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FSH.Modules.Files;

public sealed class FilesModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(FilesPermissions.All);

        builder.Services.Configure<FilesOptions>(builder.Configuration.GetSection("Files"));
        builder.Services.AddHeroDbContext<FilesDbContext>();
        builder.Services.AddScoped<IDbInitializer, FilesDbInitializer>();
        builder.Services.AddScoped<FileAccessPolicyRegistry>();
        builder.Services.AddSingleton<IFileScanner, NoOpFileScanner>();
        builder.Services.AddScoped<GetFileContentService>();
        builder.Services.AddValidatorsFromAssembly(typeof(FilesModule).Assembly);

        // Default uploader-only policies for the built-in OwnerTypes
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new DefaultUploaderOnlyPolicy("MyFiles"));
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new DefaultUploaderOnlyPolicy("User"));

        builder.Services.AddHealthChecks().AddDbContextCheck<FilesDbContext>(
            name: "db:files",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("api/v{version:apiVersion}/files")
            .WithTags("Files")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Literal routes BEFORE catch-all {id:guid} per the routing convention
        group.MapRequestUploadUrlEndpoint();        // /upload-url
        group.MapListMyFilesEndpoint();             // /mine
        group.MapListTrashedFilesEndpoint();        // /trash
        group.MapRestoreFileEndpoint();             // /{id}/restore (specific verb path)

        group.MapFinalizeUploadEndpoint();          // /{id}/finalize
        group.MapGetFileDownloadUrlEndpoint();      // /{id}/url
        group.MapGetFileContentEndpoint();          // /{id}/content
        group.MapGetFileMetadataEndpoint();         // /{id}
        group.MapDeleteFileEndpoint();              // DELETE /{id}
    }

    public void RegisterRecurringJobs(IServiceProvider sp)
    {
        // Called from the host's Hangfire init — see Program.cs wiring.
        // Recurring registrations are idempotent so this is safe to call on every startup.
    }
}
```

> **Note:** Recurring job registration pattern — see how Webhooks/Billing register their Hangfire recurring jobs. Match that pattern (likely a `JobsRegistration.cs` or similar registered via a hosted service).

- [ ] **Step 23.3: Build + commit**

```
dotnet build src/FSH.Starter.slnx --nologo
git add src/Modules/Files/Modules.Files/FilesModule.cs src/Modules/Files/Modules.Files/AssemblyInfo.cs
git commit -m "feat(files): FilesModule registration + endpoint wiring"
```

---

## Task 24 — Wire Hangfire recurring jobs

**Files:**
- Modify wherever the host wires Hangfire recurring jobs (likely `src/Host/FSH.Starter.Api/Program.cs` or a `RecurringJobsHostedService`)

- [ ] **Step 24.1: Locate the existing recurring-job registration**

Run: Grep for `RecurringJob.AddOrUpdate` across the repo.

- [ ] **Step 24.2: Add Files job registrations**

```csharp
RecurringJob.AddOrUpdate<PurgeOrphanedFilesJob>(
    "files-purge-orphans",
    job => job.RunAsync(CancellationToken.None),
    "0 * * * *"); // hourly

RecurringJob.AddOrUpdate<PurgeDeletedFilesJob>(
    "files-purge-deleted",
    job => job.RunAsync(CancellationToken.None),
    "30 3 * * *"); // daily at 03:30 UTC
```

- [ ] **Step 24.3: Commit**

```
git commit -m "feat(files): register Hangfire recurring purge jobs"
```

---

## Task 25 — Integration tests: MinIO Testcontainer + happy path

**Files:**
- Modify: `src/Tests/Integration.Tests/Infrastructure/FshWebApplicationFactory.cs` — add MinIO container
- Create: `src/Tests/Integration.Tests/Files/RequestAndFinalizeUploadTests.cs`
- Modify: `src/Tests/Integration.Tests/Integration.Tests.csproj` — add Testcontainers Minio package

- [ ] **Step 25.1: Add the package**

Run: Add `<PackageVersion Include="Testcontainers.Minio" Version="4.5.0" />` to `Directory.Packages.props`, then `<PackageReference Include="Testcontainers.Minio" />` to `Integration.Tests.csproj`.

- [ ] **Step 25.2: Wire MinIO into the factory**

In `FshWebApplicationFactory`:

```csharp
private readonly Testcontainers.Minio.MinioContainer _minio = new Testcontainers.Minio.MinioBuilder()
    .WithImage("minio/minio:latest")
    .WithUsername("minioadmin")
    .WithPassword("minioadmin")
    .WithAutoRemove(true)
    .WithCleanUp(true)
    .Build();

// in InitializeAsync: await _minio.StartAsync(); + create bucket "fsh-integration-test-uploads"
// in DisposeAsync: await _minio.DisposeAsync();
// in ConfigureAppConfiguration: add Storage:Provider=s3 + S3 sub-keys pointed at _minio
```

Full pattern: mirror the `_postgres` setup; create the bucket on `InitializeAsync` using the AWS SDK (the Storage project already references `AWSSDK.S3`).

- [ ] **Step 25.3: Write the happy-path integration test (TDD red first)**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Shouldly;

namespace Integration.Tests.Files;

public class RequestAndFinalizeUploadTests : IClassFixture<FshWebApplicationFactory>
{
    private readonly FshWebApplicationFactory _factory;
    public RequestAndFinalizeUploadTests(FshWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UploadUrl_Then_Finalize_Should_TransitionToAvailable()
    {
        var client = await _factory.AuthenticateAsRootAdminAsync();
        var bytes = new byte[256];
        Random.Shared.NextBytes(bytes);

        var upload = await client.PostAsJsonAsync("/api/v1/files/upload-url", new
        {
            ownerType = "MyFiles",
            ownerId = (Guid?)null,
            fileName = "doc.pdf",
            contentType = "application/pdf",
            sizeBytes = bytes.Length,
            visibility = 1, // Private
            category = "Document"
        });
        upload.StatusCode.ShouldBe(HttpStatusCode.OK);
        var presigned = await upload.Content.ReadFromJsonAsync<PresignedUploadResponse>();
        presigned.ShouldNotBeNull();

        // PUT bytes directly to MinIO
        using var rawClient = new HttpClient();
        var put = new HttpRequestMessage(HttpMethod.Put, presigned!.UploadUrl)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
            }
        };
        var putResp = await rawClient.SendAsync(put);
        putResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Finalize
        var finalize = await client.PostAsync($"/api/v1/files/{presigned.FileAssetId}/finalize", null);
        finalize.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await finalize.Content.ReadFromJsonAsync<FileAssetDto>();
        dto.ShouldNotBeNull();
        dto!.Status.ShouldBe(1); // Available
        dto.SizeBytes.ShouldBe(bytes.Length);
    }
}
```

> **Note:** `AuthenticateAsRootAdminAsync` is a test helper — verify the actual name used in existing integration tests (e.g., `Integration.Tests/Auth/*` or `Infrastructure/AuthExtensions.cs`).

- [ ] **Step 25.4: Run + commit**

```
dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --nologo --filter "FullyQualifiedName~Files"
git commit -m "test(files): MinIO Testcontainer + happy-path integration test"
```

---

## Task 26 — Integration tests: negative paths + policy

**Files:**
- Create: `src/Tests/Integration.Tests/Files/UploadValidationTests.cs`
- Create: `src/Tests/Integration.Tests/Files/CrossTenantTests.cs`
- Create: `src/Tests/Integration.Tests/Files/SoftDeleteAndRestoreTests.cs`
- Create: `src/Tests/Integration.Tests/Files/PurgeJobsTests.cs`

Each test class covers a slice of the negative-path matrix from spec §10. For each: 4–8 focused tests asserting:
- 400 on bad extension, 400 on oversized, 507 on quota exceeded
- 403 on `IFileAccessPolicy.CanAttachAsync` denial
- 404 on cross-tenant read (with two tenant fixtures)
- 409 on finalize-without-upload, 409 on double-finalize
- 400 + object cleanup on size mismatch
- Soft-delete makes file unreadable to non-admins, list-trash returns it, restore brings it back
- Calling `PurgeOrphanedFilesJob.RunAsync` directly with a forced past `UploadDeadline` purges
- Calling `PurgeDeletedFilesJob.RunAsync` with a forced past `DeletedAt` hard-purges + refunds quota

- [ ] **Step 26.1: UploadValidationTests**
- [ ] **Step 26.2: CrossTenantTests**
- [ ] **Step 26.3: SoftDeleteAndRestoreTests**
- [ ] **Step 26.4: PurgeJobsTests**
- [ ] **Step 26.5: Run full Files test slice**

```
dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter "FullyQualifiedName~Files"
```

- [ ] **Step 26.6: Commit**

```
git commit -m "test(files): negative paths, cross-tenant, soft-delete, purge jobs"
```

---

## Task 27 — Architecture tests

**Files:**
- Modify: `src/Tests/Architecture.Tests/<existing module rules file>` — add Files rules

- [ ] **Step 27.1: Read existing arch tests + match patterns**

Run: read 2-3 existing test classes in `src/Tests/Architecture.Tests/` to understand the NetArchTest patterns.

- [ ] **Step 27.2: Add Files rules**

Add a test class asserting:
- `Modules.Files` does not depend on `Modules.Catalog`, `Modules.Tickets`, `Modules.Billing`, etc.
- `Modules.Files.Contracts` has no runtime dependencies on `Modules.Files`
- Endpoints (classes ending `Endpoint`) live in `Features/v1/*`
- Handlers (classes ending `Handler`) implement `ICommandHandler` or `IQueryHandler`

- [ ] **Step 27.3: Commit**

```
git commit -m "test(files): architecture rules — module boundary, endpoint/handler placement"
```

---

## Task 28 — Full build, full test, doc

**Files:**
- Modify: `README.md` or `docs/` — short note pointing to spec + plan

- [ ] **Step 28.1: Full build**

```
dotnet build src/FSH.Starter.slnx --nologo
```
Expected: 0 errors, 0 warnings (per project zero-warning convention).

- [ ] **Step 28.2: Full test run**

```
dotnet test src/FSH.Starter.slnx --nologo
```
Expected: all green. Files.Tests: ~12 tests. Integration.Tests Files slice: ~15 tests. Architecture.Tests Files slice: ~4 tests.

- [ ] **Step 28.3: Verification log**

Append a session note to `memory/sessions/2026-05-12.md` summarizing what shipped and what's next (Phases B–F).

- [ ] **Step 28.4: Final commit**

```
git commit --allow-empty -m "chore(files): Phase A complete — module + storage extensions + tests"
```

---

## Self-Review

**Spec coverage check:**

| Spec section | Tasks |
|--------------|-------|
| §3.1 Project layout | Task 1 |
| §3.3 Storage block extensions | Tasks 2–6 |
| §3.4 MinIO CORS | Task 7 |
| §4 Domain model | Task 9 |
| §4 State machine | Tasks 9, 13 |
| §5.1 Upload flow | Tasks 18, 19 |
| §5.2 Failure handling | Tasks 18, 19, 26 |
| §5.3 Download flows | Task 20 |
| §6 Authorization model | Tasks 15, 16 |
| §6.1 Permissions | Task 17 |
| §7 Configuration | Task 8 |
| §8 Background jobs | Tasks 22, 24 |
| §10 Testing strategy | Tasks 12, 13, 16, 25, 26, 27 |

**Gaps acknowledged:**
- §9 Frontend integration — explicitly deferred to a follow-up Phase A.2 (`docs/superpowers/plans/2026-05-12-files-module-phase-a2-frontend.md` to be written when this plan completes). The backend Phase A produces a working API behind authentication; the kit can ship and demo via OpenAPI/Scalar without UI until A.2 lands.

**Placeholder scan:** No "TBD" / "implement later" remain. Several `> Note:` blocks call out small verification points (exact base class names, exact event interface) — those are pointers, not placeholders.

**Type consistency:** Visibility, FileAssetStatus, ScanStatus enum values are used as `int` across DTO boundary (DTO carries `(int)` cast). `FileAccessContext.Visibility` is `int` — matches the cast pattern.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-12-files-module-phase-a.md`.

Per the earlier scope conversation, execution will be **inline in this session** via `superpowers:executing-plans` — Mukesh has approved autonomous Phase A delivery. Frontend infrastructure (originally bundled into Phase A in the spec) is moved to Phase A.2 to fit one session.
