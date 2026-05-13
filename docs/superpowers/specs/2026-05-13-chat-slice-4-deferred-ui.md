# Chat Slice 4 — Deferred UI Items

**Date:** 2026-05-13
**Status:** Spec
**Scope:** `clients/dashboard/src/pages/chat/` and adjacent UI primitives. No backend changes; no new packages.

## Context

Chat Slices 1–4 backend + the dashboard UI polish pass are complete. Three deferred UI items remain from the Slice 4 polish pass handoff:

1. **ThreadPanel** — backend (`GET /api/v1/chat/messages/{id}/replies`) is live since Slice 4; `onReply` plumbing is already wired through `Message` → `MessageList`, but `chat-page.tsx` does not pass `onReply` or render a thread surface. The reply-count chip on a parent message and the "Reply in thread" action-rail button both call `onReply(messageId)` into a no-op.
2. **Mention profile peek** — `MentionPill` (message.tsx:191) currently copies `@username` to the clipboard on click. There is no surface for "who is this person." Per handoff, no profile-peek surface exists anywhere in the app yet.
3. **DM rail avatars** — `ChannelRow` (channel-rail.tsx:228) renders a `Users2` Lucide icon for all DMs, even 1-on-1s where the partner's avatar is the obvious affordance.

These are independent and can ship in any order. All three are pure client-side work.

## Section 1 — ThreadPanel

### Layout

A new `ThreadPanel` component renders as an overlay over the right portion of the active channel's main column.

- Desktop (`md+`): `absolute right-0 top-0 h-full w-[380px]`. Covers the rightmost 380px of the message area; the MessageList stays full-width underneath (its virtualizer never re-measures on open/close).
- Mobile (`<md`): `absolute inset-0`. Full-bleed over the channel column. The channel rail remains accessible via the existing left-side affordances.
- Left edge: 1px `border-l-[var(--color-border)]` plus a brand hairline so it reads as a layered surface, not a column.
- Background: `bg-[var(--color-surface-1)]` (matches the channel column so contrast comes from the border + shadow, not a different surface tone).
- Box shadow: `shadow-[-12px_0_28px_-12px_oklch(0_0_0_/_0.18)]` so the overlay reads as floating over MessageList without obscuring it.
- Z-index: above the MessageList (`z-20`); below the channel rail's NewDmDialog (`z-50`).

### Structure

```
┌────────────────────────────────────┐
│ ⤴ Thread                        ✕  │  h-14 header, mirrors channel header
├────────────────────────────────────┤
│ [parent <Message>]                 │  reuse Message component, onReply=undef
│ ─── N replies ─────────────────────│  divider; "Start the thread." when N=0
│ [reply 1 <Message>]                │  reuse Message, isMerged + onReply=undef
│ [reply 2 <Message>]                │
│ [reply 3 <Message>]                │
├────────────────────────────────────┤
│ [<Composer parentMessageId={pid}>] │  reuse existing Composer; placeholder
└────────────────────────────────────┘    auto-switches to "Reply in thread…"
```

Replies render in chronological order top→bottom (matching the main pane). Merge-block logic (`canMerge`) applies inside the replies list, but the parent message never merges with the first reply (the divider always breaks the block).

### Wiring (`chat-page.tsx::ActiveChannel`)

```ts
const [replyParentId, setReplyParentId] = useState<string | null>(null);

// Reset when the user switches channels.
useEffect(() => { setReplyParentId(null); }, [channelId]);

// Pass into MessageList.
<MessageList onReply={setReplyParentId} ... />

// Render the panel when a parent is selected.
{replyParentId && (
  <ThreadPanel
    channelId={channelId}
    parentMessageId={replyParentId}
    selfUserId={selfUserId}
    onClose={() => setReplyParentId(null)}
  />
)}
```

The panel mounts as a sibling of `MessageList` inside the main column flex container, with `position: relative` on the container so the panel's absolute positioning anchors correctly. The container already has `flex h-full min-h-0 flex-col`; we add `relative` and keep the existing flow children flowing.

### Data

**Parent message:**
1. First try the MessageList cache via `queryClient.getQueryData<MessageDto[]>(["chat","messages",channelId])` and find by id. Top-level messages are almost always there because MessageList loaded 100 entries.
2. If not in cache (e.g., the parent is older than the loaded page), fall back to a single fetch with a new query key `["chat","message",parentMessageId]`. We don't have a `GET /messages/{id}` endpoint today, but `listMessageReplies` with `pageSize: 0` won't return the parent. To keep scope tight, the panel **does not add a new endpoint** — if the parent isn't in cache it renders a "Parent message unavailable" placeholder above the replies list. This is rare in practice; users open threads from messages they can see.

**Replies:**
```ts
useQuery({
  queryKey: ["chat", "replies", parentMessageId],
  queryFn: () => listMessageReplies(parentMessageId, { pageSize: 100 }),
  staleTime: 0,
});
```

