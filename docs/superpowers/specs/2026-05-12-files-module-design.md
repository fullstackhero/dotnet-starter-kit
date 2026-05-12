# Files Module — Design Spec

**Date:** 2026-05-12
**Status:** Approved for implementation (Phase A this session, Phases B–F follow-up)
**Author:** Mukesh Murugan (via brainstorming with Claude)

---

## 1. Goal

Add a first-class **Files module** to the FullStackHero starter kit so that any owning feature (Catalog products, Tickets, "My Files", user avatars, tenant logos) can upload, manage, and serve files through a uniform, tenant-isolated, quota-metered, authorization-checked API. Ship a reusable upload component in both frontends so the kit demonstrates the full end-to-end pattern.

## 2. Decisions Locked In

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | First-class `FileAsset` entity (not inline URLs) | Lifecycle, metadata, orphan tracking, audit. Inline-URL pattern (avatars/logos today) doesn't scale to ticket attachments. |
| D2 | Per-file `Visibility = Public \| Private` | Product images public, ticket attachments private. Matches the dual reality of SaaS files. |
| D3 | Pre-signed PUT URL upload flow (browser → S3 directly) | Scales beyond API body limits, zero API bandwidth on bytes, production pattern. |
| D4 | No-op `IFileScanner` interface in v1 | Hook point only; downstream wires ClamAV / GuardDuty later. Async-aware. |
| D5 | `IFileAccessPolicy` per `OwnerType` | Each owning module owns its rule; Files module stays generic. |
| D6 | My-Files = uploader-only by default | User-scoped within tenant; flip `Visibility=Public` to share with tenant. |
| D7 | Soft-delete row, async byte purge after retention | Matches Catalog/Tickets pattern; supports Trash UI; quota refund on hard purge. |
| D8 | Clean-break migration for existing avatar + tenant-logo | EF migration HEADs each existing URL, creates FileAsset rows, drops URL columns. |
| D9 | Files placed as a **Module** (peer to Catalog/Tickets), not a BuildingBlock | Has own DbContext, entity, endpoints, contracts. Matches Auditing/Webhooks pattern. |
| D10 | Sync finalize processing (Approach A) | No-op scanner is sync; interface is async-aware; matches existing handler conventions. |
| D11 | `BuildingBlocks/Storage` may be extended (explicit approval) | New methods for presigned URL minting + HEAD; existing surface preserved. |

## 3. Architecture

### 3.1 Project layout

```
src/Modules/Files/
├── Modules.Files/                              # runtime (internal)
│   ├── Domain/
│   │   ├── FileAsset.cs                        # AggregateRoot
│   │   ├── FileAssetStatus.cs                  # PendingUpload | Available | Quarantined | SoftDeleted
│   │   ├── Visibility.cs                       # Public | Private
│   │   ├── ScanStatus.cs                       # NotScanned | Clean | Infected | ScanFailed
│   │   └── Events/
│   │       ├── FileFinalizedDomainEvent.cs
│   │       ├── FileSoftDeletedDomainEvent.cs
│   │       └── FilePurgedDomainEvent.cs
│   ├── Data/
│   │   ├── FilesDbContext.cs
│   │   └── FileAssetConfiguration.cs
│   ├── Features/v1/
│   │   ├── RequestUploadUrl/{Endpoint, Handler, Validator}.cs
│   │   ├── FinalizeUpload/{Endpoint, Handler, Validator}.cs
│   │   ├── GetFileMetadata/{Endpoint, Handler}.cs
│   │   ├── GetFileDownloadUrl/{Endpoint, Handler}.cs
│   │   ├── GetFileContent/{Endpoint, Handler}.cs       # proxy fallback
│   │   ├── ListMyFiles/{Endpoint, Handler}.cs
│   │   ├── DeleteFile/{Endpoint, Handler, Validator}.cs
│   │   ├── RestoreFile/{Endpoint, Handler}.cs
│   │   └── ListTrashedFiles/{Endpoint, Handler}.cs
│   ├── Services/
│   │   ├── IFileScanner.cs
│   │   ├── NoOpFileScanner.cs
│   │   ├── FileAccessPolicyRegistry.cs
│   │   └── StorageKeyBuilder.cs
│   ├── Authorization/
│   │   ├── FilesPermissionConstants.cs
│   │   └── DefaultUploaderOnlyPolicy.cs         # for "MyFiles" and "User" owner types
│   ├── Jobs/
│   │   ├── PurgeOrphanedFilesJob.cs
│   │   └── PurgeDeletedFilesJob.cs
│   ├── FilesOptions.cs
│   └── FilesModule.cs                           # [FshModule(Order = 2)]  — after Identity (0) and Multitenancy (1)
└── Modules.Files.Contracts/                     # public API
    ├── v1/
    │   ├── Commands/
    │   │   ├── RequestUploadUrlCommand.cs
    │   │   ├── FinalizeUploadCommand.cs
    │   │   ├── DeleteFileCommand.cs
    │   │   └── RestoreFileCommand.cs
    │   ├── Queries/
    │   │   ├── GetFileMetadataQuery.cs
    │   │   ├── GetFileDownloadUrlQuery.cs
    │   │   ├── ListMyFilesQuery.cs
    │   │   └── ListTrashedFilesQuery.cs
    │   └── DTOs/
    │       ├── FileAssetDto.cs
    │       ├── PresignedUploadResponse.cs
    │       └── PresignedDownloadResponse.cs
    ├── IFileAccessPolicy.cs                     # owning modules implement this
    ├── FileAssetReference.cs                    # value object: { Id, OwnerType, OwnerId }
    └── Events/
        ├── FileFinalizedIntegrationEvent.cs
        └── FilePurgedIntegrationEvent.cs
```

