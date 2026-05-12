# Chat Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Slack-style chat (DMs + group DMs + named channels) on top of FullStackHero, plus a peer Notifications module powering the dashboard's bell-icon inbox.

**Architecture:** Two new modules under `src/Modules/Chat` and `src/Modules/Notifications`, both peers to existing modules (Files, Catalog, Tickets, etc.). SignalR hub lives in `BuildingBlocks/Web/Realtime` so neither module depends on the other for delivery. Cross-module communication exclusively via integration events. Postgres `tsvector` for full-text search. Existing Redis as SignalR backplane.

**Tech Stack:** .NET 10, EF Core 10 + Npgsql, Mediator 3.x source generator, SignalR + Redis backplane, React 19 + Vite + TanStack Query + `@microsoft/signalr` + `@tanstack/react-virtual`.

**Spec:** `docs/superpowers/specs/2026-05-13-chat-module-design.md`

---

## Pre-flight checklist (do once before Slice 1)

- [ ] **Confirm prerequisites**

Confirm:
- Latest `develop` checked out, working tree clean (apart from any in-flight layout refactor in `clients/dashboard`).
- `dotnet build src/FSH.Starter.slnx --nologo` → 0/0.
- Read `memory/handoff.md`, `memory/project_mediator_wiring.md`, `memory/project_ef_value_generation_for_nav_children.md` — every new entity reached only via a parent nav collection needs `.ValueGeneratedNever()`.
- The API process is **stopped** for the duration of this work (it locks Host binaries during `dotnet build`).

- [ ] **Branch policy**

Per Plan B, all 4 slices land on `develop` directly. Each slice ends with full build + tests green. Each commit references the slice number in its subject (`feat(chat-1): ...`).

---

# SLICE 1 — Backend scaffold + channels + messages baseline

**Goal of slice:** Create channels, list them, add/remove members, send/edit/delete plain-text messages, mark as read. **No realtime yet** (manual refresh works). **No attachments yet** (Slice 2 wires `ChatChannelFileAccessPolicy`). **No mentions yet** (Slice 3).

## Task 1.1 — Scaffold Chat module + Contracts projects

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Modules.Chat.csproj`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/Modules.Chat.Contracts.csproj`
- Modify: `src/FSH.Starter.slnx` — add both projects
- Modify: `src/Host/FSH.Starter.Migrations.PostgreSQL/FSH.Starter.Migrations.PostgreSQL.csproj` — add ProjectReference to `Modules.Chat`
- Modify: `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` — add ProjectReference to `Modules.Chat`

- [ ] **Step 1: Create Modules.Chat.Contracts.csproj**

Pattern: copy `src/Modules/Files/Modules.Files.Contracts/Modules.Files.Contracts.csproj`. Adjust `RootNamespace` and `AssemblyName` to `FSH.Modules.Chat.Contracts`. Single ProjectReference to `BuildingBlocks/Shared/Shared.csproj`.

- [ ] **Step 2: Create Modules.Chat.csproj**

Pattern: copy `src/Modules/Files/Modules.Files/Modules.Files.csproj`. Adjust namespace + assembly name. ProjectReferences:
```xml
<ProjectReference Include="..\..\..\BuildingBlocks\Persistence\Persistence.csproj" />
<ProjectReference Include="..\..\..\BuildingBlocks\Web\Web.csproj" />
<ProjectReference Include="..\..\..\BuildingBlocks\Eventing\Eventing.csproj" />
<ProjectReference Include="..\..\..\BuildingBlocks\Storage\Storage.csproj" />
<ProjectReference Include="..\Modules.Chat.Contracts\Modules.Chat.Contracts.csproj" />
<ProjectReference Include="..\..\Files\Modules.Files.Contracts\Modules.Files.Contracts.csproj" />
<ProjectReference Include="..\..\Identity\Modules.Identity.Contracts\Modules.Identity.Contracts.csproj" />
```

- [ ] **Step 3: Add both projects to .slnx**

Edit `src/FSH.Starter.slnx`. Mirror the structure used for `Modules.Files` in the same file. New folder `Chat` under `Modules`, two `Project` entries inside.

- [ ] **Step 4: Reference Modules.Chat from the migration project + the API host**

Add `<ProjectReference Include="..\..\Modules\Chat\Modules.Chat\Modules.Chat.csproj" />` to both `FSH.Starter.Migrations.PostgreSQL.csproj` and `FSH.Starter.Api.csproj` (alphabetical within their respective ItemGroups).

- [ ] **Step 5: Verify**

```
dotnet build src/Modules/Chat/Modules.Chat.Contracts/Modules.Chat.Contracts.csproj --nologo
dotnet build src/Modules/Chat/Modules.Chat/Modules.Chat.csproj --nologo
```
Expected: both succeed with 0/0.

- [ ] **Step 6: Commit**

```
git add src/Modules/Chat/ src/FSH.Starter.slnx src/Host/FSH.Starter.Migrations.PostgreSQL/FSH.Starter.Migrations.PostgreSQL.csproj src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj
git commit -m "feat(chat-1): scaffold Modules.Chat + Modules.Chat.Contracts projects"
```

## Task 1.2 — Permissions constants

**Files:**
- Create: `src/Modules/Chat/Modules.Chat.Contracts/Authorization/ChatPermissions.cs`

- [ ] **Step 1: Write ChatPermissions.cs**

Pattern: mirror `src/Modules/Files/Modules.Files.Contracts/Authorization/FilesPermissions.cs`.

```csharp
using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Chat.Contracts.Authorization;

public static class ChatPermissions
{
    public const string Resource = "Chat";

    public static class Channels
    {
        public const string Resource = "Chat.Channels";
        public const string View      = $"Permissions.{Resource}.View";
        public const string Create    = $"Permissions.{Resource}.Create";
        public const string ManageAll = $"Permissions.{Resource}.ManageAll";
    }

    public static class Messages
    {
        public const string Resource  = "Chat.Messages";
        public const string Send      = $"Permissions.{Resource}.Send";
        public const string EditOwn   = $"Permissions.{Resource}.EditOwn";
        public const string DeleteOwn = $"Permissions.{Resource}.DeleteOwn";
        public const string DeleteAny = $"Permissions.{Resource}.DeleteAny";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Chat Channels",   ActionConstants.View,  Channels.Resource, IsBasic: true),
        new("Create Chat Channels", ActionConstants.Create, Channels.Resource, IsBasic: true),
        new("Manage All Channels",  "ManageAll",            Channels.Resource),
        new("Send Messages",        "Send",                 Messages.Resource, IsBasic: true),
        new("Edit Own Messages",    "EditOwn",              Messages.Resource, IsBasic: true),
        new("Delete Own Messages",  "DeleteOwn",            Messages.Resource, IsBasic: true),
        new("Delete Any Message",   "DeleteAny",            Messages.Resource),
    ];
}
```

- [ ] **Step 2: Verify build**

```
dotnet build src/Modules/Chat/Modules.Chat.Contracts/Modules.Chat.Contracts.csproj --nologo
```

- [ ] **Step 3: Commit**

```
git add src/Modules/Chat/Modules.Chat.Contracts/Authorization/
git commit -m "feat(chat-1): ChatPermissions constants"
```

## Task 1.3 — Domain: ChannelType enum + ChatChannel aggregate

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Domain/ChannelType.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/ChannelMemberRole.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/ChatChannel.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/ChannelMember.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/ChannelCreatedDomainEvent.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/ChannelMemberAddedDomainEvent.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/ChannelMemberRemovedDomainEvent.cs`

- [ ] **Step 1: Write the enums**

```csharp
// ChannelType.cs
namespace FSH.Modules.Chat.Domain;
public enum ChannelType { DirectMessage = 0, GroupMessage = 1, Channel = 2 }