`listMessageReplies` is already in `api/chat.ts:161`. Reply lists are typically <50; we don't paginate or virtualize.

### Realtime

Inside ThreadPanel, subscribe to the existing hub events scoped to the open thread:

| Event | Action |
| --- | --- |
| `ChatMessageCreated` | If `payload.parentMessageId === parentMessageId`, append into the replies cache via `setQueryData`. Skip if already present. |
| `ChatMessageEdited` | If the payload's id matches the parent or any loaded reply, replace via `setQueryData`. |
| `ChatMessageDeleted` | If id matches a reply, `invalidateQueries`. If id matches the parent, close the panel (the parent is gone). |
| `ChatReactionChanged` | If `messageId` matches the parent or a reply, `invalidateQueries`. Threads are small; full re-fetch is cheap. |

The handlers do not duplicate MessageList's broadcast handlers — MessageList only cares about top-level messages in its channel; ThreadPanel only cares about replies under a specific parent. Both can coexist.

### Reply send

The existing `Composer` accepts `parentMessageId` (composer.tsx:31) and already POSTs it through `sendMessage`. ThreadPanel renders `<Composer channelId={channelId} channelTitle={channelTitle} channelType={channel.type} parentMessageId={parentMessageId} />`. The composer's "Reply in thread…" placeholder activates automatically. ThreadPanel passes `channelTitle` so the placeholder is sensible if it ever falls back.

After a successful reply, the realtime `ChatMessageCreated` event closes the loop and the new reply appears at the bottom of the panel. The composer also invalidates `["chat","my-channels"]` (existing behavior) so `LastMessageAtUtc` re-sorts the channel list.

### Keyboard

