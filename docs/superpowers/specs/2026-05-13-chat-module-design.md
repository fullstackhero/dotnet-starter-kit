# Chat Module — Design Spec

**Status:** Approved, ready for implementation planning
**Date:** 2026-05-13
**Owner:** Mukesh Murugan

## Summary

Two new peer modules — `Modules.Chat` and `Modules.Notifications` — that bring Slack-style real-time messaging to the FullStackHero dashboard. Phase A scope is intentionally generous: send/edit/delete + real-time + unread + attachments + @mentions + threads + reactions + full-text search + typing indicators + a notifications inbox. Built on SignalR with the existing Redis container as a backplane.

The Notifications module is a general-purpose in-app inbox (not chat-specific) — chat is its first consumer, but it's designed to absorb future use cases (failed-webhook alerts, billing reminders, etc.) without further work.

## Goals

- A primitive flexible enough to back team chat, customer-support, or community use cases
- Production-ready realtime: groups, presence, scale-out via Redis backplane
- Reuses every existing kit primitive — Files (attachments), Identity (users + permissions), Mediator (CQRS), Webhooks (eventing), Quota (rate limits), SSE-style token-via-query auth on the hub
- Hits the same testing and architecture bar as the Files module: full unit + integration + architecture test coverage; module boundary rules enforced

## Scope

### In (Phase A)

- DMs (2-person), Group DMs (3+), named Channels (public + private)
- Send / edit / delete messages (soft-delete with `[deleted]` tombstones to keep threads coherent)
- Real-time delivery via SignalR with the existing Redis as backplane
- Unread counts + last-read pointer (per `(user, channel)`)
- Attachments via the Files module (`ownerType="ChatChannel"`)
- @mentions (parsed at write time)
- Threads / replies (1 level deep — no nested sub-threads)
- Reactions (emoji)
- Full-text search via Postgres `tsvector`
- Typing indicators (throttled, ephemeral)
- Notifications module: in-app inbox, bell-icon UI, mark-read endpoints
- Dashboard UI at `/chat`

### Out (deferred or never)