**Module order:** Files = 2. Identity = 0, Multitenancy = 1, Files = 2. Catalog, Tickets, etc. follow at order ≥ 3 and register their `IFileAccessPolicy` implementations against the Files registry during their own `ConfigureServices`.

**Migrations:** added under existing `FSH.Starter.Migrations.PostgreSQL`. No new migrations project.

### 3.2 Tenant scoping

- `FileAsset.TenantId` non-null.
- Finbuckle's anonymous tenant query filter applies automatically via the framework base DbContext.
- Soft-delete uses the **named** `SoftDelete` filter per `decisions/2026-04-30-named-query-filters.md` — cross-tenant admin / trash queries opt out with `IgnoreQueryFilters().Where(...explicit re-filter...)`.

### 3.3 Storage block extensions (`BuildingBlocks/Storage`)

`IStorageService` grows three methods; existing surface preserved:

```csharp
Task<PresignedUploadUrl> GenerateUploadUrlAsync(
    string storageKey, string contentType, long maxBytes,
    TimeSpan ttl, CancellationToken ct);

Task<Uri> GenerateDownloadUrlAsync(
    string storageKey, TimeSpan ttl,
    string? responseContentDisposition = null, CancellationToken ct = default);

Task<StoredObjectMetadata?> HeadObjectAsync(
    string storageKey, CancellationToken ct);
```

New DTOs in `FSH.Framework.Shared.Storage`:

```csharp
public sealed record PresignedUploadUrl(Uri Url, IReadOnlyDictionary<string, string> RequiredHeaders, DateTimeOffset ExpiresAt);
public sealed record StoredObjectMetadata(long SizeBytes, string ContentType, DateTimeOffset LastModified, string? ETag);
```

**Implementations:**
- `S3StorageService` — `GetPreSignedURLAsync` with `Verb=HTTP_PUT`, content-type header constraint, content-length-range condition; `GetObjectMetadataAsync` for HEAD.
- `LocalStorageService` — issues `/api/v1/files/_local-upload/{token}` URLs. A small middleware (only registered when `Storage:Provider != s3`) handles the PUT and writes to disk. Local dev/test fallback only.
- `QuotaMeteredStorageService` — presigning and HEAD pass through unmetered. Existing `UploadAsync`/`RemoveAsync` quota behavior unchanged.

### 3.4 AppHost / MinIO CORS

`minio-init` container script extended:

