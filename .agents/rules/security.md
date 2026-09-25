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

Opt-in per endpoint with **`.WithIdempotency()`** on **authenticated** POST/PUTs that must be replay-safe (e.g. CreateTenant). Reads `Idempotency-Key` (max 128 chars, `DefaultTtl` 24h); a replay returns the cached status + body + `Location`/`ETag` and `Idempotency-Replayed: true`.

- **Key scope:** resolved tenant (never the raw `tenant` header) + operation (method + route pattern) + route values + caller (`GetUserId()`, else `"anon"`), via `CacheKeys.IdempotencyEntry`.
- **Only 2xx is stored**, before the body is sent, on `CancellationToken.None`. Probe and write must share one `IDistributedCache` + key + serializer, or replay silently never engages.
- **Concurrent duplicates:** in-flight reservation under a `lock:` prefix (Redis `SET NX`, else in-process, `ReservationTtl` default 1m); cache re-probed after the lock; duplicate in flight → **409** + `Retry-After: 1`. Probe, reserve and release all fail open.
- **`RequestAborted` is detached** while the handler runs, so a client disconnect can't cancel a committed side effect. Keep these handlers short; **never** on streaming or large-file endpoints (the response is buffered).
- **Never on `AllowAnonymous()`** endpoints — all anonymous callers share `"anon"`. `IdempotencyWiringTests` fails the build if one appears.

## Quota enforcement (`Quota/`)

`QuotaEnforcementMiddleware` charges 1 `ApiCalls` unit per request via `CheckAndRecordAsync`; over-limit → 429 + ProblemDetails + `Retry-After`, and sets `HttpContext.Items[QuotaRejected]` so auditing can tag it. Resources: `ApiCalls` (counter), `StorageBytes`, `Users`, `ActiveFeatureFlags` (gauges). Skips health/metrics, unresolved tenants, and the root tenant. **Pipeline:** runs after auth (needs tenant) and after the rate limiter. Inject `TimeProvider` (not `DateTimeOffset.UtcNow`) for any time math here — the subsystem is `TimeProvider`-based.

`IQuotaService`: `CheckAsync` (no mutation), `RecordAsync` (increment), `CheckAndRecordAsync` (atomic — won't increment past the limit). Store: Redis (`RedisQuotaService`) or per-process `InMemoryQuotaService` (dev/test). `NoopQuotaService` when disabled.