- `Escape` while the panel is mounted: close the panel. Attached via a `useEffect` window listener; ignored if the composer textarea is focused with an unsent draft (handled inside Composer's own Esc handler — current behavior is to dismiss the mention picker on Esc; that takes precedence when the picker is open).
- Tab order: header close button → parent message → replies (no tab targets inside; mention pills are focusable) → composer textarea.

### Edge cases

- Switching channels while a thread is open: `useEffect([channelId])` clears `replyParentId`, unmounting the panel.
- Opening a different thread while one is open: `setReplyParentId(newId)` re-renders the panel with new data. The replies query key changes so TanStack Query refetches; no flicker because the parent loads from cache.
- Parent deleted after panel opens: `ChatMessageDeleted` handler closes the panel and toasts "Thread closed — parent deleted."
- User leaves the channel while panel open: the next channel mutation will fail with 403/404; the realtime hub will eventually drop them from the channel group. We close the panel reactively on `ChatChannelMemberRemoved` (only if `payload.userId === selfUserId`).

### Visual tokens

- Header: same `h-14 border-b border-[var(--color-border)] px-4` as the channel header in `chat-page.tsx:189`. Icon: `⤴` (Reply icon from lucide-react: `Reply` or `MessageSquareReply`). Title font: `text-display text-sm font-semibold tracking-tight`.
- Close button: ghost icon button, top-right, `X` from lucide-react.
- Replies divider: `chat-day-rule`-style hairline with centered caption — reuses existing CSS atomics.

### What we are NOT doing

- Virtualization in the replies list.
- Adding a `GET /messages/{id}` endpoint.
- Deep-linking threads via URL (e.g., `/chat/:channelId/thread/:messageId`). The panel state is local; closing/refreshing closes the panel.
- Slide-in animation. The panel mounts/unmounts plainly; the box-shadow + border are the only depth cues.
- A second composer "scope toggle" (some apps let you broadcast a thread reply back to the channel). We don't.

## Section 2 — Mention profile peek

### Trigger

`MentionPill` (message.tsx:191) currently wraps a `<button>` that copies on click. The new pill is a `DropdownMenu` trigger:

- `@radix-ui/react-dropdown-menu` is already installed (used by `Combobox`).
- We use a custom render via the existing `dropdown-menu` UI primitive (`@/components/ui/dropdown-menu`) where possible, falling back to a small ad-hoc DropdownMenu construction if the primitive's API doesn't accept arbitrary content rows.

### Resolution

The pill only carries `username`. To populate the peek we resolve it via `searchUsers({ search: username, pageSize: 5, isActive: true })` and pick the case-insensitive exact match on `userName`. The query is **enabled only when the popover opens** so we don't pay the network cost for every mention pill on render.

Cache key: `["identity", "by-username", username.toLowerCase()]`. `staleTime: 5 * 60_000`, `gcTime: 30 * 60_000` (matching `useUserDisplay`). A new helper `useUserByUsername(username)` lives next to `useUserDisplay` in `lib/use-user-display.ts`.

### Content

≈280px wide popover card:

```
┌──────────────────────────────────┐
│  ┌──┐  Alice Anderson            │
│  │AA│  @alice                    │
│  └──┘  alice@acme.test           │
│                                  │
│  [Open DM]      [Copy @alice]    │
└──────────────────────────────────┘
```

Rows:
1. Avatar (size="md") + name (font-semibold) + `@handle` (mono small, muted) + email (mono small, muted). Email is `truncate` if it overflows.
2. Action row: `[Open DM]` primary (`Button` size="sm"), `[Copy @username]` ghost (`Button` size="sm" variant="ghost").

States:
- **Loading** (first open, no cache): same card height as resolved, with mono "Looking up @username…" caption replacing the lower rows.
- **Unresolved** (no exact match): "User not found" + Copy button only.
- **Self-mention** (`resolved.id === selfUserId`): "(you)" badge next to name; Open DM disabled with tooltip "That's you".

### Actions

**Open DM:**
```ts
const dmMutation = useMutation({
  mutationFn: () => findOrCreateDm([resolved.id]),
  onSuccess: (channelId) => {
    void queryClient.invalidateQueries({ queryKey: ["chat","my-channels"] });
    setOpen(false);
    navigate(`/chat/${channelId}`);
  },
});
```

**Copy @username:** identical to today's pill behavior; lives on the button inside the popover.

### Edge cases

- Username with no match: pill stays clickable; popover shows "User not found." Copy still works.
- Network failure on resolve: same "Couldn't load this user" caption + Copy button.
- Popover closes on outside click, Esc, and after Open DM navigates.
- Keyboard: pill is focusable (`<button>`); Enter opens, Space toggles; arrow keys not used (no list inside).

### What we are NOT doing

- A separate hover preview (we use click only; hover-tip on the pill is just `aria-label`).
- "View profile" deep-link to a profile page (no profile route exists).
- Showing roles or last-seen presence (no API surface yet).
- Mention pill avatars (the pill stays text-only; the popover carries the avatar).

## Section 3 — DM rail avatars

### Change

`channel-rail.tsx::ChannelRow` already resolves the 1-on-1 partner via `useUserDisplay(otherDmMember?.userId)`. When `channel.type === 0 && otherDmMember`, render the partner's avatar in the row's icon slot:

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

All other row types — group DMs (type=1), public channels (type=2, `Hash`), private channels (type=2, `Lock`) — keep their existing Lucide icon.

### Avatar primitive — new `xs` size

`components/ui/avatar.tsx` currently supports `sm | md | lg`. Add `xs`:

```ts
type AvatarSize = "xs" | "sm" | "md" | "lg";

const sizeClass: Record<AvatarSize, string> = {
  xs: "h-5 w-5 text-[10px]",     // 20×20, fits inside h-8 rows with comfort
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

The Avatar component is a leaf UI primitive (`components/ui/avatar.tsx`), not a BuildingBlocks-tier shared library. Adding a size variant is local and low-risk; all existing call sites stay valid (no default change).

### What we are NOT doing

- Stacked avatars for group DMs (deferred per brainstorm).
- Presence dots on the rail avatars (no presence API).
- Avatar in the channel header (already there in `chat-page.tsx::ActiveChannel` via the existing `Icon` slot — that one is intentionally a channel-type glyph, not a person).

## Files touched (planned)

| File | Change |
| --- | --- |
| `clients/dashboard/src/pages/chat/thread-panel.tsx` | NEW |
| `clients/dashboard/src/pages/chat/chat-page.tsx` | Add `replyParentId` state + render ThreadPanel + `relative` on flex container |
| `clients/dashboard/src/pages/chat/message-list.tsx` | No changes — `onReply` already passes through |
| `clients/dashboard/src/pages/chat/message.tsx` | Refactor `MentionPill` to use DropdownMenu profile peek |
| `clients/dashboard/src/pages/chat/channel-rail.tsx` | Swap icon for Avatar in 1-on-1 DM rows |
| `clients/dashboard/src/components/ui/avatar.tsx` | Add `"xs"` size |
| `clients/dashboard/src/lib/use-user-display.ts` | Add `useUserByUsername(username)` helper |
| `clients/dashboard/src/styles/globals.css` | (Possibly) thread-panel border/shadow atomics if not inline |

No backend, no new packages, no migrations.

## Verification

- `npx tsc -b` — 0 errors.
- `npx vite build` — 0 errors.
- `npx eslint src/pages/chat src/components/ui/avatar.tsx src/lib/use-user-display.ts` — 0 new errors, 0 new warnings.
- Manual browser smoke test against a running Aspire host:
  1. Open `/chat`, pick a channel with a thread, click reply-count chip → panel opens, parent shows, replies load, composer placeholder reads "Reply in thread…".
  2. Send a reply → appears in panel and parent's replyCount increments via realtime.
  3. Click an `@mention` pill → popover opens, Open DM creates/finds a DM and navigates there.
  4. DM rail rows show partner avatars for 1-on-1s, Users2 icon for group DMs.
  5. Esc closes the thread panel.