```sh
mc alias set local http://minio:9000 "$MC_USER" "$MC_PASS"
mc mb --ignore-existing local/fsh-uploads
mc anonymous set download local/fsh-uploads     # (kept for Public visibility files)
# CORS: allow PUT/GET/HEAD from admin and dashboard origins
mc admin config set local cors_allow_origin="$ADMIN_ORIGIN,$DASHBOARD_ORIGIN"
mc admin config set local cors_allow_methods="GET,PUT,HEAD,POST"
mc admin service restart local
```

`AppHost.cs` passes `ADMIN_ORIGIN=http://localhost:5173` and `DASHBOARD_ORIGIN=http://localhost:5174` to `minio-init` as env vars.

For production S3 deployments, the bucket CORS config is documented in the README; the kit doesn't auto-configure real S3.

## 4. Domain Model

```csharp
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
    // + inherited: Id, TenantId, CreatedAt, UpdatedAt, IsDeleted

    public static FileAsset CreatePending(...);   // factory
    public void MarkAvailable(long actualSize, ScanStatus scan);
    public void MarkQuarantined();
    public void SoftDelete(string actorUserId);
    public void Restore();
}
```

**State machine:**

```
                 ┌────────────────────┐
   create        │  PendingUpload     │
  ───────────▶   │  (UploadDeadline)  │
                 └────────┬───────────┘
                          │ finalize
              ┌───────────┼───────────┐
              │           │           │
        scan=Clean    scan=Infected   no-upload + deadline passed
              │           │           │
              ▼           ▼           ▼
         ┌─────────┐ ┌─────────────┐ (purged by PurgeOrphanedFilesJob)
         │Available│ │ Quarantined │
         └────┬────┘ └─────────────┘
              │ DELETE
              ▼
         ┌────────────┐ restore  ┌─────────┐
         │SoftDeleted │ ───────▶ │Available│
         └────┬───────┘          └─────────┘
              │ DeletedAt + retention < now (PurgeDeletedFilesJob)
              ▼
         (row + bytes hard-deleted)
```

**Indexes:**
- `IX_FileAsset_Tenant_OwnerType_OwnerId` — `(TenantId, OwnerType, OwnerId)` for owner lookups
- `IX_FileAsset_Tenant_Status` — `(TenantId, Status)` for status queries
- `IX_FileAsset_DeletedAt` — `(IsDeleted, DeletedAt)` for the purge job (no tenant filter — job iterates across all tenants)
- `UX_FileAsset_StorageKey` — unique on `StorageKey`

**Storage key shape:**
`tenants/{tenantId}/{ownerType-lower}/{yyyy}/{MM}/{fileAssetId:N}/{sanitized-filename}`

Tenant prefix is mandatory defense-in-depth even with bucket-level isolation.

## 5. API Surface (v1)

| Method | Path | Permission | Purpose |
|--------|------|-----------|---------|
| POST | `/api/v1/files/upload-url` | `Files.Upload` + `IFileAccessPolicy.CanAttachAsync` | Mint presigned PUT, create PendingUpload row |
| POST | `/api/v1/files/{id}/finalize` | (must own pending row) | HEAD object, validate, scan, transition to Available |
| GET | `/api/v1/files/{id}` | `IFileAccessPolicy.CanReadAsync` | Metadata (+ direct URL if Public) |
| GET | `/api/v1/files/{id}/url` | `IFileAccessPolicy.CanReadAsync` | Mint short-lived presigned GET (5-min TTL) |
| GET | `/api/v1/files/{id}/content` | `IFileAccessPolicy.CanReadAsync` | Proxy bytes through API (fallback) |
| GET | `/api/v1/files/mine` | `Files.Upload` | List caller's files (paginated, filterable) |
| DELETE | `/api/v1/files/{id}` | `Files.DeleteOwn` or `Files.DeleteAny` | Soft-delete |
| POST | `/api/v1/files/{id}/restore` | `Files.Restore` (admin) | Restore from trash |
| GET | `/api/v1/files/trash` | `Files.ViewTrash` (admin) | List soft-deleted files (tenant-scoped) |

### 5.1 Upload flow (sequence)

