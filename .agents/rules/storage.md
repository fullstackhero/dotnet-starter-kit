# Storage & file uploads

`src/BuildingBlocks/Storage/`. Read before working with files/blobs.

## `IStorageService`

`UploadAsync<T>(FileUploadRequest, FileType, ct)`, `RemoveAsync(path, ct)`, `DownloadAsync`, `ExistsAsync`, `GetSizeAsync` (0 if absent), `GenerateUploadUrlAsync`/`GenerateDownloadUrlAsync` (presigned), `HeadObjectAsync`, `BuildPublicUrl(key)→string` (string, not Uri — local storage returns a server-relative path).

`CopyAsync(source, destination, ct)` is a server-side copy that **throws** on failure (unlike `RemoveAsync`) — callers move objects as copy → update the reference → delete. The quota decorator charges the copy, so a move is net-zero.

## Visibility lives in the key

Bucket policies in every stack (Aspire bootstrap, compose `rustfs-init`, Terraform `app_s3_public_read_prefix` + the CloudFront read statement) grant anonymous `s3:GetObject` on **`public/*` only** (`StorageVisibilityRoot`). So:
- Files keys are `{public|private}/tenants/{tenant}/{owner}/{yyyy}/{MM}/{id:N}/{name}` — `StorageKeyBuilder` (Files module) is the **one** place that decides; never hand-build a key.
- `S3StorageService.UploadAsync<T>` always returns a public URL, so it writes under `public/uploads/…`.
- Moves go through `FileStorageRelocator.ApplyAsync`, never by hand: it takes the file's row lock (`SELECT … FOR UPDATE`, PostgreSQL), re-reads the row, then copy → commit row + `FileStorageKeyChangedIntegrationEvent` → delete old. The old object is deleted only when no row references it; the first attempt runs right after the commit, and Files' own `DeleteSupersededObjectHandler` retries it via the outbox until it is really gone (so a Public → Private flip can't leave a public copy behind).
- Modules that persist a public URL must handle that event and use `ResolveUrl` (it returns the API's current `NewPublicUrl` for public files) — Catalog (product images) and Identity (avatars) do.
- Keys from before #1410 (`tenants/…`, `uploads/…`) have no root: `MigrateLegacyPublicFileKeysJob` (enqueued via `IJobService` on every API start; keyset-paginated; retried by Hangfire) moves public FileAssets under `public/`; legacy private ones stay put. It reads the partial index `IX_FileAsset_LegacyKey`, which is empty once done, so repeat runs are nearly free. `FinalizeUpload` relocates uploads that were presigned before the upgrade. Legacy `UploadAsync<T>` objects (`uploads/…`) are **not** migrated.
- `QuotaMeteredStorageService.RemoveAsync` refunds only when the object is gone afterwards (providers swallow delete errors).
- With `Storage:S3:Prefix` set, keys become `{Prefix}/public/…` — the policy must match that, and the prefix must **not** be `public` or `private`.
- The **Local** provider serves everything under wwwroot: it does not enforce `private/` at all. Use it for dev only.

`FileType`: `Image` (5MB), `Document`, `Pdf` (10MB) — `FileTypeMetadata.GetRules` enforces extension + size. **Always propagate `CancellationToken`.**

## Providers

`AddHeroStorage(config)` reads `Storage:Provider` **eagerly at registration**: `"s3"` → `S3StorageService` (supports any S3-compatible store, e.g. RustFS, via `ServiceUrl` + `ForcePathStyle`), else `LocalStorageService`. When quotas are enabled the service is wrapped in `QuotaMeteredStorageService` (debits `StorageBytes`).

`Storage:S3:PresignServiceUrl` (optional, validated at startup as an absolute http(s) URL): presigned PUT/GET URLs are signed for this host through a second, keyed `IAmazonS3` (`S3StorageService.PresignClientKey`); all real I/O stays on `ServiceUrl`. Needed when the API reaches the store on an internal address browsers can't resolve (compose: `http://rustfs:9000`). A test factory that re-registers the S3 stack must register that keyed client too (see `FshWebApplicationFactory`).

## Presigned upload flow (preferred for user uploads)

Don't stream large files through the API. The pattern (see Files module):
1. `RequestUploadUrl` — server validates category/extension/size + quota pre-check, returns a presigned PUT URL, persists a `PendingUpload` record.
2. Client uploads **directly** to storage.
3. `FinalizeUpload` — flips to `Available`, **debits the quota here** (not at request time), publishes `FileFinalizedIntegrationEvent`.

Local/dev without an S3 store uses `LocalPresignTokenStore` (in-memory one-shot tokens).

## Test gotcha

`AddHeroStorage` reads `Storage:Provider` **before** a test factory's in-memory config overlay applies, so it wires `LocalStorageService`. Integration tests that need object storage must **remove the `IStorageService`/`LocalStorageService`/`S3StorageService` descriptors post-registration and re-register the S3 stack** pointed at the RustFS container (see `FshWebApplicationFactory`). See `integration-testing.md`.
