# Realtime — SignalR & SSE (backend)

`src/BuildingBlocks/Web/Realtime/` + `Sse/`. For the frontend side see `frontend/shared.md` + `frontend/dashboard.md`.

## SignalR (`AppHub`)

`[Authorize] AppHub` mapped at **`/api/v1/realtime/hub`**. Groups: `user:{userId}`, `tenant:{tenantId}`, `channel:{channelId}`.

- **Channel-group join is connect-time + on-demand.** `OnConnectedAsync` auto-joins `user:{id}`, `tenant:{id}`, and every `channel:{id}` the user is *already* a member of. A channel that becomes relevant **after** the socket is live (a new DM, or being added to a channel) is **not** auto-joined — the client must call the membership-gated **`JoinChannel(channelId)`** hub method (the dashboard does this on channel open + reconnect). Without it, group broadcasts silently miss that connection until a page reload re-runs `OnConnectedAsync`. New-DM creation pushes `ChatChannelAdded` to each other participant's `user:{id}` group so their channel list refreshes.
- **⚠️ Read the user from `Context.User`, NOT `ICurrentUser`.** `ICurrentUser` flows through `IHttpContextAccessor`, but the negotiate `HttpContext` isn't pinned to subsequent hub invocations → `ICurrentUser` returns nulls inside the hub. Use `Context.User` (the hub's `GetUserId()`/`GetTenantId()` helpers).
- Broadcasts are **scoped to groups** (`tenant:{id}`, `user:{id}`, `channel:{id}`), never `Clients.All`. `PresenceChanged` goes to the tenant group.
- Redis backplane is added automatically when `CachingOptions:Redis` is set (channel prefix `fsh-signalr`) — required for multi-replica.
- Push to a user from a module via `IHubContext<AppHub>` to group `user:{userId}` (e.g. Notifications' `"NotificationCreated"`).
- `IPresenceTracker` (in-memory, **per-host** — single-replica only for presence). Modules supply `IChannelMembershipChecker`/`IUserChannelLookup` adapters so the shared hub can authorize channel groups without depending on Chat.

## SSE (`Web/Sse/`) — two-step token

EventSource can't send `Authorization`, so SSE uses a token handshake:
1. `POST /api/v1/sse/token` (authorized) → opaque Guid, **single-use, 30s TTL** in `IDistributedCache`.
2. `GET /api/v1/sse/stream?token={guid}` (anonymous, consumes the token) → `text/event-stream`, `X-Accel-Buffering: no`, 15s heartbeat.

`SseConnectionManager` (singleton, `ConcurrentDictionary`, bounded channel cap 100 `DropOldest`): `TrySend(userId)` (all tabs), `Broadcast(tenantId)`, `BroadcastAll()`.

## Tests

SignalR hub tests force **long-polling** (TestServer has no WebSocket). See `integration-testing.md`.