```
Browser                                API                                MinIO/S3
   │                                    │                                    │
   │  POST /files/upload-url            │                                    │
   ├───────────────────────────────────▶│                                    │
   │  { ownerType, ownerId, fileName,   │                                    │
   │    contentType, sizeBytes,         │                                    │
   │    visibility, category }          │                                    │
   │                                    │  Validate:                         │
   │                                    │   - extension whitelist            │
   │                                    │   - size ≤ category max            │
   │                                    │   - IFileAccessPolicy.CanAttach    │
   │                                    │   - Quota CheckAsync (no debit)    │
   │                                    │  Insert FileAsset (PendingUpload,  │
   │                                    │   UploadDeadline = now+15min)      │
   │                                    │  GenerateUploadUrlAsync            │
   │                                    │◀───── presigned PUT URL ───────────┤
   │  202 { fileAssetId, uploadUrl,     │                                    │
   │        requiredHeaders, expiresAt }│                                    │
   │◀───────────────────────────────────┤                                    │
   │                                                                         │
   │  PUT {uploadUrl}  (bytes)                                               │
   ├────────────────────────────────────────────────────────────────────────▶│
   │  200 OK                                                                 │
   │◀────────────────────────────────────────────────────────────────────────┤
   │                                                                         │
   │  POST /files/{id}/finalize         │                                    │
   ├───────────────────────────────────▶│                                    │
   │                                    │  HeadObjectAsync                   │
   │                                    ├───────────────────────────────────▶│
   │                                    │◀────── size, content-type ─────────┤
   │                                    │  Verify size ≤ declared+1%         │
   │                                    │  Verify content-type matches       │
   │                                    │  IFileScanner.ScanAsync            │
   │                                    │  Quota RecordAsync(+actualBytes)   │
   │                                    │  Transition Status=Available       │
   │                                    │  Publish FileFinalizedIntegration  │
   │  200 { FileAssetDto }              │                                    │
   │◀───────────────────────────────────┤                                    │
```

### 5.2 Failure handling

