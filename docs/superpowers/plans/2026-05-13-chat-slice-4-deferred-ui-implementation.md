# Chat Slice 4 Deferred UI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the three deferred UI items from the Slice 4 polish pass — ThreadPanel, mention profile-peek popover, and DM rail avatars — without backend changes or new packages.

**Architecture:** Pure client-side work in `clients/dashboard/src/`. ThreadPanel is a new overlay component over the right portion of the active channel; it reuses `<Message>` and `<Composer>` (which already accepts `parentMessageId`). Mention profile-peek refactors the existing `MentionPill` into a Radix DropdownMenu trigger that resolves the user via `searchUsers`. DM rail avatars add an `xs` size to the Avatar primitive and swap the Users2 icon for the partner's avatar on 1-on-1 DM rows.

**Tech Stack:** React 19, TanStack Query, `@radix-ui/react-dropdown-menu` (already installed), lucide-react, sonner, Vite.

**Spec:** `docs/superpowers/specs/2026-05-13-chat-slice-4-deferred-ui.md`

---

## File Structure

**Create:**
- `clients/dashboard/src/pages/chat/thread-panel.tsx` — the panel UI + replies query + realtime handlers

**Modify:**
- `clients/dashboard/src/pages/chat/chat-page.tsx` — wire `replyParentId` state, render `<ThreadPanel>`, add `relative` to the flex container
- `clients/dashboard/src/pages/chat/message.tsx` — refactor `MentionPill` into a profile-peek DropdownMenu
- `clients/dashboard/src/pages/chat/channel-rail.tsx` — swap icon for `<Avatar>` on 1-on-1 DM rows
- `clients/dashboard/src/components/ui/avatar.tsx` — add `"xs"` size
- `clients/dashboard/src/lib/use-user-display.ts` — add `useUserByUsername(username)` helper

**No tests in this codebase for these surfaces** — the dashboard ships UI tests only when stabilised (per UI Hardening Sprint scope). Verification is `tsc -b` + `vite build` + `eslint` + manual browser smoke test against a running Aspire host. Each task ends with one or more of those gates.

---

## Slice A — DM rail avatars (smallest, validates plumbing)

### Task A.1: Add `xs` size to Avatar primitive

**Files:**
- Modify: `clients/dashboard/src/components/ui/avatar.tsx`

- [ ] **Step 1: Update the `AvatarSize` type and size maps**

Open `clients/dashboard/src/components/ui/avatar.tsx`.

Change the type union from `"sm" | "md" | "lg"` to include `"xs"`:

```ts
type AvatarSize = "xs" | "sm" | "md" | "lg";
```

Add `xs` entries to both size maps:

```ts
const sizeClass: Record<AvatarSize, string> = {
  xs: "h-5 w-5 text-[10px]",
  sm: "h-7 w-7 text-[11px]",
  md: "h-9 w-9 text-[13px]",
  lg: "h-12 w-12 text-[16px]",
};

const dotSize: Record<AvatarSize, string> = {
  xs: "h-1 w-1",
  sm: "h-1.5 w-1.5",
  md: "h-2 w-2",
  lg: "h-2.5 w-2.5",
};
```

- [ ] **Step 2: Verify build**

Run: `cd clients/dashboard && npx tsc -b`
Expected: 0 errors. The new size key is additive; no existing call site breaks.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/components/ui/avatar.tsx
git commit -m "feat(ui): add xs size to Avatar primitive"
```

### Task A.2: Render partner avatar on 1-on-1 DM rows

**Files:**
- Modify: `clients/dashboard/src/pages/chat/channel-rail.tsx`

- [ ] **Step 1: Import Avatar**

Add at the top of `channel-rail.tsx` (Avatar is already imported around line 23, confirm — if missing, add):

```ts
import { Avatar } from "@/components/ui/avatar";
```

- [ ] **Step 2: Replace the icon render in ChannelRow**

In `channel-rail.tsx`, find the `ChannelRow` function (~line 228). The current icon render is:

```tsx
<Icon className="h-3.5 w-3.5 shrink-0" aria-hidden />
```

Replace it with a conditional that uses the avatar for 1-on-1 DMs:

```tsx
{channel.type === 0 && otherDmMember ? (
  <Avatar
    name={dmPartner.name}
    src={dmPartner.imageUrl ?? null}
    size="xs"
    className="shrink-0"
  />
) : (
  <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden />
)}
```

`otherDmMember` and `dmPartner` are already resolved earlier in the function (lines ~241–245). No new hooks.

- [ ] **Step 3: Verify build**

Run: `cd clients/dashboard && npx tsc -b && npx vite build`
Expected: 0 errors.

- [ ] **Step 4: Lint**

Run: `cd clients/dashboard && npx eslint src/pages/chat/channel-rail.tsx`
Expected: 0 errors, 0 new warnings.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/pages/chat/channel-rail.tsx
git commit -m "feat(chat-4): partner avatar on 1-on-1 DM rail rows"
```