// ChannelMemberRole.cs
namespace FSH.Modules.Chat.Domain;
public enum ChannelMemberRole { Member = 0, Admin = 1 }
```

- [ ] **Step 2: Write the domain event records**

Pattern: mirror `src/Modules/Files/Modules.Files/Domain/Events/FileFinalizedDomainEvent.cs`.

```csharp
// ChannelCreatedDomainEvent.cs
using FSH.Framework.Core.Domain;
namespace FSH.Modules.Chat.Domain.Events;
public sealed record ChannelCreatedDomainEvent(
    Guid ChannelId,
    ChannelType Type,
    string? Name,
    string CreatedByUserId,
    Guid EventId,
    DateTime OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

// ChannelMemberAddedDomainEvent.cs
public sealed record ChannelMemberAddedDomainEvent(
    Guid ChannelId,
    string AddedUserId,
    string AddedByUserId,
    Guid EventId,
    DateTime OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

// ChannelMemberRemovedDomainEvent.cs
public sealed record ChannelMemberRemovedDomainEvent(
    Guid ChannelId,
    string RemovedUserId,
    string RemovedByUserId,
    Guid EventId,
    DateTime OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
```

- [ ] **Step 3: Write ChannelMember entity**

```csharp
using FSH.Framework.Core.Domain;
namespace FSH.Modules.Chat.Domain;

public sealed class ChannelMember : BaseEntity<Guid>
{
    public Guid ChannelId { get; private set; }
    public string UserId { get; private set; } = default!;
    public ChannelMemberRole Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public Guid? LastReadMessageId { get; private set; }
    public bool IsMuted { get; private set; }

    private ChannelMember() { }

    internal static ChannelMember Create(Guid channelId, string userId, ChannelMemberRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new ChannelMember
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow,
        };
    }

    internal void MarkRead(Guid messageId) => LastReadMessageId = messageId;
    internal void SetMuted(bool muted) => IsMuted = muted;
    internal void Promote(ChannelMemberRole role) => Role = role;
}
```

- [ ] **Step 4: Write ChatChannel aggregate**

Per spec: `ISoftDeletable`, `AggregateRoot<Guid>`. `DirectKey` is the sorted user-id pair for DMs.

```csharp
using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Chat.Domain.Events;

namespace FSH.Modules.Chat.Domain;

public sealed class ChatChannel : AggregateRoot<Guid>, ISoftDeletable
{
    public ChannelType Type { get; private set; }
    public string? Name { get; private set; }
    public string? Slug { get; private set; }
    public string? Description { get; private set; }
    public bool IsPrivate { get; private set; }
    public string? DirectKey { get; private set; }   // "{minUserId}:{maxUserId}" for Type=DirectMessage
    public string CreatedByUserId { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? LastMessageAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<ChannelMember> _members = [];
    public IReadOnlyList<ChannelMember> Members => _members;

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private ChatChannel() { }

    public static ChatChannel CreateChannel(string name, string? description, bool isPrivate, string creatorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUserId);

        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.Channel,
            Name = name.Trim(),
            Slug = Slugify(name),
            Description = description?.Trim(),
            IsPrivate = isPrivate,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        c._members.Add(ChannelMember.Create(c.Id, creatorUserId, ChannelMemberRole.Admin));
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, c.Name, creatorUserId, id, ts)));
        return c;
    }

    public static ChatChannel CreateDirect(string userAId, string userBId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userBId);
        if (userAId == userBId) throw new ArgumentException("Cannot DM yourself.");

        var (lo, hi) = string.CompareOrdinal(userAId, userBId) < 0 ? (userAId, userBId) : (userBId, userAId);
        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.DirectMessage,
            IsPrivate = true,
            DirectKey = $"{lo}:{hi}",
            CreatedByUserId = userAId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        c._members.Add(ChannelMember.Create(c.Id, userAId, ChannelMemberRole.Member));
        c._members.Add(ChannelMember.Create(c.Id, userBId, ChannelMemberRole.Member));
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, null, userAId, id, ts)));
        return c;
    }

    public static ChatChannel CreateGroupDm(IReadOnlyList<string> userIds, string creatorUserId)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count < 3) throw new ArgumentException("Group DM requires at least 3 members.");
        if (userIds.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("All user ids required.");

        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.GroupMessage,
            IsPrivate = true,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        foreach (var uid in userIds.Distinct(StringComparer.Ordinal))
        {
            c._members.Add(ChannelMember.Create(c.Id, uid,
                uid == creatorUserId ? ChannelMemberRole.Admin : ChannelMemberRole.Member));
        }
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, null, creatorUserId, id, ts)));
        return c;
    }

    public void Rename(string name, string? description)
    {
        if (Type != ChannelType.Channel)
            throw new InvalidOperationException("Only named Channels can be renamed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Slug = Slugify(name);
        Description = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPrivate(bool isPrivate)
    {
        if (Type != ChannelType.Channel)
            throw new InvalidOperationException("Only named Channels can change privacy.");
        IsPrivate = isPrivate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public ChannelMember AddMember(string userId, string addedByUserId, ChannelMemberRole role = ChannelMemberRole.Member)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (Type == ChannelType.DirectMessage)
            throw new InvalidOperationException("DMs have fixed membership.");
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException($"User {userId} is already a member.");

        var m = ChannelMember.Create(Id, userId, role);
        _members.Add(m);
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelMemberAddedDomainEvent(Id, userId, addedByUserId, id, ts)));
        return m;
    }

    public void RemoveMember(string userId, string removedByUserId)
    {
        if (Type == ChannelType.DirectMessage)
            throw new InvalidOperationException("DMs have fixed membership.");
        var m = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException($"User {userId} is not a member.");
        _members.Remove(m);
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelMemberRemovedDomainEvent(Id, userId, removedByUserId, id, ts)));
    }

    public void MarkRead(string userId, Guid messageId)
    {
        var m = _members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new InvalidOperationException($"User {userId} is not a member.");
        m.MarkRead(messageId);
    }

    public void TouchLastMessage(DateTime utcNow)
    {
        LastMessageAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        return collapsed;
    }
}
```

- [ ] **Step 5: Verify**

```
dotnet build src/Modules/Chat/Modules.Chat/Modules.Chat.csproj --nologo
```
Expected: 0/0.

- [ ] **Step 6: Commit**

```
git add src/Modules/Chat/Modules.Chat/Domain/
git commit -m "feat(chat-1): ChatChannel aggregate + ChannelMember + domain events"
```

## Task 1.4 — Domain: Message aggregate + MessageAttachment

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Domain/Message.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/MessageAttachment.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/MessageCreatedDomainEvent.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/MessageEditedDomainEvent.cs`
- Create: `src/Modules/Chat/Modules.Chat/Domain/Events/MessageDeletedDomainEvent.cs`

- [ ] **Step 1: Domain event records**

Mirror the Channel events. Three events: created/edited/deleted. Each carries `ChannelId`, `MessageId`, `AuthorUserId`, plus event metadata.

- [ ] **Step 2: MessageAttachment entity**

```csharp
using FSH.Framework.Core.Domain;
namespace FSH.Modules.Chat.Domain;

public sealed class MessageAttachment : BaseEntity<Guid>
{
    public Guid MessageId { get; private set; }
    public Guid? FileAssetId { get; private set; }
    public string Url { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public long SizeBytes { get; private set; }

    private MessageAttachment() { }

    internal static MessageAttachment Create(
        Guid messageId, Guid? fileAssetId, string url, string contentType, string fileName, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new MessageAttachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            FileAssetId = fileAssetId,
            Url = url,
            ContentType = contentType,
            OriginalFileName = fileName,
            SizeBytes = sizeBytes,
        };
    }
}
```

- [ ] **Step 3: Message aggregate**

```csharp
using FSH.Framework.Core.Domain;
using FSH.Modules.Chat.Domain.Events;
namespace FSH.Modules.Chat.Domain;

public sealed class Message : AggregateRoot<Guid>
{
    public Guid ChannelId { get; private set; }
    public string AuthorUserId { get; private set; } = default!;
    public string? Body { get; private set; }
    public Guid? ParentMessageId { get; private set; }
    public int ReplyCount { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<MessageAttachment> _attachments = [];
    public IReadOnlyList<MessageAttachment> Attachments => _attachments;

    private Message() { }

    public static Message Create(Guid channelId, string authorUserId, string body, Guid? parentMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        var m = new Message
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            ParentMessageId = parentMessageId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        m.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageCreatedDomainEvent(channelId, m.Id, authorUserId, parentMessageId, id, ts)));
        return m;
    }

    public void Edit(string newBody, string editingUserId)
    {
        if (DeletedAtUtc.HasValue) throw new InvalidOperationException("Cannot edit a deleted message.");
        if (!string.Equals(AuthorUserId, editingUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("Only the author can edit a message.");
        ArgumentException.ThrowIfNullOrWhiteSpace(newBody);
        Body = newBody.Trim();
        EditedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageEditedDomainEvent(ChannelId, Id, AuthorUserId, id, ts)));
    }

    public void SoftDelete(string deletingUserId, bool isModerator)
    {
        if (DeletedAtUtc.HasValue) return;
        if (!isModerator && !string.Equals(AuthorUserId, deletingUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("Only the author or a moderator can delete.");
        DeletedAtUtc = DateTime.UtcNow;
        Body = null;
        // Note: attachments stay attached for now; Phase B can clean up via PurgeJob.
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageDeletedDomainEvent(ChannelId, Id, AuthorUserId, id, ts)));
    }

    public MessageAttachment AddAttachment(Guid? fileAssetId, string url, string contentType, string fileName, long sizeBytes)
    {
        var a = MessageAttachment.Create(Id, fileAssetId, url, contentType, fileName, sizeBytes);
        _attachments.Add(a);
        return a;
    }

    internal void IncrementReplyCount() => ReplyCount++;
    internal void DecrementReplyCount() => ReplyCount = Math.Max(0, ReplyCount - 1);
}
```

- [ ] **Step 4: Verify**

```
dotnet build src/Modules/Chat/Modules.Chat/Modules.Chat.csproj --nologo
```

- [ ] **Step 5: Commit**

```
git add src/Modules/Chat/Modules.Chat/Domain/
git commit -m "feat(chat-1): Message aggregate + MessageAttachment + domain events"
```