| Failure | Response |
|---------|----------|
| Invalid extension / oversize on `upload-url` | 400 ProblemDetails |
| `IFileAccessPolicy.CanAttachAsync` returns false | 403 |
| Quota pre-check fails | 507 (insufficient storage, matches `QuotaMeteredStorageService`) |
| Browser PUT fails or never happens | Row stays `PendingUpload`; purged by orphan job after `UploadDeadline` |
| `HeadObjectAsync` returns null in finalize | 409 (object not uploaded) |
| Size/content-type mismatch in finalize | 400 + best-effort `RemoveAsync` of the object |
| Scanner returns `Infected` | Row → `Quarantined`; bytes kept for forensics until manual purge |
| Double-finalize | 409 (already in non-PendingUpload state) |
| Cross-tenant read attempt | 404 (don't leak existence) |

### 5.3 Download flow

- **Public file:** `GET /files/{id}` returns metadata including a direct bucket/CDN URL. UI uses it as `<img src=...>`. No per-request API call.
- **Private file:** UI calls `GET /files/{id}/url`. After `IFileAccessPolicy.CanReadAsync`, API mints a 5-min presigned GET with `response-content-disposition=attachment; filename="{originalFileName}"`. UI follows redirect or sets `href`.
- **Proxy fallback:** `GET /files/{id}/content` streams bytes through the API after the same policy check. For clients that can't follow presigned URLs.

## 6. Authorization Model

```csharp
public interface IFileAccessPolicy
{
    string OwnerType { get; }
    Task<bool> CanAttachAsync(Guid? ownerId, ClaimsPrincipal user, CancellationToken ct);
    Task<bool> CanReadAsync(FileAsset file, ClaimsPrincipal user, CancellationToken ct);
    Task<bool> CanDeleteAsync(FileAsset file, ClaimsPrincipal user, CancellationToken ct);
}
```

`FileAccessPolicyRegistry` resolves policies by `OwnerType`. Owning modules register via extension method:

```csharp
services.AddFileAccessPolicy<ProductImagePolicy>();
```

**Built-in policies (Phase A):**
- `DefaultUploaderOnlyPolicy` — handles `OwnerType="MyFiles"` and `OwnerType="User"`. Attach: any authenticated user (own scope). Read: `Visibility=Public` → anyone in tenant; `Visibility=Private` → only `CreatedByUserId == currentUser`. Delete: only `CreatedByUserId == currentUser` or `Files.DeleteAny`.

**Registry behavior on unknown `OwnerType`:** return false (closed by default). Owning modules must register explicitly.

**Tenant scope** is always enforced by the registry layer regardless of per-policy outcome — never delegated.

### 6.1 Permissions

```csharp
public static class FilesPermissionConstants
{
    public const string Upload     = "Permissions.Files.Upload";
    public const string DeleteOwn  = "Permissions.Files.DeleteOwn";
    public const string DeleteAny  = "Permissions.Files.DeleteAny";
    public const string ViewTrash  = "Permissions.Files.ViewTrash";
    public const string Restore    = "Permissions.Files.Restore";
}
```

## 7. Configuration

```jsonc
// appsettings.json
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

`FileTypeMetadata.GetRules(FileType)` (existing in Storage block) is now legacy — the Files module reads from `FilesOptions.Categories`. The Storage block enum stays for backward compat with avatar/logo flows until Phase E/F migration.

## 8. Background Jobs

**`PurgeOrphanedFilesJob`** — Hangfire recurring, hourly.
- Query: `WHERE Status = PendingUpload AND UploadDeadline < NOW()`.
- For each: best-effort `RemoveAsync(storageKey)` (object may not exist if browser never uploaded), hard-delete row.
- No quota effect (never debited).
- Iterates across all tenants — bypasses tenant filter (`IgnoreQueryFilters`).

**`PurgeDeletedFilesJob`** — Hangfire recurring, daily.
- Query: `WHERE IsDeleted = true AND DeletedAt < NOW() - {SoftDeleteRetentionDays}`.
- For each: `RemoveAsync(storageKey)` (debits quota refund via `QuotaMeteredStorageService`), hard-delete row.
- Publishes `FilePurgedIntegrationEvent` (so owning modules can react if needed).
- Iterates across all tenants.

Job registration in `FilesModule.ConfigureServices` via the existing `BuildingBlocks/Jobs` Hangfire wiring.

## 9. Frontend Integration

**Shared infrastructure** (Phase A, both `clients/admin` and `clients/dashboard`):

```
src/lib/files/
├── useFileUpload.ts            # presigned PUT + finalize + progress + retry
├── useFileDownloadUrl.ts       # fetch + cache short-lived GET URL
├── components/
│   ├── FileDropzone.tsx
│   ├── FileGallery.tsx         # image-specific, used by Catalog Product images
│   ├── AttachmentList.tsx      # non-image, used by Tickets + MyFiles
│   └── UploadProgress.tsx
└── api.ts                      # typed wrappers around /api/v1/files/*
```

`useFileUpload({ ownerType, ownerId, visibility, category, file })`:
1. POST `/files/upload-url` → returns `{ fileAssetId, uploadUrl, requiredHeaders, expiresAt }`.
2. `XMLHttpRequest` PUT to `uploadUrl` with `requiredHeaders` set; `upload.onprogress` drives progress state.
3. On PUT success: POST `/files/{id}/finalize`. Retry up to 3× on network errors (presigned URL has already been used, so retry is finalize-only).
4. Returns `{ fileAsset, progress, status: 'idle' | 'uploading' | 'finalizing' | 'done' | 'error', cancel() }`.

**Per-phase wiring** (Phases B–F):
- Phase B: `ProductImagesPanel` (admin/catalog/products edit page) consumes `FileDropzone` + `FileGallery`.
- Phase C: `TicketAttachmentsPanel` (dashboard/tickets detail) consumes `FileDropzone` + `AttachmentList`.
- Phase D: `MyFilesPage` (dashboard) = `FileDropzone` over `AttachmentList` filtered to `OwnerType=MyFiles`.
- Phase E: existing profile picture upload UI swapped to `useFileUpload`.
- Phase F: existing tenant branding upload UI swapped to `useFileUpload`.

## 10. Testing Strategy

**Integration tests** in `src/Tests/Files.Tests` (new) using `FshWebApplicationFactory`:

| Scenario | Coverage |
|----------|----------|
| Happy path | upload-url → simulated PUT → finalize → get metadata → get download URL → delete → list trash → restore → delete again → fast-forward purge job → row + bytes gone |
| Extension reject | upload-url returns 400 for `.exe` |
| Size reject | upload-url returns 400 for sizeBytes > category max |
| Quota reject | upload-url returns 507 when quota CheckAsync says no |
| Policy reject (attach) | upload-url returns 403 when policy says no |
| Cross-tenant read | GET `/files/{id}` returns 404 for tenant B when row owned by tenant A |
| Cross-tenant download URL | same as above |
| Finalize without upload | 409 |
| Finalize size mismatch | 400 + object cleanup |
| Finalize content-type mismatch | 400 + object cleanup |
| Double finalize | 409 |
| Orphan purge | row past UploadDeadline removed by job |
| Soft-deleted purge | row past retention hard-deleted, quota refunded |
| Scanner Infected | row transitions to Quarantined |

**Test infrastructure:** `FshWebApplicationFactory` currently provisions only Postgres via Testcontainers (no object storage). Phase A adds a Testcontainers `minio/minio` instance to the factory, sets `Storage:Provider=s3` + bucket / endpoint / credentials env vars, and bootstraps the bucket on `InitializeAsync` so tests exercise the real presigned URL path. Existing avatar/logo tests that use `LocalStorageService` continue to work because they don't go through the new Files module endpoints.

**Unit tests:**
- `FileAccessPolicyRegistry` resolution + tenant guard
- `DefaultUploaderOnlyPolicy` rules
- `FileAsset` aggregate state-machine transitions (reject illegal transitions)
- `StorageKeyBuilder` produces tenant-prefixed keys

**Architecture tests** (`NetArchTest`): `Modules.Files` does not reference any other module's runtime project.

## 11. Phasing

| Phase | Scope | Mergeable on its own? |
|-------|-------|----------------------|
| **A** | Files module + Storage block extensions + MinIO CORS + integration tests + frontend `lib/files` infrastructure. No owning-feature wiring. | Yes — ships behind feature flag if needed; endpoints exist, no consumers. |
| **B** | Catalog Product images: `ProductImage` join, endpoints, admin UI gallery, tests. | Yes — depends on A. |
| **C** | Tickets attachments: `TicketAttachment` join on Ticket + TicketComment, endpoints, dashboard UI, tests. | Yes — depends on A. |
| **D** | My Files dashboard page. Pure UI on existing endpoints. | Yes — depends on A. |
| **E** | Avatar clean-break migration: `FshUser.AvatarFileAssetId`, EF data migration, `UserProfileService` refactor, UI swap. | Yes — depends on A. Migration is the risk surface. |
| **F** | Tenant logo clean-break migration: `TenantTheme.{Logo,LogoDark,Favicon}FileAssetId`, EF data migration, `TenantThemeService` refactor, UI swap. | Yes — depends on A. |

## 12. Out of Scope (Explicit)

- Server-side image transforms / thumbnails
- EXIF stripping
- Hash-based dedup (`Sha256` column is reserved but unused in v1)
- Per-tenant file-category overrides
- Pre-signed multipart upload for >100MB files
- Real AV scanning (interface only — `IFileScanner` no-op default)
- Public S3 bucket auto-configuration outside MinIO
- File versioning / history
- Folder hierarchy in My Files (flat list only in v1)

## 13. Risks

| Risk | Mitigation |
|------|-----------|
| MinIO CORS misconfig breaks browser uploads in dev | AppHost init script tested; documented troubleshooting in README |
| Presigned URL leaks via referrer headers | Use `response-content-disposition` + short TTL (5 min for download, 15 for upload) |
| Orphan rows accumulate if Hangfire stops | Orphan job runs hourly; manual `/api/v1/files/admin/purge-orphans` (admin perm) for backstop |
| Clean-break migration (E/F) fails on missing legacy objects | Migration logs each 404 + leaves `AvatarFileAssetId=null`; idempotent re-run safe |
| `BuildingBlocks/Storage` change breaks downstream consumers | New methods only; existing surface preserved. Existing tests gate the change. |
| Scanner adds latency at finalize | No-op default is instant; once real scanner is wired, can move to async via `Scanning` status |

---

**This spec is approved for Phase A implementation. Phases B–F will reference back to this document; their own implementation plans will be created when picked up.**
