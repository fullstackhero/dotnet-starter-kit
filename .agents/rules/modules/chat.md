# Module: Chat

Slack-style messaging: 1:1 DMs, group DMs, named channels, threads, reactions, mentions, pins. Module `Order = 800` (after Notifications 750, so Notifications' handlers register first).

**Entities / DbContext:** `ChatChannel` (aggregate, soft-deletable) + `ChannelMember`; `Message` (aggregate) + `MessageAttachment`/`MessageMention`/`MessageReaction`. `ChatDbContext : BaseDbContext`, schema `chat`. Publishes `MentionedInChannelIntegrationEvent`.
**Areas:** Channels, Messages (incl. pin/edit/delete/threads), Reactions, Search. Full list: `Features/v1/` or `/scalar`.

## Gotchas

- **EF value-generation for nav children** — `MessageConfiguration` sets `Property(x => x.Id).ValueGeneratedNever()` for child collections (attachments/mentions/reactions). The domain assigns `Guid.CreateVersion7()` in factories; without this EF treats nav-collection children as `Modified` → 0-row UPDATE instead of INSERT. See `database.md`.
- `ChatDbContext` calls **`base.OnModelCreating` LAST** so tenant auto-apply sees the configured child types.
- **`ChannelAuthorization`** (`Features/v1/Internal/`): `RequireMember` throws **NotFound (404)** (not 403) so non-members can't probe channel existence; `RequireAdmin` throws `ForbiddenException`. Use these in every channel/message handler.
- **SignalR via `IHubContext<AppHub>`** (the shared hub in BuildingBlocks), groups `channel:{id}`. The hub reads the user via `Context.User`, not `ICurrentUser` (see `realtime.md`). Chat registers `IChannelMembershipChecker`/`IUserChannelLookup` adapters so the shared hub can authorize channel groups.
- SendMessage publishes `MentionedInChannelIntegrationEvent` **per distinct mentioned user**; Notifications consumes it.
- DMs use a sorted `DirectKey` (`"{lo}:{hi}"`) for find-or-create; **threads are single-level only**.
- Chat attachments register `ChatChannelFileAccessPolicy` (OwnerType `"ChatChannel"`): attach/read require membership, delete is uploader-only (see `modules/files.md`).