---

## Slice B — Mention profile-peek popover

### Task B.1: Add `useUserByUsername` helper

**Files:**
- Modify: `clients/dashboard/src/lib/use-user-display.ts`

- [ ] **Step 1: Inspect existing searchUsers shape**

Open `clients/dashboard/src/api/identity.ts` and confirm `searchUsers` returns `{ items: UserDto[]; ... }`. Confirm `UserDto` exposes `id`, `userName`, `firstName`, `lastName`, `email`, `imageUrl`.

(If `searchUsers` signature differs, adjust the queryFn accordingly. The existing call in `composer.tsx:58` confirms the shape.)

- [ ] **Step 2: Add the helper at the bottom of `use-user-display.ts`**

```ts
import { searchUsers, type UserDto } from "@/api/identity";

export type UserByUsername = {
  resolved: UserDto | null;
  loading: boolean;
  error: boolean;
};

/**
 * Resolves an @username (case-insensitive) to a full UserDto via
 * /api/v1/identity/users/search. Used by the mention profile peek so
 * the popover can show the user's avatar / email and open a DM.
 *
 * Cached separately from useUserDisplay (which is keyed by userId).
 * Two consumers of the same @handle on the same page share one fetch.
 *
 * `enabled` should be controlled by the caller — passing `false`
 * keeps the query dormant until the popover actually opens.
 */
export function useUserByUsername(
  username: string | null | undefined,
  enabled: boolean,
): UserByUsername {
  const normalized = (username ?? "").trim().toLowerCase();

  const query = useQuery({
    queryKey: ["identity", "by-username", normalized],
    queryFn: async () => {
      const page = await searchUsers({ search: normalized, pageSize: 5, isActive: true });
      const items: UserDto[] = page.items ?? [];
      // Case-insensitive exact match on userName.
      return items.find((u) => (u.userName ?? "").toLowerCase() === normalized) ?? null;
    },
    enabled: enabled && normalized.length > 0,
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
    retry: 1,
  });

  return {
    resolved: query.data ?? null,
    loading: query.isPending && enabled,
    error: query.isError,
  };
}
```

The `import { searchUsers, type UserDto } from "@/api/identity"` should be added at the top of the file if not already present.

- [ ] **Step 3: Verify build**

