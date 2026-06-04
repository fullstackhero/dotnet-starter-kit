# Module: Files

Presigned-URL file lifecycle (upload → finalize → serve → delete) shared by Catalog images, Chat attachments, avatars. Module `Order = 350` (loads before consumer modules).

**Entities / DbContext:** `FileAsset` (aggregate, soft-deletable): status `PendingUpload → Available | Quarantined`, `Visibility` (Public/Private), `ScanStatus`. `FilesDbContext`. Publishes `FileFinalizedIntegrationEvent`.
**Areas:** RequestUploadUrl, FinalizeUpload, GetFileDownloadUrl/Metadata, ChangeVisibility, Delete/Restore, ListMy/Shared/Trashed. Purge jobs (orphaned hourly, deleted daily). Full list: `Features/v1/` or `/scalar`. Storage mechanics: `storage.md`.

## Gotchas

- **Presigned flow** — never stream uploads through the API. RequestUploadUrl validates category/extension/size + quota **pre-check** and persists a `PendingUpload`; client uploads directly to storage; **FinalizeUpload debits the quota** (not at request time) and flips to Available/Quarantined.
- **`FileAccessPolicyRegistry`** resolves `IFileAccessPolicy` by **OwnerType** — case-insensitive, **closed by default** (unknown OwnerType → forbidden), **last-write-wins** on duplicates (intentional, for test substitution). Each owning module registers its own policy in its `ConfigureServices` (Catalog/Tickets load after Files). Files ships `DefaultUploaderOnlyPolicy` for built-in OwnerTypes `"MyFiles"`/`"User"`.
- `CanChangeVisibilityAsync` defaults to the delete rule (uploader-only); domain-bound files (e.g. product images) may override to forbid visibility flips.
- Tenant scoping is implicit via `BaseDbContext` (no explicit `TenantId` on `FileAsset`).

To support uploads for a new owner type: implement `IFileAccessPolicy`, register it in the owning module, and use that OwnerType in RequestUploadUrl.