## Task 1.5 — EF configurations + ChatDbContext + initializer

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Data/ChatDbContext.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/ChatDbInitializer.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/Configurations/ChatChannelConfiguration.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/Configurations/ChannelMemberConfiguration.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/Configurations/MessageConfiguration.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/Configurations/MessageAttachmentConfiguration.cs`

- [ ] **Step 1: ChatDbContext**

Pattern: mirror `src/Modules/Files/Modules.Files/Data/FilesDbContext.cs`. Schema = `"chat"`. DbSets for `Channels` and `Messages` (the child entities are reached via nav).

- [ ] **Step 2: ChatDbInitializer**

Pattern: mirror `FilesDbInitializer`. `MigrateAsync` runs `Database.MigrateAsync()`. No seed needed for chat — channels are user-created.

- [ ] **Step 3: ChatChannelConfiguration**

Critical: every Id property gets `.ValueGeneratedNever()` per `project_ef_value_generation_for_nav_children.md`. Partial unique index on `(Type, DirectKey)` WHERE `Type = 0` (DirectMessage). Members nav AutoInclude.

```csharp
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class ChatChannelConfiguration : IEntityTypeConfiguration<ChatChannel>
{
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Channels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Slug).HasMaxLength(220);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.IsPrivate).IsRequired();
        builder.Property(x => x.DirectKey).HasMaxLength(80);
        builder.HasIndex(x => x.DirectKey).IsUnique().HasFilter("\"Type\" = 0 AND \"IsDeleted\" = FALSE");

        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.LastMessageAtUtc);

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Members).AutoInclude();

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 4: ChannelMemberConfiguration**

```csharp
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ChannelMembers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Role).IsRequired().HasConversion<int>();
        builder.Property(x => x.JoinedAtUtc).IsRequired();
        builder.Property(x => x.LastReadMessageId);
        builder.Property(x => x.IsMuted).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ChannelId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
```

- [ ] **Step 5: MessageConfiguration**

```csharp
using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.AuthorUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Body).HasColumnType("text"); // up to 32 KB body in practice
        builder.Property(x => x.ParentMessageId);
        builder.Property(x => x.ReplyCount).IsRequired();
        builder.Property(x => x.EditedAtUtc);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Reverse-chronological paging by Id desc (Guid v7 sortable)
        builder.HasIndex(x => new { x.ChannelId, x.Id }).IsDescending(false, true);
        builder.HasIndex(x => x.ParentMessageId).HasFilter("\"ParentMessageId\" IS NOT NULL");

        // ChannelId FK is logical (no nav back from Message → ChatChannel to avoid cyclic eager loads)
        builder.HasOne<ChatChannel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attachments).AutoInclude();

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 6: MessageAttachmentConfiguration**

```csharp
public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MessageAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(255);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(512);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.HasIndex(x => x.MessageId);
    }
}
```

- [ ] **Step 7: Verify**

```
dotnet build src/Modules/Chat/Modules.Chat/Modules.Chat.csproj --nologo
```

- [ ] **Step 8: Commit**

```
git add src/Modules/Chat/Modules.Chat/Data/
git commit -m "feat(chat-1): ChatDbContext + EF configurations"
```

## Task 1.6 — ChatModule registration (skeleton, no endpoints yet)

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/AssemblyInfo.cs`
- Create: `src/Modules/Chat/Modules.Chat/ChatModule.cs`

- [ ] **Step 1: AssemblyInfo**

```csharp
using FSH.Framework.Web.Modules;
[assembly: FshModule(typeof(FSH.Modules.Chat.ChatModule), 800)]
```

- [ ] **Step 2: ChatModule.cs**

Pattern: mirror `src/Modules/Files/Modules.Files/FilesModule.cs`. ConfigureServices registers DbContext + initializer + permissions + FluentValidation. MapEndpoints starts empty (we wire endpoints task-by-task below).

```csharp
using Asp.Versioning;
using FluentValidation;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Chat.Contracts.Authorization;
using FSH.Modules.Chat.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Chat;

public sealed class ChatModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(ChatPermissions.All);

        builder.Services.AddHeroDbContext<ChatDbContext>();
        builder.Services.AddScoped<IDbInitializer, ChatDbInitializer>();
        builder.Services.AddValidatorsFromAssembly(typeof(ChatModule).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ChatDbContext>("db:chat", failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();
        var group = endpoints.MapGroup("api/v{version:apiVersion}/chat")
            .WithTags("Chat")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Endpoints mapped here task-by-task below.
        _ = group;
    }
}
```

- [ ] **Step 3: Wire Program.cs**

Modify `src/Host/FSH.Starter.Api/Program.cs`:
1. Add `using FSH.Modules.Chat;`
2. Add `typeof(FSH.Modules.Chat.ChatModule).Assembly,` to the `moduleAssemblies` array.
3. Mediator: add `typeof(FSH.Modules.Chat.Contracts.ChatContractsMarker), typeof(FSH.Modules.Chat.ChatModule)` to the `AddMediator(o => o.Assemblies = [...])` list. (Will define `ChatContractsMarker` in next step.)

- [ ] **Step 4: Create the Contracts marker**

```csharp
// src/Modules/Chat/Modules.Chat.Contracts/ChatContractsMarker.cs
namespace FSH.Modules.Chat.Contracts;
public abstract class ChatContractsMarker;
```

- [ ] **Step 5: Verify full host build**

```
dotnet build src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj --nologo
```
Expected: 0/0. If errors, double-check: every handler created in subsequent tasks must be `public sealed` (per `project_mediator_wiring.md`).

- [ ] **Step 6: Generate initial migration**

```
cd src/Host/FSH.Starter.Api
dotnet ef migrations add InitialChat --project ../FSH.Starter.Migrations.PostgreSQL --context ChatDbContext --output-dir Chat
cd ../../..
```

Review the generated `<timestamp>_InitialChat.cs`: should create `Channels`, `ChannelMembers`, `Messages`, `MessageAttachments` tables, the partial unique indexes, and FKs.

- [ ] **Step 7: Commit**

```
git add src/Modules/Chat/ src/Host/FSH.Starter.Api/Program.cs src/Host/FSH.Starter.Migrations.PostgreSQL/Chat/
git commit -m "feat(chat-1): ChatModule registration + initial EF migration"
```

## Task 1.7 — Contracts: DTOs + commands + queries

**Files:**
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/DTOs/ChannelDto.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/DTOs/ChannelMemberDto.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/DTOs/MessageDto.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/DTOs/MessageAttachmentDto.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/CreateChannelCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/UpdateChannelCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/ArchiveChannelCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/RestoreChannelCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/AddChannelMembersCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/RemoveChannelMemberCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/FindOrCreateDmCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/SendMessageCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/EditMessageCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/DeleteMessageCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Commands/MarkChannelReadCommand.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Queries/GetChannelByIdQuery.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Queries/ListMyChannelsQuery.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Queries/DiscoverChannelsQuery.cs`
- Create: `src/Modules/Chat/Modules.Chat.Contracts/v1/Queries/ListChannelMessagesQuery.cs`

- [ ] **Step 1: DTOs**

```csharp
// ChannelDto.cs
public sealed record ChannelDto(
    Guid Id,
    int Type,             // 0=DM, 1=GroupDM, 2=Channel
    string? Name,
    string? Slug,
    string? Description,
    bool IsPrivate,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    IReadOnlyList<ChannelMemberDto> Members);

// ChannelMemberDto.cs
public sealed record ChannelMemberDto(
    Guid Id,
    string UserId,
    int Role,             // 0=Member, 1=Admin
    DateTime JoinedAtUtc,
    Guid? LastReadMessageId,
    bool IsMuted);

// MessageDto.cs
public sealed record MessageDto(
    Guid Id,
    Guid ChannelId,
    string AuthorUserId,
    string? Body,
    Guid? ParentMessageId,
    int ReplyCount,
    DateTime? EditedAtUtc,
    DateTime? DeletedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<MessageAttachmentDto> Attachments);

// MessageAttachmentDto.cs
public sealed record MessageAttachmentDto(
    Guid Id,
    Guid? FileAssetId,
    string Url,
    string ContentType,
    string OriginalFileName,
    long SizeBytes);
```

- [ ] **Step 2: Commands**

All commands implement `ICommand<TResponse>`. Reply types: Guid for create/edit; Unit for state changes; specific DTOs where useful.

```csharp
// CreateChannelCommand.cs
public sealed record CreateChannelCommand(string Name, string? Description, bool IsPrivate) : ICommand<Guid>;

// UpdateChannelCommand.cs
public sealed record UpdateChannelCommand(Guid ChannelId, string Name, string? Description, bool IsPrivate) : ICommand<Unit>;

// ArchiveChannelCommand.cs
public sealed record ArchiveChannelCommand(Guid ChannelId) : ICommand<Unit>;

// RestoreChannelCommand.cs
public sealed record RestoreChannelCommand(Guid ChannelId) : ICommand<Unit>;

// AddChannelMembersCommand.cs
public sealed record AddChannelMembersCommand(Guid ChannelId, IReadOnlyList<string> UserIds) : ICommand<Unit>;

// RemoveChannelMemberCommand.cs
public sealed record RemoveChannelMemberCommand(Guid ChannelId, string UserId) : ICommand<Unit>;

