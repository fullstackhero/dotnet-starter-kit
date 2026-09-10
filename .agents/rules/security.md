# Web security & request governance

CORS, security headers, rate limiting, idempotency, quota enforcement. `src/BuildingBlocks/Web/` + `Quota/`.
For auth/JWT/permissions see `modules/identity.md`; for the global exception handler see `api-conventions.md`.

## CORS (`Web/Cors/`) — the SignalR gotcha

Policy `FSHCorsPolicy`. When `CorsOptions.AllowAll=true` it uses **`SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`** — deliberately **NOT `AllowAnyOrigin()`**. `Access-Control-Allow-Origin: *` is illegal with credentialed requests, and **SignalR's negotiate always runs credentialed**, so `AllowAnyOrigin()` silently breaks SignalR while REST keeps working. Never "simplify" it to `AllowAnyOrigin()`. `UseHeroCors()` runs **before** `UseHttpsRedirection()` so OPTIONS preflight isn't 307-redirected.

## Security headers (`Web/Security/`)

`UseHeroSecurityHeaders()` sets `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, HSTS (HTTPS), and a CSP. `SecurityHeadersOptions.ExcludedPaths` defaults to `["/scalar","/openapi"]` (they manage their own scripts) — keep those excluded.

## Rate limiting (`Web/RateLimiting/`)

Chained partitioned fixed-window limiter: **tenant → user → IP** (defaults 1000 / 200 / 300 per 60s) + a stricter named `"auth"` policy (10/60s). Health paths are unlimited. Rejection → 429 + ProblemDetails + `Retry-After`. `RateLimitingOptions.Enabled` is read **eagerly** — when false the middleware is skipped entirely (tests set it via env var before host build).

## Idempotency (`Web/Idempotency/`)

Opt-in per endpoint with **`.WithIdempotency()`**. Reads the `Idempotency-Key` header (max 128 chars, 24h TTL); replays return the cached response with `Idempotency-Replayed: true`. Cache key is tenant-scoped (`CacheKeys.IdempotencyEntry`). Put it on POSTs that must be replay-safe (e.g. CreateTenant).

## Quota enforcement (`Quota/`)

`QuotaEnforcementMiddleware` charges 1 `ApiCalls` unit per request via `CheckAndRecordAsync`; over-limit → 429 + ProblemDetails + `Retry-After`, and sets `HttpContext.Items[QuotaRejected]` so auditing can tag it. Resources: `ApiCalls` (counter), `StorageBytes`, `Users`, `ActiveFeatureFlags` (gauges). Skips health/metrics, unresolved tenants, and the root tenant. **Pipeline:** runs after auth (needs tenant) and after the rate limiter. Inject `TimeProvider` (not `DateTimeOffset.UtcNow`) for any time math here — the subsystem is `TimeProvider`-based.

`IQuotaService`: `CheckAsync` (no mutation), `RecordAsync` (increment), `CheckAndRecordAsync` (atomic — won't increment past the limit). Store: Redis (`RedisQuotaService`) or per-process `InMemoryQuotaService` (dev/test). `NoopQuotaService` when disabled.

## Data Protection keys (`Caching/`, `Persistence/DataProtection/`)

Data Protection encrypts auth cookies, password-reset and confirmation tokens, and antiforgery
tokens. Two settings decide whether that survives.

**`DataProtection:ApplicationName`** is what keys are isolated by, so it MUST be unique per
application. It is read from configuration, never hard-coded: the framework also ships as compiled
`FSH.Framework.*` packages, where the template's token substitution cannot reach a literal, so a
constant would make every project on those packages share one key ring — and two of them against
the same store could decrypt each other's cookies and tokens. `appsettings.json` is scaffolded
source, so the value there is renamed per project in both distribution modes. Unconfigured, it
falls back to the entry assembly name, which errs towards isolation.

**`DataProtection:Store`** is `Redis` (default) or `Database`.

| | Redis | Database |
|---|---|---|
| Wired in | `AddHeroCaching` | `AddHeroPlatform` |
| Needs | a configured Redis | `DataProtectionKeysDbContext` + its migrations |
| Choose when | Redis is durable and shared by every host | hosts do not reliably share Redis, or Redis is a cache with eviction |

Pick `Database` when the DbMigrator is run standalone — outside the AppHost wiring that injects a
Redis connection string — since anything its seed encrypts otherwise becomes undecryptable by the
API (`CryptographicException: key {guid} not found in the key ring`). Redis eviction has the same
effect on a live system: losing a key takes every session and pending reset token with it.

The DbMigrator additionally creates the key table **before** it starts its host
(`DataProtectionSchema.EnsureAsync`). Data Protection resolves its key ring eagerly during
`StartAsync`, long before the migrator's own Step 0/1/2 flow, so the `IDbInitializer` alone is too
late there: the first run against an empty database logs a query failure with a stack trace and —
worse — does not fail, because a key created while the table is missing cannot be persisted, and
anything encrypted in that window is undecryptable afterwards. The initializer still covers the API
and the test harness, which migrate before they serve.

`DataProtectionKeysDbContext` is a plain `DbContext`, not `BaseDbContext` — keys are global
framework infrastructure, not tenant data. It registers a `DataProtectionKeysDbInitializer` like
every other framework context, which is what makes the table appear in the API, the DbMigrator and
the integration-test harness alike. **Wiring the context without its `IDbInitializer` is the
failure mode to avoid**: the table then only exists wherever someone migrated it by hand, and every
flow that protects a payload fails with `Invalid object name 'DataProtectionKeys'` everywhere else.

