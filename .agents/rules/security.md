# Web security & request governance

CORS, security headers, rate limiting, idempotency, quota enforcement. `src/BuildingBlocks/Web/` + `Quota/`.
For auth/JWT/permissions see `modules/identity.md`; for the global exception handler see `api-conventions.md`.

## CORS (`Web/Cors/`) — the SignalR gotcha

Policy `FSHCorsPolicy`. When `CorsOptions.AllowAll=true` it uses **`SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`** — deliberately **NOT `AllowAnyOrigin()`**. `Access-Control-Allow-Origin: *` is illegal with credentialed requests, and **SignalR's negotiate always runs credentialed**, so `AllowAnyOrigin()` silently breaks SignalR while REST keeps working. Never "simplify" it to `AllowAnyOrigin()`. `UseHeroCors()` runs **before** `UseHttpsRedirection()` so OPTIONS preflight isn't 307-redirected.

## Front-end origin for outbound links (`Web/Frontend/`)

`IFrontendOriginResolver` builds the origin of user-facing links sent in e-mails (password reset, e-mail confirmation). Backed by **`FrontendOptions`**, deliberately separate from `CorsOptions`: CORS governs which browsers may *call* the API, this governs which origins may appear *inside an outbound link*. Never merge the two lists — coupling them breaks same-origin/reverse-proxy topologies and overloads a security boundary.

- **`ResolveForCurrentRequest()`** — self-service flows only (forgot-password, self-register), where the caller *is* the recipient. Validates the request `Origin` against `FrontendOptions:AllowedOrigins` and returns the **canonical configured entry**, never the client's string. Present-but-unlisted against a non-empty list → `CustomException(HttpStatusCode.BadRequest)`: these endpoints are anonymous, so a forged header must never reach an e-mail. No `Origin` header, or an empty/all-unparseable list, falls through to the default.
- **`ResolveDefault()`** — operator-driven flows (admin register, resend-confirmation) and background jobs, where the caller is **not** the recipient. Chain: `FrontendOptions:DefaultOrigin` → `OriginOptions:OriginUrl` → the current request's host → throw. Never the caller's `Origin`, or an operator would send a tenant user a link into the admin console.

Picking the wrong method is a silent bug — both compile and both return a plausible origin. Match the method to **who receives the link**, not to who sent the request.

No `ValidateOnStart` on `FrontendOptions`, on purpose: a deployment that never sends such a link must not be taken down by the setting. `UseHeroPlatform` logs one startup `Warning` instead, counted after normalization so an all-typo list reports as the empty list it effectively is. `OriginOptions:OriginUrl` keeps its own job — the API's own public base for back-end-served assets (avatars), read through `IRequestContext.Origin`.

## Security headers (`Web/Security/`)

`UseHeroSecurityHeaders()` sets `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, HSTS (HTTPS), and a CSP. `SecurityHeadersOptions.ExcludedPaths` defaults to `["/scalar","/openapi"]` (they manage their own scripts) — keep those excluded.

## Rate limiting (`Web/RateLimiting/`)

Chained partitioned fixed-window limiter: **tenant → user → IP** (defaults 1000 / 200 / 300 per 60s) + a stricter named `"auth"` policy (10/60s). Health paths are unlimited. Rejection → 429 + ProblemDetails + `Retry-After`. `RateLimitingOptions.Enabled` is read **eagerly** — when false the middleware is skipped entirely (tests set it via env var before host build).

## Idempotency (`Web/Idempotency/`)

Opt-in per endpoint with **`.WithIdempotency()`**. Reads the `Idempotency-Key` header (max 128 chars, 24h TTL); replays return the cached response with `Idempotency-Replayed: true`. Cache key is tenant-scoped (`CacheKeys.IdempotencyEntry`). Put it on POSTs that must be replay-safe (e.g. CreateTenant).

## Quota enforcement (`Quota/`)

`QuotaEnforcementMiddleware` charges 1 `ApiCalls` unit per request via `CheckAndRecordAsync`; over-limit → 429 + ProblemDetails + `Retry-After`, and sets `HttpContext.Items[QuotaRejected]` so auditing can tag it. Resources: `ApiCalls` (counter), `StorageBytes`, `Users`, `ActiveFeatureFlags` (gauges). Skips health/metrics, unresolved tenants, and the root tenant. **Pipeline:** runs after auth (needs tenant) and after the rate limiter. Inject `TimeProvider` (not `DateTimeOffset.UtcNow`) for any time math here — the subsystem is `TimeProvider`-based.

`IQuotaService`: `CheckAsync` (no mutation), `RecordAsync` (increment), `CheckAndRecordAsync` (atomic — won't increment past the limit). Store: Redis (`RedisQuotaService`) or per-process `InMemoryQuotaService` (dev/test). `NoopQuotaService` when disabled.