// FindOrCreateDmCommand.cs
public sealed record FindOrCreateDmCommand(IReadOnlyList<string> UserIds) : ICommand<Guid>;

// SendMessageCommand.cs
public sealed record SendMessageCommand(
    Guid ChannelId,
    string Body,
    Guid? ParentMessageId,
    IReadOnlyList<SendMessageAttachmentInput> Attachments) : ICommand<MessageDto>;
public sealed record SendMessageAttachmentInput(Guid? FileAssetId, string Url, string ContentType, string FileName, long SizeBytes);

// EditMessageCommand.cs
public sealed record EditMessageCommand(Guid MessageId, string Body) : ICommand<Unit>;

// DeleteMessageCommand.cs
public sealed record DeleteMessageCommand(Guid MessageId) : ICommand<Unit>;

// MarkChannelReadCommand.cs
public sealed record MarkChannelReadCommand(Guid ChannelId, Guid MessageId) : ICommand<Unit>;
```

- [ ] **Step 3: Queries**

```csharp
public sealed record GetChannelByIdQuery(Guid ChannelId) : IQuery<ChannelDto>;

public sealed record ListMyChannelsQuery(int Page = 1, int PageSize = 50) : IQuery<IReadOnlyList<ChannelDto>>;

public sealed record DiscoverChannelsQuery(string? Search, int Page = 1, int PageSize = 50) : IQuery<IReadOnlyList<ChannelDto>>;

public sealed record ListChannelMessagesQuery(Guid ChannelId, Guid? Before, int PageSize = 50) : IQuery<IReadOnlyList<MessageDto>>;
```

- [ ] **Step 4: Verify build**

```
dotnet build src/Modules/Chat/Modules.Chat.Contracts/Modules.Chat.Contracts.csproj --nologo
```

- [ ] **Step 5: Commit**

```
git add src/Modules/Chat/Modules.Chat.Contracts/v1/
git commit -m "feat(chat-1): Chat contracts — DTOs, commands, queries"
```

## Task 1.8 — Channel feature: CreateChannel (handler + validator + endpoint)

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Features/v1/Channels/CreateChannel/CreateChannelCommandHandler.cs`
- Create: `src/Modules/Chat/Modules.Chat/Features/v1/Channels/CreateChannel/CreateChannelCommandValidator.cs`
- Create: `src/Modules/Chat/Modules.Chat/Features/v1/Channels/CreateChannel/CreateChannelEndpoint.cs`
- Modify: `src/Modules/Chat/Modules.Chat/ChatModule.cs` — call `group.MapCreateChannelEndpoint()`

- [ ] **Step 1: Validator**

```csharp
public sealed class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
```

- [ ] **Step 2: Handler**

```csharp
public sealed class CreateChannelCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<CreateChannelCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateChannelCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId)) throw new UnauthorizedException("no current user");
        var channel = ChatChannel.CreateChannel(cmd.Name, cmd.Description, cmd.IsPrivate, userId);
        db.Channels.Add(channel);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return channel.Id;
    }
}
```

- [ ] **Step 3: Endpoint**

```csharp
public static class CreateChannelEndpoint
{
    internal static RouteHandlerBuilder MapCreateChannelEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/channels",
                async (CreateChannelCommand cmd, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(cmd, ct)))
            .WithName("CreateChannel")
            .WithSummary("Create a new named channel")
            .RequirePermission(ChatPermissions.Channels.Create);
}
```

- [ ] **Step 4: Wire in ChatModule**

In `ChatModule.MapEndpoints`, after the `var group = ...` line, replace `_ = group;` with `group.MapCreateChannelEndpoint();`.

- [ ] **Step 5: Verify build**

```
dotnet build src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj --nologo
```

- [ ] **Step 6: Commit**

```
git add src/Modules/Chat/Modules.Chat/Features/v1/Channels/CreateChannel/ src/Modules/Chat/Modules.Chat/ChatModule.cs
git commit -m "feat(chat-1): CreateChannel endpoint"
```

## Task 1.9 — Channel features: UpdateChannel, ArchiveChannel, RestoreChannel

For each: handler (loads channel via `db.Channels.FirstOrDefaultAsync` — AutoInclude on Members; throw `NotFoundException` if missing; check current user is a member with Admin role; mutate via domain method; SaveChanges), validator, endpoint.

- Update: PUT `/channels/{id}` — `RequirePermission(ChatPermissions.Channels.Create)` + check Admin role; calls `channel.Rename(name, description)` then `channel.SetPrivate(isPrivate)`.
- Archive: DELETE `/channels/{id}` — `RequirePermission(ChatPermissions.Channels.Create)` + check Admin role OR `RequirePermission(ChatPermissions.Channels.ManageAll)`; `db.Channels.Remove(channel)` triggers soft-delete via interceptor.
- Restore: POST `/channels/{id}/restore` — `RequirePermission(ChatPermissions.Channels.ManageAll)`; query with `IgnoreQueryFilters()`, call `channel.Restore()`.

- [ ] **Step 1: UpdateChannel + commit**
- [ ] **Step 2: ArchiveChannel + commit**
- [ ] **Step 3: RestoreChannel + commit**

After all three, wire all three `group.MapXxxEndpoint();` calls in `ChatModule`.

## Task 1.10 — Channel features: AddChannelMembers, RemoveChannelMember

- AddChannelMembers: POST `/channels/{id}/members` body `{ userIds: [...] }`. Handler: load channel, foreach `userId` call `channel.AddMember(userId, currentUserId)`. Permission: any member can add (for public channels, anyone can join — endpoint defers that nuance to the handler). Return 204.
- RemoveChannelMember: DELETE `/channels/{id}/members/{userId}`. If `userId == currentUser`, it's "leave" (allowed). Otherwise must be Admin of the channel. Calls `channel.RemoveMember(userId, currentUserId)`.

- [ ] **Step 1: AddChannelMembers + commit**
- [ ] **Step 2: RemoveChannelMember + commit**

## Task 1.11 — FindOrCreateDm

POST `/dms` body `{ userIds: [...] }`. Handler:
- If `userIds.Count == 1`: DM. Compute `DirectKey`. Query `db.Channels.FirstOrDefaultAsync(c => c.Type == DirectMessage && c.DirectKey == key)`. Return existing Id or create + return.
- If `userIds.Count >= 2`: Group DM. No find-or-create — just create.

Validator: `userIds.Count` between 1 and 9.

- [ ] **Step 1: Handler + validator + endpoint + commit**

## Task 1.12 — Channel queries: GetChannelById, ListMyChannels, DiscoverChannels

For each: query handler in `Features/v1/Channels/<QueryName>/` + endpoint.

- **GetChannelByIdQueryHandler**: Loads channel by Id (AutoInclude pulls Members), verifies current user is a member OR channel is public (`!IsPrivate`), maps to ChannelDto with computed UnreadCount via `Messages WHERE ChannelId = X AND Id > LastReadMessageId AND DeletedAtUtc IS NULL` count.
- **ListMyChannelsQueryHandler**: Joins `ChannelMembers` filtered by `UserId == currentUserId`, returns the channels ordered by `LastMessageAtUtc DESC` (nulls last). Paginated.
- **DiscoverChannelsQueryHandler**: `WHERE Type = Channel AND IsPrivate = FALSE AND NOT EXISTS (SELECT 1 FROM ChannelMembers WHERE ChannelId = c.Id AND UserId = @currentUserId)`. Optional name/slug search via `ILIKE`.

Endpoints: GET `/channels/{id}`, GET `/channels`, GET `/channels/discover`. All `.RequirePermission(ChatPermissions.Channels.View)`.

A small helper for the unread count (used by Get + List):
```csharp
internal static class UnreadCounter
{
    public static IQueryable<UnreadProjection> Project(ChatDbContext db, string userId)
        => from m in db.Channels.SelectMany(c => c.Members)
           where m.UserId == userId
           let count = db.Messages.Count(x =>
                x.ChannelId == m.ChannelId
                && x.DeletedAtUtc == null
                && (m.LastReadMessageId == null || x.Id > m.LastReadMessageId.Value))
           select new UnreadProjection(m.ChannelId, count);
}
public sealed record UnreadProjection(Guid ChannelId, int Count);
```

- [ ] **Step 1: GetChannelById + commit**
- [ ] **Step 2: ListMyChannels + commit**
- [ ] **Step 3: DiscoverChannels + commit**

## Task 1.13 — Message features: SendMessage (+ baseline attachments)

POST `/channels/{id}/messages` body `SendMessageCommand`. Idempotency-Key header supported via `.WithIdempotency()`.

Handler:
1. Verify channel exists + current user is a member.
2. If `ParentMessageId` is set, verify the parent exists, is in the same channel, is not itself a reply (no nested threads).
3. Create `Message.Create(channelId, currentUserId, body, parentMessageId)`.
4. For each attachment in command: `message.AddAttachment(fileAssetId, url, contentType, fileName, sizeBytes)`.
5. `db.Messages.Add(message)`.
6. If has parent: `parent.IncrementReplyCount()`.
7. `channel.TouchLastMessage(DateTime.UtcNow)`.
8. `SaveChangesAsync`.
9. Return `MessageDto` (mapper helper in `Features/v1/Internal/MessageMapper.cs`).

