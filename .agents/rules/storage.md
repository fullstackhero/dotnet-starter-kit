# Storage & file uploads

`src/BuildingBlocks/Storage/`. Read before working with files/blobs.

## `IStorageService`

`UploadAsync<T>(FileUploadRequest, FileType, ct)`, `RemoveAsync(path, ct)`, `DownloadAsync`, `ExistsAsync`, `GetSizeAsync` (0 if absent), `GenerateUploadUrlAsync`/`GenerateDownloadUrlAsync` (presigned), `HeadObjectAsync`, `BuildPublicUrl(key)→string` (string, not Uri — local storage returns a server-relative path).

`FileType`: `Image` (5MB), `Document`, `Pdf` (10MB) — `FileTypeMetadata.GetRules` enforces extension + size. **Always propagate `CancellationToken`.**

## Providers

`AddHeroStorage(config)` reads `Storage:Provider` **eagerly at registration**: `"s3"` → `S3StorageService` (supports MinIO via `ServiceUrl` + `ForcePathStyle`), else `LocalStorageService`. When quotas are enabled the service is wrapped in `QuotaMeteredStorageService` (debits `StorageBytes`).

## Presigned upload flow (preferred for user uploads)

Don't stream large files through the API. The pattern (see Files module):
1. `RequestUploadUrl` — server validates category/extension/size + quota pre-check, returns a presigned PUT URL, persists a `PendingUpload` record.
2. Client uploads **directly** to storage.
3. `FinalizeUpload` — flips to `Available`, **debits the quota here** (not at request time), publishes `FileFinalizedIntegrationEvent`.

Local/dev without MinIO uses `LocalPresignTokenStore` (in-memory one-shot tokens).

## Test gotcha

`AddHeroStorage` reads `Storage:Provider` **before** a test factory's in-memory config overlay applies, so it wires `LocalStorageService`. Integration tests that need MinIO must **remove the `IStorageService`/`LocalStorageService`/`S3StorageService` descriptors post-registration and re-register the S3 stack** pointed at the MinIO container (see `FshWebApplicationFactory`). See `integration-testing.md`.