Run: `cd clients/dashboard && npx tsc -b`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/lib/use-user-display.ts
git commit -m "feat(chat-4): useUserByUsername helper for mention resolution"
```

### Task B.2: Refactor MentionPill into a profile-peek DropdownMenu

**Files:**
- Modify: `clients/dashboard/src/pages/chat/message.tsx`

- [ ] **Step 1: Add the new imports**

At the top of `message.tsx`, add the following imports (some may already exist — merge into existing lines, don't duplicate):

```ts
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { findOrCreateDm } from "@/api/chat";
import { Avatar } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuth } from "@/auth/use-auth";
import { useUserByUsername } from "@/lib/use-user-display";
```

`useState`, `useMutation`, `useQueryClient`, `toast` are already imported. `Button` is already imported. Verify.

- [ ] **Step 2: Replace the `MentionPill` function**

Find the current `MentionPill` (around line 191). Replace it entirely with:

```tsx
function MentionPill({ username }: { username: string }) {
  const [open, setOpen] = useState(false);
  const { user } = useAuth();
  const { resolved, loading, error } = useUserByUsername(username, open);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const isSelf = !!resolved?.id && resolved.id === user?.id;

  const copy = () => {
    const text = `@${username}`;
    void navigator.clipboard
      ?.writeText(text)
      .then(() => toast.success(`Copied ${text}`))
      .catch(() => toast.error("Couldn't copy to clipboard"));
  };

  const dmMutation = useMutation({
    mutationFn: () => {
      if (!resolved?.id) throw new Error("Cannot DM an unresolved user");
      return findOrCreateDm([resolved.id]);
    },
    onSuccess: (channelId) => {
      void queryClient.invalidateQueries({ queryKey: ["chat", "my-channels"] });
      setOpen(false);
      navigate(`/chat/${channelId}`);
    },
    onError: () => toast.error("Couldn't open DM"),
  });

  const displayName =
    [resolved?.firstName, resolved?.lastName].filter(Boolean).join(" ").trim() ||
    resolved?.userName ||
    resolved?.email ||
    `@${username}`;

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="chat-mention"
          title={`Mention of ${username}`}
          aria-label={`Mention of ${username}, click to open profile`}
        >
          @{username}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[280px] p-0">
        <div className="flex items-start gap-3 p-3">
          <Avatar
            name={displayName}
            src={resolved?.imageUrl ?? null}
            size="md"
            className="shrink-0"
          />
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-1.5">
              <span className="truncate text-sm font-semibold tracking-tight text-[var(--color-foreground)]">
                {loading ? `@${username}` : displayName}
              </span>
              {isSelf && (
                <span className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[9px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
                  you
                </span>
              )}
            </div>
            <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
              @{resolved?.userName ?? username}
            </div>
            {resolved?.email && (
              <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                {resolved.email}
              </div>
            )}
            {loading && (
              <div className="mt-1 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                Looking up…
              </div>
            )}
            {!loading && !resolved && !error && (
              <div className="mt-1 text-xs italic text-[var(--color-muted-foreground)]">
                User not found.
              </div>
            )}
            {error && (
              <div className="mt-1 text-xs italic text-[var(--color-destructive)]">
                Couldn&apos;t load this user.
              </div>
            )}
          </div>
        </div>
        <div className="flex items-center justify-end gap-1.5 border-t border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-2">
          <Button
            size="sm"
            variant="ghost"
            onClick={copy}
            disabled={dmMutation.isPending}
          >
            Copy @{resolved?.userName ?? username}
          </Button>
          <Button
            size="sm"
            disabled={!resolved?.id || isSelf || dmMutation.isPending}
            onClick={() => dmMutation.mutate()}
            title={isSelf ? "That's you" : undefined}
          >
            {dmMutation.isPending ? "Opening…" : "Open DM"}
          </Button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
```

- [ ] **Step 3: Verify build**

Run: `cd clients/dashboard && npx tsc -b && npx vite build`
Expected: 0 errors.

- [ ] **Step 4: Lint**

Run: `cd clients/dashboard && npx eslint src/pages/chat/message.tsx src/lib/use-user-display.ts`
Expected: 0 errors, 0 new warnings.

- [ ] **Step 5: Manual browser smoke**

With Aspire running, open `/chat`, find a message with an `@mention`, click the pill. Verify:
- Popover opens with avatar, name, @handle, email.
- "Copy @handle" copies + toasts.
- "Open DM" creates / finds a DM and navigates to `/chat/{id}`.
- Clicking your own mention shows the "you" badge and disables Open DM.
- Clicking a fake `@nobody` pill (insert one manually if needed) shows "User not found" with only Copy enabled.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/chat/message.tsx clients/dashboard/src/lib/use-user-display.ts
git commit -m "feat(chat-4): mention pill opens profile peek with Open DM action"
```

(Step 6 may already include `use-user-display.ts` from Task B.1 if you batched commits — that's fine.)

---

## Slice C — ThreadPanel

### Task C.1: Create the ThreadPanel skeleton

**Files:**
- Create: `clients/dashboard/src/pages/chat/thread-panel.tsx`

- [ ] **Step 1: Write the component file**

Create `clients/dashboard/src/pages/chat/thread-panel.tsx` with:

```tsx
import { useEffect, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { MessageSquareReply, X } from "lucide-react";
import {
  listMessageReplies,
  type ChannelTypeValue,
  type MessageDto,
} from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { Composer } from "@/pages/chat/composer";
import { Message } from "@/pages/chat/message";
import { canMerge } from "@/pages/chat/chat-utils";
import { cn } from "@/lib/cn";

/**
 * Slack-style thread overlay. Floats over the right ~380px of the active
 * channel's main column on desktop; full-bleed on mobile. The MessageList
 * underneath stays full-width (its virtualizer never re-measures).
 *
 * Parent message is read from the channel's message-list cache when
 * available; if it's older than the loaded window, we render a tombstone
 * placeholder instead of adding a new endpoint.
 */
export function ThreadPanel({
  channelId,
  channelTitle,
  channelType,
  parentMessageId,
  selfUserId,
  onClose,
}: {
  channelId: string;
  channelTitle: string;
  channelType: ChannelTypeValue;
  parentMessageId: string;
  selfUserId?: string;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const repliesKey = useMemo(
    () => ["chat", "replies", parentMessageId] as const,
    [parentMessageId],
  );

  // Try to pull the parent out of the channel's message-list cache first.
  const parent = useMemo<MessageDto | null>(() => {
    const cached = queryClient.getQueryData<MessageDto[]>([
      "chat",
      "messages",
      channelId,
    ]);
    return cached?.find((m) => m.id === parentMessageId) ?? null;
  }, [queryClient, channelId, parentMessageId]);

  const repliesQuery = useQuery({
    queryKey: repliesKey,
    queryFn: () => listMessageReplies(parentMessageId, { pageSize: 100 }),
    staleTime: 0,
  });

  const replies = useMemo(() => repliesQuery.data ?? [], [repliesQuery.data]);

  // ── Realtime: scope events to this thread ─────────────────────────────
  useRealtimeEvent<MessageDto>(
    "ChatMessageCreated",
    (payload) => {
      if (payload.parentMessageId !== parentMessageId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(repliesKey, (prev) => {
        if (!prev) return [payload];
        if (prev.some((m) => m.id === payload.id)) return prev;
        return [...prev, payload];
      });
    },
    [parentMessageId],
  );

  useRealtimeEvent<MessageDto>(
    "ChatMessageEdited",
    (payload) => {
      if (payload.id === parentMessageId) {
        // Patch the parent in the message-list cache so the body updates here too.
        queryClient.setQueryData<MessageDto[] | undefined>(
          ["chat", "messages", channelId],
          (prev) => prev?.map((m) => (m.id === payload.id ? payload : m)),
        );
        return;
      }
      if (payload.parentMessageId !== parentMessageId) return;
      queryClient.setQueryData<MessageDto[] | undefined>(repliesKey, (prev) =>
        prev?.map((m) => (m.id === payload.id ? payload : m)),
      );
    },
    [parentMessageId, channelId],
  );

  useRealtimeEvent<{ channelId: string; messageId: string; parentMessageId?: string | null }>(
    "ChatMessageDeleted",
    (payload) => {
      if (payload.messageId === parentMessageId) {
        // Parent gone — close the panel.
        onClose();
        return;
      }
      // A reply was deleted somewhere; cheap to invalidate.
      void queryClient.invalidateQueries({ queryKey: repliesKey });
    },
    [parentMessageId, onClose],
  );

  useRealtimeEvent<{ channelId: string; messageId: string }>(
    "ChatReactionChanged",
    (payload) => {
      if (
        payload.messageId !== parentMessageId &&
        !replies.some((r) => r.id === payload.messageId)
      ) {
        return;
      }
      void queryClient.invalidateQueries({ queryKey: repliesKey });
      if (payload.messageId === parentMessageId) {
        void queryClient.invalidateQueries({
          queryKey: ["chat", "messages", channelId],
        });
      }
    },
    [parentMessageId, channelId, replies],
  );

  // ── Esc to close ──────────────────────────────────────────────────────
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  return (
    <aside
      className={cn(
        "absolute right-0 top-0 z-20 flex h-full w-full flex-col md:w-[380px]",
        "border-l border-[var(--color-border)] bg-[var(--color-surface-1)]",
        "shadow-[-12px_0_28px_-12px_oklch(0_0_0_/_0.18)]",
      )}
      aria-label="Thread"
    >
      {/* Header — mirrors the channel header height. */}
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-[var(--color-border)] px-4">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-[var(--color-surface-3)] text-[var(--color-muted-foreground)]"
        >
          <MessageSquareReply className="h-3.5 w-3.5" />
        </span>
        <h2 className="text-display flex-1 truncate text-sm font-semibold tracking-tight">
          Thread
        </h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Close thread"
          className={cn(
            "grid h-7 w-7 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          )}
        >
          <X className="h-3.5 w-3.5" />
        </button>
      </header>

      {/* Scrollable body */}
      <div className="min-h-0 flex-1 overflow-y-auto">
        {parent ? (
          <Message message={parent} selfUserId={selfUserId} isMerged={false} />
        ) : (
          <div className="px-4 py-3 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Parent message unavailable.
          </div>
        )}

        <div className="px-4">
          <div className="chat-day-rule">
            <span>
              {replies.length === 0
                ? "Start the thread"
                : `${replies.length} ${replies.length === 1 ? "reply" : "replies"}`}
            </span>
          </div>
        </div>

        {repliesQuery.isLoading ? (
          <div className="px-4 py-3 font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Loading replies…
          </div>
        ) : (
          replies.map((reply, i) => {
            const prev = i > 0 ? replies[i - 1] : null;
            const merged = prev !== null && canMerge(prev, reply);
            return (
              <Message
                key={reply.id}
                message={reply}
                selfUserId={selfUserId}
                isMerged={merged}
              />
            );
          })
        )}
      </div>

      {/* Composer reuses the existing primitive with parentMessageId. */}
      <Composer
        channelId={channelId}
        channelTitle={channelTitle}
        channelType={channelType}
        parentMessageId={parentMessageId}
      />
    </aside>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd clients/dashboard && npx tsc -b`
Expected: 0 errors.

- [ ] **Step 3: Lint**

Run: `cd clients/dashboard && npx eslint src/pages/chat/thread-panel.tsx`
Expected: 0 errors, 0 new warnings.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/pages/chat/thread-panel.tsx
git commit -m "feat(chat-4): ThreadPanel component — overlay + replies query + realtime"
```

### Task C.2: Wire ThreadPanel into chat-page.tsx

**Files:**
- Modify: `clients/dashboard/src/pages/chat/chat-page.tsx`

- [ ] **Step 1: Add the import**

At the top of `chat-page.tsx`, add:

```ts
import { useState } from "react";  // useState may already be present via other imports — merge.
import { ThreadPanel } from "@/pages/chat/thread-panel";
```

Verify `useState` is imported (it's needed if not already).

- [ ] **Step 2: Add reply-parent state to ActiveChannel**

Find `function ActiveChannel({ channelId, selfUserId })` (around line 114). Add at the top of the function body, after `useQueryClient()`:

```tsx
const [replyParentId, setReplyParentId] = useState<string | null>(null);

// Reset thread state when switching channels.
useEffect(() => {
  setReplyParentId(null);
}, [channelId]);
```

(`useEffect` is already imported.)

- [ ] **Step 3: Make the outer container positioned + pass `onReply` to MessageList**

In the return JSX of `ActiveChannel`, change the outer wrapper from:

```tsx
<div className="flex h-full min-h-0 flex-col">
```

to:

```tsx
<div className="relative flex h-full min-h-0 flex-col">
```

Then change the `<MessageList>` call to pass `onReply`:

```tsx
<MessageList
  channelId={channelId}
  selfUserId={selfUserId}
  lastReadMessageId={lastReadMessageId}
  onReply={setReplyParentId}
/>
```

- [ ] **Step 4: Render the ThreadPanel below Composer**

Inside the same outer `<div>` (as the last child, after `<Composer />`), add:

```tsx
{replyParentId && (
  <ThreadPanel
    channelId={channelId}
    channelTitle={title}
    channelType={channel.type}
    parentMessageId={replyParentId}
    selfUserId={selfUserId}
    onClose={() => setReplyParentId(null)}
  />
)}
```

`title`, `channel.type`, `selfUserId` are all already in scope.

- [ ] **Step 5: Verify build**

Run: `cd clients/dashboard && npx tsc -b && npx vite build`
Expected: 0 errors.

- [ ] **Step 6: Lint**

Run: `cd clients/dashboard && npx eslint src/pages/chat/chat-page.tsx`
Expected: 0 errors, 0 new warnings.

- [ ] **Step 7: Manual browser smoke**

With Aspire running, in `/chat`:
- Open a channel that has at least one message with replies (or send a reply to seed one).
- Click the reply-count chip on a parent → ThreadPanel opens on the right.
- Click "Reply in thread" from a parent's hover action rail → same.
- Send a reply via the panel composer → it appears in the panel; the parent's reply count increments.
- From a second tab as another user, send a reply → it streams into the panel via SignalR.
- Edit the parent → body updates in the panel.
- Delete the parent → panel closes.
- Press Esc → panel closes.
- Switch channels in the rail → panel closes (replyParentId resets).

- [ ] **Step 8: Commit**

```bash
git add clients/dashboard/src/pages/chat/chat-page.tsx
git commit -m "feat(chat-4): wire ThreadPanel into chat-page + auto-close on channel switch"
```

---

## Slice D — Verification + handoff

### Task D.1: Full verification sweep

- [ ] **Step 1: TypeScript build**

Run: `cd clients/dashboard && npx tsc -b`
Expected: 0 errors.

- [ ] **Step 2: Vite production build**

Run: `cd clients/dashboard && npx vite build`
Expected: 0 errors. Chat chunk size should be roughly in line with the previous build (~55 KB / ~15 KB gzip — small increase expected from ThreadPanel).

- [ ] **Step 3: Lint touched files**

Run:

```bash
cd clients/dashboard && npx eslint \
  src/pages/chat/thread-panel.tsx \
  src/pages/chat/chat-page.tsx \
  src/pages/chat/channel-rail.tsx \
  src/pages/chat/message.tsx \
  src/components/ui/avatar.tsx \
  src/lib/use-user-display.ts
```

Expected: 0 errors, 0 new warnings.

- [ ] **Step 4: Verify no regressions on existing chat flows**

In the browser:
- Send a top-level message → still works.
- Edit / delete own message → still works.
- Mark-read → unread counter clears.
- Channel switch → MessageList lands at latest.
- Composer @-mention picker → still works inside both the main composer and the thread composer.

### Task D.2: Update handoff

**Files:**
- Modify: `C:\Users\mukesh\.claude\projects\C--Users-mukesh-repos-fullstackhero-dotnet-starter-kit\memory\handoff.md`

- [ ] **Step 1: Prepend a new entry**

Add a new "## Most recent: Chat Slice 4 deferred UI" section at the top of `handoff.md` summarising the three items, commit hashes, and the verification gates that passed. Keep the existing content intact (chronological log).

- [ ] **Step 2: Commit the spec, plan, and handoff updates if not already committed**

```bash
git status
# If memory/ is checked into the repo, it's outside this repo's git tree (memory lives in ~/.claude/projects/...).
# No commit needed there.
```

The memory directory is in `~/.claude/projects/...`, not the project repo — it doesn't need a git commit. Just save the file.

---

## Self-review checklist

- [ ] **Spec coverage**
  - ThreadPanel — Task C.1 + C.2 ✓
  - Mention profile peek — Task B.1 + B.2 ✓
  - DM rail avatars — Task A.1 + A.2 ✓
  - useUserByUsername helper — Task B.1 ✓
  - Avatar `xs` size — Task A.1 ✓
  - Realtime handlers for all four hub events scoped to parent — Task C.1 ✓
  - Esc-to-close — Task C.1 ✓
  - Channel-switch reset — Task C.2 ✓
  - Parent-from-cache + tombstone fallback — Task C.1 ✓
  - Composer reuse with parentMessageId — Task C.1 ✓
  - Self-mention disables Open DM — Task B.2 ✓
  - Unresolved/loading/error states — Task B.2 ✓

- [ ] **Placeholder scan** — no TBD / TODO / "implement later" / vague "add error handling" — clean.

- [ ] **Type consistency** — `MessageDto.parentMessageId` accessed in C.1 matches the type in `api/chat.ts:64`. `UserDto.imageUrl` accessed in B.2 matches `getUserById`/`searchUsers` return shape (both pull from Identity user records). `ChannelTypeValue` import in C.1 matches the export from `api/chat.ts`.

---

## Execution notes

- All commits use the existing `feat(chat-4): …` prefix style for consistency with prior Slice 4 commits.
- No backend, migrations, or new packages.
- Estimated time: A.1 + A.2 ~15 min, B.1 + B.2 ~30–40 min, C.1 + C.2 ~45–60 min, D ~15 min.
- The thread panel is the only piece with non-trivial realtime behaviour — pay attention to subscription deps in `useRealtimeEvent` (the existing message-list.tsx pattern is the reference).