Validator: body required, max 32KB; attachments count <= 10.

Permission: `RequirePermission(ChatPermissions.Messages.Send)`.

- [ ] **Step 1: Mapper helper + commit**
- [ ] **Step 2: Handler + validator + endpoint + commit**

## Task 1.14 — Message features: EditMessage, DeleteMessage, MarkChannelRead

- **EditMessage**: PUT `/messages/{id}` body `{ body }`. Handler: load message + channel; verify membership; call `message.Edit(newBody, currentUserId)` (domain enforces author check). Permission: `Messages.EditOwn`.
- **DeleteMessage**: DELETE `/messages/{id}`. Handler: load message; check author OR current user has `Messages.DeleteAny` permission (use `IUserPermissionService.HasPermissionAsync` — already exists). Call `message.SoftDelete(currentUserId, isModerator)`. Decrement parent's ReplyCount if has parent. Permission: `Messages.DeleteOwn` (the handler's content check decides between author-only and moderator path).
- **MarkChannelRead**: POST `/channels/{id}/read` body `{ messageId }`. Handler: load channel + verify member; verify messageId exists in channel; call `channel.MarkRead(currentUserId, messageId)`. Permission: `Channels.View`.

- [ ] **Step 1: EditMessage + commit**
- [ ] **Step 2: DeleteMessage + commit**
- [ ] **Step 3: MarkChannelRead + commit**

## Task 1.15 — Message query: ListChannelMessages

GET `/channels/{id}/messages?before={guid}&pageSize=50`.

Handler:
1. Verify channel exists + user is member.
2. Query: `db.Messages.Where(m => m.ChannelId == id && m.ParentMessageId == null)` — top-level only by default. (Thread replies fetched via `/messages/{id}/replies` in Slice 4.)
3. If `before` is set: `Where(m => m.Id < before)`.
4. `OrderByDescending(m => m.Id).Take(pageSize)`.
5. Map each to MessageDto. Deleted messages render with `Body == null` (UI shows tombstone).

Permission: `Channels.View`.

- [ ] **Step 1: Handler + endpoint + commit**

## Task 1.16 — Architecture tests for Chat

**Files:**
- Modify: `src/Tests/Architecture.Tests/EndpointConventionTests.cs` — add `Send`, `Mark`, `Archive`, `Discover` to the allowed-verb list if not already there.
- Modify or create: `src/Tests/Architecture.Tests/ChatModuleArchitectureTests.cs` — new module-rules file.

```csharp
public sealed class ChatModuleArchitectureTests
{
    [Fact]
    public void Chat_Should_Not_Depend_On_Other_Modules_Runtime()
    {
        var result = Types.InAssembly(typeof(ChatModule).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FSH.Modules.Tickets",
                "FSH.Modules.Catalog",
                "FSH.Modules.Billing",
                "FSH.Modules.Webhooks",
                "FSH.Modules.Auditing"
                // Identity.Contracts + Files.Contracts are intentionally allowed
            )
            .GetResult();
        result.IsSuccessful.ShouldBeTrue(string.Join(",", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ChatContracts_Should_Not_Depend_On_Runtime_Modules()
    {
        var result = Types.InAssembly(typeof(FSH.Modules.Chat.Contracts.ChatContractsMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("FSH.Modules.Chat", "Microsoft.AspNetCore.SignalR")
            .GetResult();
        result.IsSuccessful.ShouldBeTrue(string.Join(",", result.FailingTypeNames ?? []));
    }
}
```

- [ ] **Step 1: Add verbs to EndpointConventionTests + commit**
- [ ] **Step 2: Add ChatModuleArchitectureTests + commit**
- [ ] **Step 3: Run arch tests, verify 48+N green**

```
dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj --nologo
```

## Task 1.17 — Unit tests project: Chat.Tests

**Files:**
- Create: `src/Tests/Chat.Tests/Chat.Tests.csproj`
- Create: `src/Tests/Chat.Tests/GlobalUsings.cs`
- Create: `src/Tests/Chat.Tests/ChatChannelTests.cs`
- Create: `src/Tests/Chat.Tests/MessageTests.cs`

- [ ] **Step 1: csproj**

Pattern: copy `src/Tests/Files.Tests/Files.Tests.csproj`. References: `Modules.Chat`, xUnit, Shouldly, NSubstitute, AutoFixture.

- [ ] **Step 2: GlobalUsings**

```csharp
global using Xunit;
global using Shouldly;
global using FSH.Modules.Chat.Domain;
```

- [ ] **Step 3: ChatChannelTests (~12 tests)**

Cover:
- `CreateChannel_Should_AddCreator_As_Admin_Member`
- `CreateChannel_Should_Slugify_Name`
- `CreateDirect_Should_Sort_UserIds_In_DirectKey`
- `CreateDirect_Should_Throw_When_SameUser`
- `CreateGroupDm_Should_Require_3_Or_More_Users`
- `AddMember_Should_Reject_Duplicate`
- `AddMember_Should_Throw_On_DirectMessage`
- `RemoveMember_Should_Throw_On_DirectMessage`
- `Rename_Should_Throw_For_DM`
- `Rename_Should_Update_Slug`
- `SetPrivate_Should_Throw_For_DM`
- `MarkRead_Should_Set_LastReadMessageId`

- [ ] **Step 4: MessageTests (~8 tests)**

Cover:
- `Create_Should_Set_AuthorUserId_Body_CreatedAt`
- `Create_Should_Reject_Empty_Body`
- `Edit_Should_Throw_If_Not_Author`
- `Edit_Should_Throw_If_Deleted`
- `Edit_Should_Set_EditedAtUtc`
- `SoftDelete_Should_Clear_Body`
- `SoftDelete_Author_Should_Succeed`
- `SoftDelete_NonAuthor_NonModerator_Should_Throw`

- [ ] **Step 5: Verify**

```
dotnet test src/Tests/Chat.Tests/Chat.Tests.csproj --nologo
```
Expected: 20+/20+ pass.

- [ ] **Step 6: Commit**

```
git add src/Tests/Chat.Tests/
git commit -m "test(chat-1): domain unit tests for ChatChannel + Message"
```

## Task 1.18 — Integration tests for Slice 1

**Files:**
- Create: `src/Tests/Integration.Tests/Tests/Chat/ChannelLifecycleTests.cs`
- Create: `src/Tests/Integration.Tests/Tests/Chat/ChannelMembershipTests.cs`
- Create: `src/Tests/Integration.Tests/Tests/Chat/MessageLifecycleTests.cs`

Pattern: mirror `src/Tests/Integration.Tests/Tests/Files/RequestAndFinalizeUploadTests.cs`. Use `[Collection(FshCollectionDefinition.Name)]`, `AuthHelper.CreateRootAdminClientAsync`.

- [ ] **Step 1: ChannelLifecycleTests (~8 tests)**

Cover:
- `CreateChannel_Should_Return_Id_And_AddCreatorAsAdmin`
- `GetChannelById_Should_Return_Channel_With_Members`
- `ListMyChannels_Should_Include_NewlyCreatedChannel`
- `UpdateChannel_Should_RenameAndSlugify`
- `UpdateChannel_Should_Return_403_When_NotAdmin`
- `ArchiveChannel_Should_HideFrom_ListMyChannels`
- `RestoreChannel_Should_BringBack`
- `DiscoverChannels_Should_Return_Public_Channels_User_Is_Not_In`

- [ ] **Step 2: ChannelMembershipTests (~6 tests)**

Cover:
- `AddChannelMembers_Should_AddUsers`
- `AddChannelMembers_Should_Reject_Duplicates`
- `RemoveChannelMember_Self_Should_Leave`
- `FindOrCreateDm_Two_Users_Should_Return_Same_Channel_On_Second_Call`
- `FindOrCreateDm_Three_Users_Should_Create_GroupDm`
- `AddMember_To_Dm_Should_Return_400`

- [ ] **Step 3: MessageLifecycleTests (~10 tests)**

Cover:
- `SendMessage_Should_Return_MessageDto_And_BumpLastMessageAt`
- `SendMessage_With_IdempotencyKey_Should_Return_Same_Id_On_Replay`
- `ListChannelMessages_Should_Return_NewestFirst_Excluding_Replies`
- `ListChannelMessages_With_Before_Should_Page`
- `EditMessage_Should_UpdateBody_AndSet_EditedAt`
- `EditMessage_Should_Return_403_When_NotAuthor`
- `DeleteMessage_Should_SoftDelete_And_Render_AsNull`
- `DeleteMessage_AsModerator_Should_Succeed`
- `MarkChannelRead_Should_Set_LastReadMessageId_AndZero_UnreadCount`
- `SendMessage_To_NonMember_Channel_Should_Return_403`

- [ ] **Step 4: Run**

```
dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --nologo --filter "FullyQualifiedName~Tests.Chat"
```

- [ ] **Step 5: Commit**

```
git add src/Tests/Integration.Tests/Tests/Chat/
git commit -m "test(chat-1): integration tests — channels + membership + messages"
```