- Mobile-responsive layout — **Phase B**
- Push notifications (FCM/APNs) — **Phase B/C**
- Email digest for missed mentions — **Phase B**
- Admin app chat surface — **Phase B**
- Per-message read receipts (who's read what) — **Phase B**
- Bot framework / webhooks INTO chat — **Phase B**
- Voice / video — out of scope for this kit
- Cross-tenant messaging — out of scope (schema-per-tenant constrains)
- Message encryption at rest beyond Postgres TDE — out of scope
- Slack-import / migration tools — out of scope

## Architecture

### Module structure

```
src/Modules/Chat/
├── Modules.Chat/                  ← runtime
└── Modules.Chat.Contracts/        ← commands, queries, events, DTOs, realtime event records

src/Modules/Notifications/
├── Modules.Notifications/         ← runtime
└── Modules.Notifications.Contracts/

src/BuildingBlocks/Web/Realtime/   ← shared SignalR hub (AppHub) — see "SignalR hub" section
```

### Module boundaries

- `Modules.Chat` MUST NOT reference `Modules.Notifications` runtime — only Contracts
- `Modules.Notifications` MUST NOT reference `Modules.Chat` runtime
- Cross-module communication is via Integration Events: Chat publishes `MentionedInChannelIntegrationEvent`, Notifications consumes via `IIntegrationEventHandler<>`
- Both modules send realtime via the shared `IHubContext<AppHub>` — the SignalR hub lives in a BuildingBlock so neither module has to depend on the other for delivery

### Module load order

`[FshModule(Order = n)]` placements:
- Notifications: **750** (loads before Chat so Chat can register integration-event handlers against it)
- Chat: **800**

### Mediator wiring

`Program.cs::AddMediator(o => o.Assemblies = [...])` gains four markers — `ChatContractsMarker`, `ChatModule`, `NotificationsContractsMarker`, `NotificationsModule`. Per the existing project gotcha documented in `project_mediator_wiring.md`.

### Permissions

Registered via `PermissionConstants.Register(...)` in each module's `ConfigureServices`:

```
ChatPermissions.Channels.View          (basic)
ChatPermissions.Channels.Create        (basic — per "anyone can create" decision)
ChatPermissions.Channels.ManageAll     (admin — moderation: rename, delete any channel)
ChatPermissions.Messages.Send          (basic)
ChatPermissions.Messages.EditOwn       (basic)
ChatPermissions.Messages.DeleteOwn     (basic)
ChatPermissions.Messages.DeleteAny     (admin — moderation)
NotificationPermissions.View           (basic — see your own inbox)
```

### New NuGet dependencies

- `Microsoft.AspNetCore.SignalR.StackExchangeRedis` — Redis backplane for SignalR
- `@microsoft/signalr` (dashboard) — client library, ~15KB gzipped
- `@tanstack/react-virtual` (dashboard) — virtualized message list

No new Aspire containers. SignalR reuses the existing Redis configured via `CachingOptions:Redis`.

## Domain model

### Chat aggregates

```
ChatChannel (aggregate root, ISoftDeletable)
├── Id : Guid                           ── v7, ValueGeneratedNever
├── Type : enum                          ── DirectMessage | GroupMessage | Channel
├── Name : string?                       ── required for Channel; null for DMs
├── Slug : string?                       ── kebab-case; only for named channels
├── Description : string?
├── IsPrivate : bool                     ── DMs always private; Channels can be public/private
├── DirectKey : string?                  ── "{minUserId}:{maxUserId}" for Type=DirectMessage; null otherwise
├── CreatedByUserId : string
├── CreatedAtUtc, UpdatedAtUtc
├── LastMessageAtUtc : DateTime?         ── denormalized for "recent chats" sort
└── Members : List<ChannelMember>

ChannelMember (entity, child of ChatChannel)
├── Id : Guid                            ── v7, VGN
├── ChannelId, UserId
├── Role : enum                          ── Admin | Member  (channel creator = Admin)
├── JoinedAtUtc
├── LastReadMessageId : Guid?            ── unread-count pointer
└── IsMuted : bool

Message (aggregate root)
├── Id : Guid                            ── v7 (sortable; doubles as ordering key), VGN
├── ChannelId
├── AuthorUserId
├── Body : string?                       ── null when DeletedAtUtc is set
├── ParentMessageId : Guid?              ── thread root; null = top-level
├── ReplyCount : int                     ── denormalized for top-level messages
├── EditedAtUtc : DateTime?              ── null = never edited
├── DeletedAtUtc : DateTime?             ── soft-delete in-place; body cleared on delete
├── CreatedAtUtc
├── BodyTsv : tsvector                   ── generated column via raw SQL in migration; GIN-indexed
├── Mentions : List<MessageMention>
├── Reactions : List<MessageReaction>
└── Attachments : List<MessageAttachment>

MessageMention   (Id, MessageId, MentionedUserId, StartIndex, Length)
MessageReaction  (Id, MessageId, UserId, Emoji, CreatedAtUtc) — UNIQUE(MessageId,UserId,Emoji)
MessageAttachment (Id, MessageId, FileAssetId?, Url, ContentType, OriginalFileName, SizeBytes)
```

### Notifications aggregate

```
Notification (aggregate root)
├── Id : Guid                            ── v7, VGN
├── UserId                                ── recipient
├── Type : string                         ── "chat.mention", "webhook.failed", "billing.invoice_due", …
├── Title, Body?, Link?
├── Source : string                       ── originating module
├── Metadata : JSONB                      ── type-specific extras (e.g. channelId, messageId)
├── ReadAtUtc : DateTime?
└── CreatedAtUtc
```

### Notable decisions

**DM uniqueness.** When user A starts a DM with B, the system needs a unique DM channel between them. Solution: `DirectKey : string?` on `ChatChannel`, populated as `"{minUserId}:{maxUserId}"` for `Type=DirectMessage` (sorted user-id pair, lexical), null otherwise. Partial unique index `WHERE Type = 'DirectMessage'` enforces uniqueness. Write-once at creation — never updated — so the partial-unique-index swap race that bit ProductImages doesn't apply.

**Mention parsing at write time.** `Message.Create(body, knownUsers)` regex-matches `@\w+` against tenant user names; matches become `MessageMention` rows; unmatched `@foo` stays in body as plain text. Parsing is a domain method so it's testable and re-runs on `Edit`.

**Soft-delete strategy.** Channels: standard `ISoftDeletable` (global query filter hides them; restorable). Messages: NOT `ISoftDeletable` — just a nullable `DeletedAtUtc` column. We want deleted messages to render as `[deleted]` tombstones in the timeline to keep threads coherent.

**Search via Postgres `tsvector`.** Migration adds a `STORED` generated column on `Messages.Body` plus a GIN index. Search query uses raw SQL via `dbContext.Database.SqlQueryRaw<>` or `EF.Functions.ToTsQuery`. English language by default; document how to change it.

### Index plan

| Table | Index | Purpose |
|---|---|---|
| ChatChannels | `(Type, DirectKey)` UNIQUE WHERE `Type='DirectMessage'` | DM find-or-create |
| ChatChannels | `(Slug)` UNIQUE WHERE `Slug IS NOT NULL AND IsDeleted=FALSE` | named channel uniqueness |
| ChannelMembers | `(UserId, ChannelId)` UNIQUE | "is X in Y", member list |
| ChannelMembers | `(UserId)` | "list X's channels" |
| Messages | `(ChannelId, Id DESC)` | reverse-chrono paging (Id is v7-sortable) |
| Messages | `(ParentMessageId)` WHERE `ParentMessageId IS NOT NULL` | thread queries |
| Messages | GIN(`BodyTsv`) | full-text search |
| MessageMentions | `(MentionedUserId, MessageId DESC)` | "my mentions" inbox |
| Notifications | `(UserId, ReadAtUtc, CreatedAtUtc DESC)` | unread inbox by user |

## API surface

### REST endpoints

Send/edit/delete go through REST, **not** the hub. Keeps validators, permission attributes, idempotency, audit logging on a single coherent path. The hub is for *receiving* events and ephemeral typing only.

```
─── Channels ──────────────────────────────────────────────────
POST   /api/v1/chat/channels                       create (Channel | GroupMessage)
POST   /api/v1/chat/dms                            find-or-create DM/GroupDM
                                                   body: { userIds: [...] } → returns channelId
GET    /api/v1/chat/channels                       my channels (cursor-paged, sort=LastMessageAtUtc desc)
GET    /api/v1/chat/channels/discover?search=…     public channels I'm not in
GET    /api/v1/chat/channels/{id}                  details + members
PUT    /api/v1/chat/channels/{id}                  rename / desc / toggle private (channel admin)
DELETE /api/v1/chat/channels/{id}                  soft-archive (channel admin or ManageAll)
POST   /api/v1/chat/channels/{id}/restore          from archive (ManageAll)
POST   /api/v1/chat/channels/{id}/members          add user(s)
DELETE /api/v1/chat/channels/{id}/members/{userId} remove or self-leave
POST   /api/v1/chat/channels/{id}/read             mark as read up to messageId

─── Messages ──────────────────────────────────────────────────
POST   /api/v1/chat/channels/{id}/messages         send  (body: { body, parentMessageId?, attachments[] })
                                                   Idempotency-Key header supported
GET    /api/v1/chat/channels/{id}/messages?before= cursor-paged (uses Guid v7 sortability)
GET    /api/v1/chat/messages/{id}                  single message + thread metadata
PUT    /api/v1/chat/messages/{id}                  edit  (author only)
DELETE /api/v1/chat/messages/{id}                  soft-delete (author or DeleteAny)
GET    /api/v1/chat/messages/{id}/replies?before=  thread replies, cursor-paged

─── Reactions ─────────────────────────────────────────────────
POST   /api/v1/chat/messages/{id}/reactions        body: { emoji }
DELETE /api/v1/chat/messages/{id}/reactions/{emoji} remove your own

─── Search ────────────────────────────────────────────────────
GET    /api/v1/chat/search?q=&channelId=&pageNumber=  tsvector-backed full-text

─── Notifications ─────────────────────────────────────────────
GET    /api/v1/notifications?unreadOnly=&page=&pageSize=  inbox
POST   /api/v1/notifications/{id}/read                    mark single read
POST   /api/v1/notifications/read-all                     bulk
GET    /api/v1/notifications/unread-count                 just the count, for the bell badge
```

### Cross-cutting

- **Cursor pagination** — `?before={Guid}` and `?after={Guid}` over `Message.Id`. Guid v7 is monotonic, no separate timestamp column needed in the WHERE.
- **Idempotency** — `POST /chat/channels/{id}/messages` uses existing `WithIdempotency()` filter; replays return the original Message id with no duplicate row.
- **Rate limiting** — `RequireRateLimiting("chat")` on send-message. Default 30 messages/minute per user; configurable in `RateLimitingOptions`.
- **Audit log** — chat messages NOT audited by default (volume + privacy). Channel-management mutations (create/rename/delete channel, add/remove member) ARE audited. Configurable via `Chat.AuditMessages = false` setting.

## SignalR hub

### Location

`BuildingBlocks/Web/Realtime/AppHub.cs` — not inside Chat. Rationale: both Chat and Notifications push to the same user-connection. Putting the hub in a building block keeps module boundaries clean.

### Auth

SignalR uses the existing JWT. Browsers can't send `Authorization` headers on the WebSocket handshake, so we follow the project's existing SSE convention: `?access_token=...` query param on hub negotiation. Configured via `JwtBearerOptions.OnMessageReceived` in the existing JWT setup — one-line addition.

### Group taxonomy

Set up by `AppHub.OnConnectedAsync`:

- `user:{userId}` — every connection of that user joins. Targets: direct deliveries (notifications, your-mentions).
- `channel:{channelId}` — every member's connection joins for each channel they're in. Targets: message broadcasts.

Re-evaluated when channel members are added/removed (membership handlers also call `IHubContext<AppHub>.Groups.AddToGroupAsync` / `RemoveFromGroupAsync` for affected connections).

### Server → client events

DTOs in `Modules.Chat.Contracts.Realtime` and `Modules.Notifications.Contracts.Realtime`:

| Event | Target group | Fired by |
|---|---|---|
| `ChatMessageCreated` | `channel:{id}` | `SendMessageCommandHandler` |
| `ChatMessageEdited` | `channel:{id}` | `EditMessageCommandHandler` |
| `ChatMessageDeleted` | `channel:{id}` | `DeleteMessageCommandHandler` |
| `ChatReactionChanged` | `channel:{id}` | reactions handlers |
| `ChatMessageRead` | `channel:{id}` | mark-read handler (read-receipt dots) |
| `ChatTypingStarted` | `channel:{id}` | hub method `Typing(channelId)` |
| `ChatChannelMemberAdded` / `Removed` | `channel:{id}` + `user:{newMember}` | membership handlers (latter so the new member's UI adds the channel to its list live) |
| `NotificationCreated` | `user:{userId}` | `MentionedInChannelIntegrationEventHandler` in Notifications |

### Client → server methods

Just one, on purpose:

- `Typing(Guid channelId)` — hub verifies membership via injected `IChannelMembershipChecker` (interface in `Modules.Chat.Contracts`, impl in runtime), throttles via Redis cache (`typing:{channelId}:{userId}` SETEX 3s — skip broadcast if key exists), then broadcasts `ChatTypingStarted` to the channel group.

### Sending abstraction

Each module injects `IHubContext<AppHub>` and broadcasts directly. No custom wrapper interface — `IHubContext<T>` is already abstract and test-friendly (NSubstitute-able). Mediator handlers stay clean of SignalR types thanks to receiver record DTOs in Contracts.

## Frontend (dashboard)

### Scope

Dashboard only for Phase A. Admin app port deferred (matches the precedent set with Files).

### Route + layout

- New top-level route `/chat`, lazy-loaded chunk (same `lazyNamed` pattern as `/files`)
- Sub-routes: `/chat/{channelId}` and `/chat/{channelId}?thread={messageId}` (thread is a query param so opening/closing it doesn't unmount the message pane)
- Desktop-first; `min-width: 1024px`. Below that, render a "use a wider screen" hint. Mobile is Phase B.

```
┌────────────────────────────────────────────────────────────────────┐
│ Topbar  (Search · Bell with unread badge · Avatar)                 │
├──────────┬──────────────────────┬──────────────────────┬───────────┤
│ App nav  │ ChannelRail          │ MessageList          │ ThreadPan │
│ (left)   │  · DMs               │  · ChannelHeader     │  (only    │
│          │  · Channels you're   │  · messages          │  when     │
│          │    in                │  · TypingIndicator   │  open;    │
│          │  · "+ New" / Browse  │  · Composer          │  ~340px)  │
└──────────┴──────────────────────┴──────────────────────┴───────────┘
```

### New nav entry

One line in `clients/dashboard/src/components/layout/nav-data.ts`:

```ts
{ to: "/chat", label: "Chat", icon: MessageSquare }
```

### Components

Under `clients/dashboard/src/components/chat/`:

| Component | Job |
|---|---|
| `ChatPage` | Route root; orchestrates the three panes; reads `:channelId` from path |
| `ChannelRail` | Left rail. DMs section + Channels section + "+ Start DM" + "+ New channel" + "Browse channels" |
| `ChannelHeader` | Title + member count + add-member chip + privacy badge + thread-history button |
| `MessageList` | Virtualized via `@tanstack/react-virtual`; infinite-scroll-up for history; auto-scroll-to-bottom on new message only if user is already at bottom (otherwise "↓ N new messages" pill) |
| `Message` | One bubble: author avatar, body with mentions highlighted, edit indicator, reactions row, hover action toolbar |
| `Composer` | Multi-line textarea, `@`-trigger autocomplete, drag-drop file zone, attachment thumbnails, send button (Enter = send, Shift+Enter = newline) |
| `MentionAutocomplete` | Popover triggered by `@`; queries `/api/v1/identity/users?search=`, debounced |
| `EmojiPicker` | Lightweight curated 200-emoji set shipped under `data/emoji.json` — avoids pulling `emoji-mart` |
| `ThreadPanel` | Right rail; reuses `MessageList` + `Composer` scoped to `parentMessageId` |
| `TypingIndicator` | "X is typing" / "X and Y are typing" / "Several people are typing" — 5s rolling window |
| `NotificationBell` | Topbar component; bell + unread badge; dropdown lists recent notifications; click navigates to `notification.link` |

### Realtime context

`clients/dashboard/src/realtime/realtime-context.tsx` — singleton SignalR wrapper:

- Opens connection on `AuthProvider` user-resolved
- Auto-reconnect with `withAutomaticReconnect([0, 2000, 10000, 30000])`
- Exposes `useRealtimeEvent("ChatMessageCreated", handler)` — typed hook that adds/removes handlers per event name
- One connection per tab; closed on logout/page unload
- Token refresh: on 401-on-handshake, calls `apiFetch` to refresh and re-handshakes

### Cache integration

- `ChatMessageCreated` → `queryClient.setQueryData(['chat','channels',channelId,'messages'], append)` + invalidate `['chat','channels']` (for the rail's "last message" preview + unread count)
- `ChatMessageEdited` / `Deleted` → in-place patch via `setQueryData`
- `NotificationCreated` → invalidate `['notifications']` + `['notifications','unread-count']` + toast if the link points outside the current `/chat/{channelId}`

### File attachments in composer

Reuses the existing `useFileUpload` hook with `ownerType: "ChatChannel"`, `ownerId: channelId`, `visibility: Visibility.Private`, `category` resolved per-file from extension (same map as the My Files page). Backend adds a `ChatChannelFileAccessPolicy` in `Modules.Chat/Authorization/` that gates attach by channel membership.

## Phasing — Plan B (4 slices on `develop`)

Each slice ends green (build + arch + tests passing) and is independently demoable.

### Slice 1 — Backend scaffold + channels + messages baseline (~6 commits)

What you can use after: send and receive plain-text messages in named channels via REST. No realtime yet (manual refresh).

Includes: module scaffolding, EF migration #1 (channels, members, messages tables), CRUD endpoints for channels and messages, permissions registration, architecture tests, integration tests for the baseline.

### Slice 2 — SignalR + realtime + DMs (~4 commits)

What you can use after: messages stream live across tabs. DMs work.

Includes: `AppHub` building block, SignalR Redis backplane wiring, group taxonomy + connect/disconnect lifecycle, broadcast plumbing in send/edit/delete handlers, DM find-or-create endpoint, dashboard realtime context and live updates.

### Slice 3 — Notifications module + @mentions (~4 commits)

What you can use after: bell-icon inbox in the topbar. Mention someone, they see a notification.

Includes: Notifications module scaffolding, EF migration, REST endpoints, `MentionedInChannelIntegrationEvent` raised by Chat + consumed by Notifications, mention parsing in `Message.Create`, dashboard `NotificationBell`.

### Slice 4 — Threads + reactions + search + typing (~6 commits)

What you can use after: all the bells. Phase A done.

Includes: thread data model + endpoints + side panel, reactions endpoints + UI, tsvector migration + search endpoint + search UI, typing hub method + indicator, full sweep + handoff update.

## Testing strategy

### `Chat.Tests` (new unit project)

Pure-domain tests, NSubstitute for the few injectables. No DB, no SignalR. Targets aggregate invariants. Examples:

- `Message.Create_Should_Parse_Mentions_From_Body`
- `Message.Edit_Should_Recompute_Mentions_And_Set_EditedAtUtc`
- `Message.SoftDelete_Should_Clear_Body_And_Set_DeletedAtUtc`
- `ChatChannel.AddMember_Should_Reject_Duplicate`
- `ChatChannel.RemoveCreator_From_DM_Should_Throw` (DMs are immutable membership)
- `ChatChannel.SetDirectKey_Should_Sort_User_Ids`

Expected count: ~25 unit tests.

### `Integration.Tests/Tests/Chat/`

REST endpoints against the real `FshWebApplicationFactory` (Postgres testcontainer + MinIO testcontainer already there).

New test classes:

- `ChannelLifecycleTests` — create / rename / archive / restore / discover / DM find-or-create
- `ChannelMembershipTests` — add / remove / leave / public-join self-serve
- `MessageLifecycleTests` — send / edit / delete / cursor pagination / idempotency
- `MentionAndNotificationTests` — full cross-module integration via `MentionedInChannelIntegrationEvent`
- `ReactionsTests` — add / remove / unique-per-user-emoji
- `ThreadsTests` — reply / `ReplyCount` denormalization / thread pagination
- `SearchTests` — tsvector hits across member channels; non-members can't see results
- `RealtimeEventsTests` — `Microsoft.AspNetCore.SignalR.Client` against `WebApplicationFactory.Server`; verifies that a POST to send-message triggers `ChatMessageCreated` to a connected member and *not* to a non-member; typing throttle

Expected count: ~40-50 integration tests.

### `Architecture.Tests`

New rules:

- `Modules.Chat` does NOT depend on `Modules.Notifications` runtime
- `Modules.Notifications` does NOT depend on `Modules.Chat` runtime
- Chat handlers don't reference `Microsoft.AspNetCore.SignalR` directly — they go through `IHubContext<AppHub>`
- Endpoint name verb whitelist gets new verbs as needed (`Mute`, `Unmute`, `Mark`, `React`)

## Verification gate

Slice 4 is "done" when:

- Full `.slnx` build: 0 warnings, 0 errors
- Architecture.Tests + Chat.Tests + Integration.Tests all green
- Dashboard typecheck + lint + Vite build clean
- Manual smoke (browser): create channel → send → see live on second tab → mention → see notification in bell → edit → see edit propagate → react → see reaction propagate → upload image → see preview → search for word → find message → open thread → reply
- Handoff + memory updated to reflect the shipped state

## Open questions / future work

Not blockers for Phase A, but worth tracking:

- **Read receipts**: only my-own LastReadMessageId in Phase A. Phase B could add per-message-per-user receipts.
- **Channel categories / sections**: Slack-style sidebar groupings. Phase B.
- **Pinned messages**: per-channel pin list. Phase B.
- **Message scheduling / drafts**: client-only drafts work; server-side scheduling is Phase B.
- **Compliance exports**: bulk message export for legal hold. Out of scope for kit; adopters can add.
- **Bot / app framework**: webhook-out exists (existing module); webhook-IN to chat is Phase B.
