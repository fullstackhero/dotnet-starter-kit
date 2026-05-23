# Integration testing

`src/Tests/Integration.Tests/` + `Integration.Middleware.Tests/`. Read before writing tests that touch the DB/HTTP pipeline. See `testing.md` for unit conventions.

## Harness

`WebApplicationFactory` over **real** infra via Testcontainers — PostgreSQL + Redis + MinIO. **Docker must be running**; if it isn't, tests fail fast with `DockerUnavailableException` (environmental, not a regression — run the unit projects instead).

`FshWebApplicationFactory` (`Integration.Tests/Infrastructure/`) boots the containers, overlays in-memory config, swaps `IMailService` → `NoOpMailService`, and rewires storage to MinIO.

## Must-know gotchas

- **Tenant context is AsyncLocal — set it inline.** Set the Finbuckle tenant context **in the same method** as the `UserManager`/`DbContext` call. Setting it in an awaited helper loses it across the async boundary → NRE in the tenant query filter.
- **Storage is wired eagerly.** `AddHeroStorage` reads `Storage:Provider` before the test config overlay, so it picks `LocalStorageService`. The factory **removes the `IStorageService`/`LocalStorageService`/`S3StorageService` descriptors post-registration and re-registers the S3 stack** at MinIO. Follow that when a test needs real object storage. (See `storage.md`.)
- **SignalR tests force long-polling** — TestServer has no WebSocket. Configure the client transport accordingly.
- **Rate limiting is read eagerly** — `Integration.Middleware.Tests` sets `RateLimitingOptions:Enabled` via env var **before** host build, since flipping it after has no effect.

## Coverage

```bash
dotnet test --collect "XPlat Code Coverage" --settings coverage.runsettings
```
Cobertura; includes `[FSH.Modules.*]` + `[FSH.Framework.*]`; excludes tests, the Migrations project, and `*HostedService`.