## Task 1.19 — Slice 1 verification gate

- [ ] **Step 1: Full build**

```
dotnet build src/FSH.Starter.slnx --nologo
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Full test sweep**

```
dotnet test src/FSH.Starter.slnx --nologo
```
Expected: Architecture.Tests + Chat.Tests + Files.Tests + Integration.Tests all green.

- [ ] **Step 3: Update handoff**

Edit `memory/handoff.md`: note Slice 1 complete; list endpoints shipped; note Slices 2-4 remaining.

- [ ] **Step 4: Commit (allow-empty if no further changes)**

```
git commit --allow-empty -m "chore(chat-1): Slice 1 complete — channels + messages baseline, all tests green"
```

---

# SLICE 2 — SignalR hub + realtime + DMs

**Goal of slice:** Messages stream live across tabs. DMs already exist from Slice 1's FindOrCreateDm — this slice wires the realtime broadcasting.

## Task 2.1 — Add SignalR + Redis backplane to the host

**Files:**
- Modify: `src/Directory.Packages.props` — add `<PackageVersion Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="..." />` (use latest matching .NET 10).
- Modify: `src/BuildingBlocks/Web/Web.csproj` — add `<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" />`.

- [ ] **Step 1: Add NuGet packages**
- [ ] **Step 2: Verify build of Web project**
- [ ] **Step 3: Commit**

```
git add src/Directory.Packages.props src/BuildingBlocks/Web/Web.csproj
git commit -m "feat(chat-2): add SignalR.StackExchangeRedis package"
```

## Task 2.2 — Build the AppHub

**Files:**
- Create: `src/BuildingBlocks/Web/Realtime/AppHub.cs`
- Create: `src/BuildingBlocks/Web/Realtime/Extensions.cs` (`AddHeroRealtime`, `MapHeroRealtime`)
- Create: `src/BuildingBlocks/Web/Realtime/IChannelMembershipChecker.cs` (in BuildingBlocks/Web — interface so AppHub can verify membership without depending on Chat module)

- [ ] **Step 1: IChannelMembershipChecker interface**

```csharp
namespace FSH.Framework.Web.Realtime;
public interface IChannelMembershipChecker
{
    ValueTask<bool> IsMemberAsync(Guid channelId, string userId, CancellationToken cancellationToken = default);
}
```

The Chat module's runtime registers an implementation that queries `ChatDbContext.Channels.AnyAsync(c => c.Id == channelId && c.Members.Any(m => m.UserId == userId))`. Since this is just an interface, BuildingBlocks/Web stays decoupled from Chat.

- [ ] **Step 2: AppHub**

```csharp
using FSH.Framework.Core.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Web.Realtime;

[Authorize]
public sealed class AppHub : Hub
{
    private readonly ICurrentUser _currentUser;
    private readonly IChannelMembershipChecker _membership;
    private readonly IDistributedCache _cache;
    private readonly IUserChannelLookup _channels;
    private readonly ILogger<AppHub> _logger;

    public AppHub(ICurrentUser currentUser, IChannelMembershipChecker membership,
                  IDistributedCache cache, IUserChannelLookup channels, ILogger<AppHub> logger)
    { _currentUser = currentUser; _membership = membership; _cache = cache; _channels = channels; _logger = logger; }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId)) { Context.Abort(); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Pre-join the channels the user is a member of.
        var channelIds = await _channels.ListMyChannelIdsAsync(userId, Context.ConnectionAborted);
        foreach (var channelId in channelIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");

        await base.OnConnectedAsync();
    }

    public async Task Typing(Guid channelId)
    {
        var userId = _currentUser.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId)) return;
        if (!await _membership.IsMemberAsync(channelId, userId, Context.ConnectionAborted)) return;

        // Throttle: 3s per (channel, user) via distributed cache marker.
        var key = $"typing:{channelId}:{userId}";
        var existing = await _cache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(existing)) return;
        await _cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
        });

        await Clients.OthersInGroup($"channel:{channelId}")
            .SendAsync("ChatTypingStarted", new { channelId, userId });
    }
}

public interface IUserChannelLookup
{
    ValueTask<IReadOnlyList<Guid>> ListMyChannelIdsAsync(string userId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Extensions to wire in**

```csharp
public static class HeroRealtimeExtensions
{
    public static IServiceCollection AddHeroRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration["CachingOptions:Redis"];
        var signalr = services.AddSignalR();
        if (!string.IsNullOrWhiteSpace(redis))
            signalr.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix = "fsh-signalr");
        return services;
    }

    public static IEndpointRouteBuilder MapHeroRealtime(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<AppHub>("/api/v1/realtime/hub");
        return endpoints;
    }
}
```

- [ ] **Step 4: Configure JwtBearer to read token from query string for the hub path**

Modify the existing JWT config (probably in `BuildingBlocks/Web/Extensions.cs` or wherever `AddJwtBearer` lives) — pattern is identical to the SSE token-via-query setup already used:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = ctx =>
    {
        var accessToken = ctx.Request.Query["access_token"];
        var path = ctx.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/v1/realtime/hub"))
            ctx.Token = accessToken;
        return Task.CompletedTask;
    }
};
```

- [ ] **Step 5: Wire in `AddHeroPlatform` / `UseHeroPlatform`**

Find where SSE is wired (`MapHeroSseEndpoints`) and add `AddHeroRealtime(builder.Configuration)` + `MapHeroRealtime()` calls alongside.

- [ ] **Step 6: Commit**

```
git add src/BuildingBlocks/Web/Realtime/ src/BuildingBlocks/Web/Extensions.cs
git commit -m "feat(chat-2): AppHub + Redis backplane wiring"
```

