import { ChannelType, type ChannelDto, type MessageDto } from "@/api/chat";

/** Stable display name for a channel — falls back through type-appropriate paths. */
export function channelTitle(channel: ChannelDto, selfUserId?: string): string {
  if (channel.type === ChannelType.Channel) return channel.name?.trim() || "(unnamed channel)";
  if (channel.type === ChannelType.DirectMessage) {
    // DM — show the other member's user id (richer name resolution would
    // require an Identity lookup; users can be wired in later via a
    // useUserDisplay hook).
    const other = channel.members.find((m) => m.userId !== selfUserId);
    return other ? `@${shortenUserId(other.userId)}` : "Direct message";
  }
  // Group DM — list the other members up to 3, "+N more" beyond that.
  const others = channel.members.filter((m) => m.userId !== selfUserId);
  if (others.length === 0) return "Empty group";
  const first = others.slice(0, 3).map((m) => `@${shortenUserId(m.userId)}`);
  const extra = others.length - first.length;
  return extra > 0 ? `${first.join(", ")} +${extra}` : first.join(", ");
}

/** Heuristic: short, recognisable user id token for fallback display. */
export function shortenUserId(userId: string): string {
  // GUID-shaped → first 8 chars; otherwise the original.
  if (/^[0-9a-f-]{32,}$/i.test(userId)) return userId.slice(0, 8);
  return userId;
}

/** Date key (YYYY-MM-DD) used to bucket messages into day rules. */
export function dayKey(iso: string): string {
  const d = new Date(iso);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Human-readable day rule label — "TODAY" / "YESTERDAY" / "TUESDAY · MAR 5". */
export function dayRuleLabel(iso: string): string {
  const d = new Date(iso);
  const today = new Date();
  const startOfDay = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate());
  const diffMs = startOfDay(today).getTime() - startOfDay(d).getTime();
  const days = Math.round(diffMs / (24 * 60 * 60 * 1000));
  if (days === 0) return "Today";
  if (days === 1) return "Yesterday";
  if (days < 7) {
    return d.toLocaleDateString("en-US", { weekday: "long" });
  }
  return d.toLocaleDateString("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
  });
}

/** "HH:MM" / "h:MM AM" — sender-time chip next to the author's name. */
export function shortTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit" });
}

/** "Today 10:42" / "Yesterday 4:18 PM" / "Mar 3 9:01 AM" — for search results
 *  and other contexts where the date matters alongside the time. */
export function shortDateTime(iso: string): string {
  return `${dayRuleLabel(iso)} ${shortTime(iso)}`;
}

/** Returns true when two messages can be visually merged (same author, < 5min apart, same thread). */
export function canMerge(a: MessageDto, b: MessageDto): boolean {
  if (a.authorUserId !== b.authorUserId) return false;
  if ((a.parentMessageId ?? null) !== (b.parentMessageId ?? null)) return false;
  const diffMs = new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
  return diffMs < 5 * 60 * 1000;
}

/** Group reactions by emoji and count + whether the caller has reacted. */
export function groupReactions(
  message: MessageDto,
  selfUserId: string | undefined,
): Array<{ emoji: string; count: number; mine: boolean }> {
  const buckets = new Map<string, { count: number; mine: boolean }>();
  for (const r of message.reactions) {
    const bucket = buckets.get(r.emoji) ?? { count: 0, mine: false };
    bucket.count += 1;
    if (selfUserId && r.userId === selfUserId) bucket.mine = true;
    buckets.set(r.emoji, bucket);
  }
  return Array.from(buckets.entries()).map(([emoji, v]) => ({ emoji, ...v }));
}
