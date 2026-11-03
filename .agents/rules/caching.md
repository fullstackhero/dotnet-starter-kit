# Caching

`src/BuildingBlocks/Caching/`. Read before adding cached reads or invalidation.

## What's registered

`AddHeroCaching(config)` always registers **`HybridCache`** (L1 in-memory + optional L2 Redis). Inject `HybridCache`, not `IDistributedCache`.

- `CachingOptions.Redis` empty → in-memory only (dev fallback). Set → a **single shared `ConnectionMultiplexer`** (singleton `IConnectionMultiplexer`) backs both the L2 cache and the DataProtection key ring.
- Defaults: total expiration 1h, L1 (local) expiration 2min (`CachingOptions`).
- **`CachingOptions:KeyPrefix`** namespaces everything the framework writes to Redis: L2 entries + tag invalidations (`RedisCacheOptions.InstanceName`), the Data Protection key ring, idempotency locks, quota counters (unless `QuotaOptions:KeyPrefix` overrides) and the SignalR channel. Set it per app **and** environment (`MyApp`, `MyApp:prod`) — a `defaultDatabase` index is not enough (pub/sub ignores it). Shared helper: `FSH.Framework.Core.Common.RedisKeyPrefix`. Code that talks to `IConnectionMultiplexer` directly must prepend `RedisKeyPrefix.Normalize(...)` itself; `CacheKeys` stay unprefixed (the prefix is applied underneath). Changing the prefix rotates the Data Protection key ring and starts a cold cache.
- `ObservableHybridCache` transparently decorates `HybridCache` to emit OpenTelemetry (hits/misses/factory-duration/invalidations). You don't reference it — just inject `HybridCache`.

## Pattern

```csharp
var perms = await cache.GetOrCreateAsync(
    CacheKeys.UserPermissions(userId),
    async ct => await LoadPermissionsAsync(userId, ct),
    tags: [CacheKeys.Tags.Permissions, CacheKeys.Tags.User(userId)],
    cancellationToken: ct);
```

- **Keys & tags live in `CacheKeys.cs`** — add new keys/tags there, don't inline strings. Existing: `UserPermissions(userId)`, `TenantTheme(tenantId)`, `IdempotencyEntry(tenantId,key)`, `ImpersonationGrantStatus(jti)`; tags `Permissions`, `Themes`, `Idempotency`, `Tenant(id)`, `User(id)`.
- Invalidate with `RemoveAsync(key)` or `RemoveByTagAsync(tag)` in the relevant mutation handler.
- `GetOrCreateAsync` gives **stampede protection** for free (factory runs once per key).

## Gotchas

- **No L1 backplane.** `RemoveByTagAsync` on one node does **not** evict L1 on peer nodes — cross-node staleness is bounded only by the 2-min local expiration. Don't rely on instant cross-node invalidation; keep local expiration short for hot, mutable data.
- Don't reach for `IDistributedCache` directly except where the framework already does so deliberately (idempotency probe-read) — prefer `HybridCache`.