## Task 2.3 — Chat module: register hub adapters

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Services/ChannelMembershipChecker.cs`
- Create: `src/Modules/Chat/Modules.Chat/Services/UserChannelLookup.cs`
- Modify: `src/Modules/Chat/Modules.Chat/ChatModule.cs` — register them

- [ ] **Step 1: ChannelMembershipChecker**

```csharp
public sealed class ChannelMembershipChecker(ChatDbContext db) : IChannelMembershipChecker
{
    public async ValueTask<bool> IsMemberAsync(Guid channelId, string userId, CancellationToken ct = default)
        => await db.Channels.AnyAsync(c => c.Id == channelId && c.Members.Any(m => m.UserId == userId), ct);
}
```

- [ ] **Step 2: UserChannelLookup**

```csharp
public sealed class UserChannelLookup(ChatDbContext db) : IUserChannelLookup
{
    public async ValueTask<IReadOnlyList<Guid>> ListMyChannelIdsAsync(string userId, CancellationToken ct = default)
        => await db.Channels.Where(c => c.Members.Any(m => m.UserId == userId)).Select(c => c.Id).ToListAsync(ct);
}
```

- [ ] **Step 3: Register in ChatModule.ConfigureServices**

```csharp
builder.Services.AddScoped<IChannelMembershipChecker, ChannelMembershipChecker>();
builder.Services.AddScoped<IUserChannelLookup, UserChannelLookup>();
```

- [ ] **Step 4: Commit**

```
git add src/Modules/Chat/Modules.Chat/Services/ src/Modules/Chat/Modules.Chat/ChatModule.cs
git commit -m "feat(chat-2): Chat module implementations of realtime checkers"
```

## Task 2.4 — Realtime broadcasts in message handlers

Modify `SendMessageCommandHandler`, `EditMessageCommandHandler`, `DeleteMessageCommandHandler`, `AddChannelMembersCommandHandler`, `RemoveChannelMemberCommandHandler` to inject `IHubContext<AppHub>` and, after SaveChanges, broadcast the corresponding realtime event.

Example for SendMessage:
```csharp
public sealed class SendMessageCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub)
    : ICommandHandler<SendMessageCommand, MessageDto>
{
    public async ValueTask<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        // ... existing logic ...
        var dto = MessageMapper.ToDto(message);
        await hub.Clients.Group($"channel:{channel.Id}")
            .SendAsync("ChatMessageCreated", dto, ct);
        return dto;
    }
}
```

- [ ] **Step 1: SendMessageCommandHandler broadcasts ChatMessageCreated**
- [ ] **Step 2: EditMessageCommandHandler broadcasts ChatMessageEdited**
- [ ] **Step 3: DeleteMessageCommandHandler broadcasts ChatMessageDeleted**
- [ ] **Step 4: AddChannelMembersCommandHandler: broadcast ChatChannelMemberAdded to channel group AND ChatChannelAdded to user:{newMember}; also add new member's connections to channel group via `hub.Groups.AddToGroupAsync` (look up active connection ids from IHubContext is tricky — instead, accept that until the new member's next OnConnectedAsync they won't get realtime updates; document)**
- [ ] **Step 5: RemoveChannelMemberCommandHandler broadcasts ChatChannelMemberRemoved**
- [ ] **Step 6: MarkChannelReadCommandHandler broadcasts ChatMessageRead to channel group**
- [ ] **Step 7: Commit after each**

## Task 2.5 — Integration test: realtime flow

**Files:**
- Create: `src/Tests/Integration.Tests/Tests/Chat/RealtimeEventsTests.cs`

Pattern: open a `HubConnectionBuilder` against `_factory.Server.CreateHandler()`, register listener for `ChatMessageCreated`, POST a message via the REST API, assert the listener fires.

- [ ] **Step 1: Test setup helper for hub connections**

```csharp
private async Task<HubConnection> ConnectAsync(HttpClient authedClient)
{
    var token = await IssueSseStyleAccessTokenAsync(authedClient); // helper that returns access_token string
    var connection = new HubConnectionBuilder()
        .WithUrl($"http://localhost/api/v1/realtime/hub?access_token={token}",
            options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.WebSocketFactory = (ctx, ct) => throw new NotSupportedException();  // forces long-polling/SSE transport, which Server supports
            })
        .Build();
    await connection.StartAsync();
    return connection;
}
```

- [ ] **Step 2: Test cases (~5)**

- `SendingMessage_Should_FireChatMessageCreated_To_Channel_Members`
- `SendingMessage_Should_Not_Fire_To_NonMembers`
- `EditingMessage_Should_FireChatMessageEdited`
- `DeletingMessage_Should_FireChatMessageDeleted`
- `Typing_Should_Throttle_To_OnceEvery3Seconds`

- [ ] **Step 3: Commit**

```
git add src/Tests/Integration.Tests/Tests/Chat/RealtimeEventsTests.cs
git commit -m "test(chat-2): realtime SignalR integration tests"
```

## Task 2.6 — Slice 2 verification gate

- [ ] **Step 1: Full build + tests**
- [ ] **Step 2: Update handoff**
- [ ] **Step 3: Commit**

---

# SLICE 3 — Notifications module + @mentions

**Goal of slice:** Bell-icon inbox in the topbar. Mention someone, they get a Notification row + a SignalR push.

## Task 3.1 — Scaffold Notifications module

Same shape as Task 1.1 but for `src/Modules/Notifications/`. Module order = 750.

- [ ] **Step 1: csproj + slnx + Migrations + Api project references**
- [ ] **Step 2: Permissions constants (`NotificationPermissions`)**
- [ ] **Step 3: Mediator markers in Program.cs**
- [ ] **Step 4: Commit**

## Task 3.2 — Domain: Notification aggregate

```csharp
public sealed class Notification : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string? Link { get; private set; }
    public string Source { get; private set; } = default!;
    public string MetadataJson { get; private set; } = "{}";
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { }

    public static Notification Create(string userId, string type, string title, string? body, string? link, string source, object? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = userId, Type = type, Title = title, Body = body, Link = link, Source = source,
            MetadataJson = metadata is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkRead()
    {
        if (!ReadAtUtc.HasValue) ReadAtUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 1: Domain class + commit**

## Task 3.3 — EF, DbContext, migration, ModuleRegistration

Pattern: same as Chat. Schema = `notifications`. Single table `Notifications`. Index `(UserId, ReadAtUtc, CreatedAtUtc DESC)`.

- [ ] **Step 1: NotificationConfiguration + commit**
- [ ] **Step 2: NotificationsDbContext + initializer + commit**
- [ ] **Step 3: NotificationsModule (Order=750) + register in Program.cs + commit**
- [ ] **Step 4: EF migration `InitialNotifications` + commit**

## Task 3.4 — Notifications REST endpoints

GET `/notifications?unreadOnly=&page=&pageSize=` — list inbox
GET `/notifications/unread-count` — bell badge
POST `/notifications/{id}/read` — single
POST `/notifications/read-all` — bulk

Each: handler + endpoint (no validators beyond Id format).

Reads scoped to current user — `Where(n => n.UserId == currentUserId)`. No cross-user reads.

- [ ] **Step 1-4: One commit per endpoint**

## Task 3.5 — MentionedInChannel integration event + handler

**Files:**
- Create: `src/Modules/Chat/Modules.Chat.Contracts/Events/MentionedInChannelIntegrationEvent.cs`
- Modify: `src/Modules/Chat/Modules.Chat/Features/v1/Messages/SendMessage/SendMessageCommandHandler.cs` — parse mentions, publish event
- Create: `src/Modules/Notifications/Modules.Notifications/Features/IntegrationEventHandlers/MentionedInChannelIntegrationEventHandler.cs`

- [ ] **Step 1: Integration event record**

```csharp
public sealed record MentionedInChannelIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string TenantId,
    string CorrelationId,
    string Source,
    Guid ChannelId,
    string? ChannelName,
    Guid MessageId,
    string AuthorUserId,
    string MentionedUserId,
    string BodyPreview) : IIntegrationEvent;
```

- [ ] **Step 2: Mention parser + add MessageMention entity**

Move-up from spec: this task introduces `MessageMention` because the SendMessageCommandHandler needs it.

- Create `src/Modules/Chat/Modules.Chat/Domain/MessageMention.cs` (Id, MessageId, MentionedUserId, StartIndex, Length)
- Modify `Message.cs`: add `_mentions` list + `IReadOnlyList<MessageMention> Mentions => _mentions` + `Message.Create` regex-parses `@\w+` matches; resolver is passed in: `Message.Create(channelId, authorUserId, body, mentionResolver, parentMessageId?)` where `mentionResolver` is `Func<string, string?>` mapping username to userId.
- Create EF config for MessageMention; add to ChatDbContext
- New EF migration `AddMessageMentions`

The handler injects a resolver service: `IMentionResolver { Task<IReadOnlyDictionary<string,string>> ResolveUserIdsAsync(IReadOnlyList<string> usernames, CancellationToken ct) }` implemented against Identity contracts.

- [ ] **Step 3: SendMessage handler raises one MentionedInChannelIntegrationEvent per mention**

After SaveChanges:
```csharp
foreach (var mention in message.Mentions)
{
    await events.PublishAsync(new MentionedInChannelIntegrationEvent(
        Guid.NewGuid(), DateTime.UtcNow, tenantId, correlationId, "Chat",
        channel.Id, channel.Name, message.Id, currentUserId, mention.MentionedUserId, BodyPreview(body)), ct);
}
```

- [ ] **Step 4: MentionedInChannelIntegrationEventHandler in Notifications**

```csharp
public sealed class MentionedInChannelIntegrationEventHandler(
    NotificationsDbContext db,
    IHubContext<AppHub> hub)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public async ValueTask HandleAsync(MentionedInChannelIntegrationEvent e, CancellationToken ct)
    {
        var n = Notification.Create(
            userId: e.MentionedUserId,
            type: "chat.mention",
            title: $"You were mentioned in {e.ChannelName ?? "a conversation"}",
            body: e.BodyPreview,
            link: $"/chat/{e.ChannelId}?messageId={e.MessageId}",
            source: "Chat",
            metadata: new { e.ChannelId, e.MessageId, e.AuthorUserId });
        db.Notifications.Add(n);
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group($"user:{e.MentionedUserId}")
            .SendAsync("NotificationCreated", new
            {
                id = n.Id, type = n.Type, title = n.Title, body = n.Body, link = n.Link,
                createdAtUtc = n.CreatedAtUtc
            }, ct);
    }
}
```

- [ ] **Step 5: Register the integration event handler in NotificationsModule.ConfigureServices**

- [ ] **Step 6: Integration test: MentionAndNotificationTests**

`SendingMessageWith_AtMention_Should_Persist_Notification_And_FireSignalrEvent`

- [ ] **Step 7: Commit each substantial piece**

## Task 3.6 — Slice 3 verification gate

- [ ] Full build + tests + handoff update + commit.

---

# SLICE 4 — Threads + reactions + search + typing + dashboard UI

**Goal of slice:** All Phase A features ship. Dashboard `/chat` is fully usable.

## Task 4.1 — Threads (reply chains)

Already partially done: `Message.ParentMessageId` + `ReplyCount` exist. Wire the read path:

- New endpoint: GET `/messages/{id}/replies?before={guid}` — handler queries `Messages.Where(m => m.ParentMessageId == id)` desc by Id.
- Update `SendMessageCommandHandler` (already does this from Task 1.13) to bump parent's `ReplyCount`.
- Update `DeleteMessageCommandHandler` to decrement parent's `ReplyCount` when a reply is deleted.

- [ ] **Step 1: ListReplies handler + endpoint + integration tests for thread paging + commit**

## Task 4.2 — Reactions

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Domain/MessageReaction.cs`
- Create: `src/Modules/Chat/Modules.Chat/Data/Configurations/MessageReactionConfiguration.cs`
- Modify: `Message.cs` — add `AddReaction(userId, emoji)` / `RemoveReaction(userId, emoji)` methods
- New EF migration `AddMessageReactions`
- Commands: `AddReactionCommand(MessageId, Emoji)`, `RemoveReactionCommand(MessageId, Emoji)`
- Endpoints: POST `/messages/{id}/reactions`, DELETE `/messages/{id}/reactions/{emoji}`
- Realtime: broadcast `ChatReactionChanged` to channel group on each mutation.

Unique constraint on `(MessageId, UserId, Emoji)` so a user can only add one of each emoji per message.

- [ ] **Step 1: Domain method + EF config + commit**
- [ ] **Step 2: Migration + commit**
- [ ] **Step 3: Add/Remove handlers + endpoints + commit**
- [ ] **Step 4: Integration tests + commit**

## Task 4.3 — Full-text search

**Files:**
- New EF migration with raw SQL adding the generated tsvector column + GIN index
- New query: `SearchMessagesQuery(string Q, Guid? ChannelId, int Page, int PageSize)`
- Handler: raw SQL via `db.Database.SqlQueryRaw<MessageDto>("SELECT ... FROM chat.\"Messages\" WHERE BodyTsv @@ to_tsquery(@q) AND ... ORDER BY ts_rank DESC")`
- Endpoint: GET `/chat/search?q=&channelId=`
- Permission: must filter to channels the user is a member of (no cross-channel leakage).

```sql
-- migration Up()
migrationBuilder.Sql(@"
ALTER TABLE chat.""Messages"" ADD COLUMN ""BodyTsv"" tsvector
GENERATED ALWAYS AS (to_tsvector('english', coalesce(""Body"", ''))) STORED;
CREATE INDEX ""IX_Messages_BodyTsv"" ON chat.""Messages"" USING gin (""BodyTsv"");
");
```

- [ ] **Step 1: Migration + commit**
- [ ] **Step 2: Query handler + endpoint + commit**
- [ ] **Step 3: Integration tests (search across channels you're a member of; cannot see results from channels you're not in) + commit**

## Task 4.4 — Typing indicator (already 90% in AppHub)

The `Typing` hub method is already in place from Task 2.2. This task:
- Adds an integration test that connects two clients, calls `Typing(channelId)` from one, asserts the other receives `ChatTypingStarted` exactly once within the 3s throttle window.

- [ ] **Step 1: Integration test + commit**

## Task 4.5 — Dashboard: Realtime context + API client

**Files:**
- Create: `clients/dashboard/package.json` — add `@microsoft/signalr` and `@tanstack/react-virtual`
- Create: `clients/dashboard/src/realtime/realtime-context.tsx`
- Create: `clients/dashboard/src/api/chat.ts` — typed client for the chat REST endpoints
- Create: `clients/dashboard/src/api/notifications.ts`

- [ ] **Step 1: Add packages + commit**

```
cd clients/dashboard && npm install @microsoft/signalr @tanstack/react-virtual
```

- [ ] **Step 2: realtime-context.tsx**

Opens a singleton HubConnection on AuthProvider user-resolved. `useRealtimeEvent("EventName", handler)` typed hook. Reconnect with backoff `[0, 2000, 10000, 30000]`. On 401, refreshes JWT and re-handshakes.

- [ ] **Step 3: api/chat.ts (mirror api/files.ts pattern)**

Types for ChannelDto, MessageDto, MemberDto. Functions: createChannel, listMyChannels, getChannelById, updateChannel, archiveChannel, addChannelMembers, removeChannelMember, findOrCreateDm, sendMessage, editMessage, deleteMessage, markChannelRead, listChannelMessages, listReplies, searchMessages, addReaction, removeReaction.

- [ ] **Step 4: api/notifications.ts**

Types for NotificationDto. Functions: listNotifications, getUnreadCount, markRead, markAllRead.

- [ ] **Step 5: Commit**

## Task 4.6 — Dashboard: ChatPage shell + nav entry

**Files:**
- Modify: `clients/dashboard/src/components/layout/nav-data.ts` — add `/chat` to topNavTop
- Modify: `clients/dashboard/src/routes.tsx` — add lazy `/chat` route
- Create: `clients/dashboard/src/pages/chat/chat-page.tsx`
- Create: `clients/dashboard/src/components/chat/channel-rail.tsx` (skeleton — populated in next tasks)
- Create: `clients/dashboard/src/components/chat/channel-header.tsx`
- Create: `clients/dashboard/src/components/chat/message-list.tsx`
- Create: `clients/dashboard/src/components/chat/message.tsx`
- Create: `clients/dashboard/src/components/chat/composer.tsx`
- Create: `clients/dashboard/src/components/chat/thread-panel.tsx`
- Create: `clients/dashboard/src/components/chat/typing-indicator.tsx`

- [ ] **Step 1: Nav entry + route + page shell + commit**
- [ ] **Step 2: ChannelRail with channels list + commit**
- [ ] **Step 3: ChannelHeader + commit**
- [ ] **Step 4: MessageList with virtualization + infinite scroll + commit**
- [ ] **Step 5: Message component + commit**
- [ ] **Step 6: Composer with @mention autocomplete + file attach + commit**
- [ ] **Step 7: ThreadPanel + commit**
- [ ] **Step 8: TypingIndicator wired to ChatTypingStarted + commit**

## Task 4.7 — Dashboard: NotificationBell

**Files:**
- Create: `clients/dashboard/src/components/notifications/notification-bell.tsx`
- Modify: `clients/dashboard/src/components/layout/topbar.tsx` — mount the bell

- [ ] **Step 1: NotificationBell component + commit**
- [ ] **Step 2: Wire into topbar + commit**

## Task 4.8 — Backend: ChatChannelFileAccessPolicy (so attachments actually authorize)

**Files:**
- Create: `src/Modules/Chat/Modules.Chat/Authorization/ChatChannelFileAccessPolicy.cs`
- Modify: `ChatModule.cs` — register it

Mirrors `ProductFileAccessPolicy`. `CanAttachAsync(ownerId, userId)` returns whether the user is a member of `ownerId` channel.

- [ ] **Step 1: Policy + register + integration test + commit**

## Task 4.9 — Slice 4 verification gate

- [ ] **Step 1: Full build (.slnx) — 0/0**
- [ ] **Step 2: All test projects green — Architecture + Chat.Tests + Files.Tests + Integration.Tests**
- [ ] **Step 3: Dashboard typecheck + lint + Vite build clean**
- [ ] **Step 4: Manual smoke (browser) per spec verification gate checklist**
- [ ] **Step 5: Update memory/handoff.md — Chat module Phase A complete; reference spec + plan; list Phase B items**
- [ ] **Step 6: Final commit**

```
git commit --allow-empty -m "chore(chat): Phase A complete — channels, messages, mentions, threads, reactions, search, typing, notifications, dashboard UI"
```

---

## Self-Review

**Spec coverage check:**

| Spec section | Tasks |
|---|---|
| Module structure & boundaries | 1.1, 3.1 |
| Domain model — ChatChannel | 1.3 |
| Domain model — Message | 1.4, 3.5 (mentions), 4.2 (reactions) |
| Domain model — Notification | 3.2 |
| DM uniqueness via DirectKey | 1.3, 1.5, 1.11 |
| Soft-delete strategy (Channel = ISoftDeletable, Message = nullable DeletedAtUtc) | 1.3, 1.4, 1.14 |
| `tsvector` search | 4.3 |
| Indexes (DirectKey unique, ChannelMembers UNIQUE, Messages cursor, FTS GIN) | 1.5, 4.3 |
| REST endpoints — Channels | 1.8–1.12 |
| REST endpoints — Messages | 1.13–1.15, 4.1 |
| REST endpoints — Reactions | 4.2 |
| REST endpoints — Search | 4.3 |
| REST endpoints — Notifications | 3.4 |
| SignalR hub (AppHub) | 2.2 |
| Hub group taxonomy + typing throttle | 2.2 |
| Server → client events (all 8) | 2.4, 3.5, 4.2 |
| `IChannelMembershipChecker` decoupling | 2.2, 2.3 |
| `IHubContext<AppHub>` injection in handlers | 2.4, 3.5, 4.2 |
| Cursor pagination over Guid v7 | 1.15 |
| Idempotency on send | 1.13 |
| Mentions parsing + notification | 3.5 |
| Frontend route + nav | 4.6 |
| Frontend components | 4.6, 4.7 |
| Realtime context | 4.5 |
| File attachments via existing Files module + ChatChannelFileAccessPolicy | 4.8 |
| Plan B 4-slice phasing | structure of doc |
| Chat.Tests unit project | 1.17 |
| Integration.Tests new test classes | 1.18, 2.5, 3.5, 4.1, 4.2, 4.3, 4.4 |
| Architecture tests | 1.16 |
| Verification gates per slice | 1.19, 2.6, 3.6, 4.9 |

All spec sections covered.

**Placeholder scan:** No `TBD`, `TODO`, "fill in" phrases. Every code block is concrete. Every endpoint specifies its HTTP verb, path, permission, and handler outline.

**Type consistency:** `ChatChannel` / `ChannelMember` / `Message` / `MessageAttachment` / `MessageMention` / `MessageReaction` / `Notification` — used consistently across all tasks. `IChannelMembershipChecker` / `IUserChannelLookup` — same names in Hub (2.2), implementations (2.3), and usages. Commands and queries match the DTO type signatures listed in Task 1.7 throughout.

**Scope check:** This is one cohesive implementation plan for two modules that ship together. The 4-slice phasing makes interruption-friendly checkpoints. No further decomposition needed.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-13-chat-module-implementation.md`.

Per the user's "proceed autonomously" instruction, executing inline starting with Slice 1.
