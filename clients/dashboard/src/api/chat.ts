import { apiFetch } from "@/lib/api-client";

// Mirrors FSH.Modules.Chat.Domain.ChannelType. The API serializes enums as
// their string name (global JsonStringEnumConverter).
export const ChannelType = {
  DirectMessage: "DirectMessage",
  GroupMessage: "GroupMessage",
  Channel: "Channel",
} as const;
export type ChannelTypeValue = (typeof ChannelType)[keyof typeof ChannelType];

// Mirrors FSH.Modules.Chat.Domain.ChannelMemberRole.
export const ChannelMemberRole = {
  Member: "Member",
  Admin: "Admin",
} as const;
export type ChannelMemberRoleValue = (typeof ChannelMemberRole)[keyof typeof ChannelMemberRole];

export type ChannelMemberDto = {
  id: string;
  userId: string;
  role: ChannelMemberRoleValue;
  joinedAtUtc: string;
  lastReadMessageId?: string | null;
};

export type ChannelDto = {
  id: string;
  type: ChannelTypeValue;
  name?: string | null;
  slug?: string | null;
  description?: string | null;
  isPrivate: boolean;
  createdByUserId: string;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  lastMessageAtUtc?: string | null;
  unreadCount: number;
  members: ChannelMemberDto[];
};

export type MessageAttachmentDto = {
  id: string;
  fileAssetId?: string | null;
  url: string;
  contentType: string;
  originalFileName: string;
  sizeBytes: number;
};

export type MessageReactionDto = {
  id: string;
  messageId: string;
  userId: string;
  emoji: string;
  createdAtUtc: string;
};

export type MessageDto = {
  id: string;
  channelId: string;
  authorUserId: string;
  body?: string | null;
  parentMessageId?: string | null;
  replyCount: number;
  editedAtUtc?: string | null;
  deletedAtUtc?: string | null;
  createdAtUtc: string;
  attachments: MessageAttachmentDto[];
  reactions: MessageReactionDto[];
  isPinned?: boolean;
  pinnedByUserId?: string | null;
  pinnedAtUtc?: string | null;
};

// ── Channels ───────────────────────────────────────────────────────────

export function listMyChannels(params: { page?: number; pageSize?: number } = {}): Promise<ChannelDto[]> {
  const qs = new URLSearchParams();
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  const q = qs.toString();
  return apiFetch<ChannelDto[]>(`/api/v1/chat/channels${q ? `?${q}` : ""}`);
}

export function getChannelById(channelId: string): Promise<ChannelDto> {
  return apiFetch<ChannelDto>(`/api/v1/chat/channels/${encodeURIComponent(channelId)}`);
}

export function createChannel(input: { name: string; description?: string | null; isPrivate: boolean }): Promise<string> {
  return apiFetch<string>(`/api/v1/chat/channels`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateChannel(input: { channelId: string; name: string; description?: string | null; isPrivate: boolean }): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/channels/${encodeURIComponent(input.channelId)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function archiveChannel(channelId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/channels/${encodeURIComponent(channelId)}`, { method: "DELETE" });
}

export function addChannelMembers(channelId: string, userIds: string[]): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/channels/${encodeURIComponent(channelId)}/members`, {
    method: "POST",
    body: JSON.stringify({ channelId, userIds }),
  });
}

export function removeChannelMember(channelId: string, userId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/chat/channels/${encodeURIComponent(channelId)}/members/${encodeURIComponent(userId)}`,
    { method: "DELETE" },
  );
}

export function findOrCreateDm(userIds: string[]): Promise<string> {
  return apiFetch<string>(`/api/v1/chat/dms`, {
    method: "POST",
    body: JSON.stringify({ userIds }),
  });
}

export function markChannelRead(channelId: string, messageId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/channels/${encodeURIComponent(channelId)}/read`, {
    method: "POST",
    body: JSON.stringify({ messageId }),
  });
}

// ── Messages ───────────────────────────────────────────────────────────

export function listChannelMessages(
  channelId: string,
  params: { before?: string; pageSize?: number } = {},
): Promise<MessageDto[]> {
  const qs = new URLSearchParams();
  if (params.before) qs.set("before", params.before);
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  const q = qs.toString();
  return apiFetch<MessageDto[]>(
    `/api/v1/chat/channels/${encodeURIComponent(channelId)}/messages${q ? `?${q}` : ""}`,
  );
}

export function listMessageReplies(
  parentMessageId: string,
  params: { before?: string; pageSize?: number } = {},
): Promise<MessageDto[]> {
  const qs = new URLSearchParams();
  if (params.before) qs.set("before", params.before);
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  const q = qs.toString();
  return apiFetch<MessageDto[]>(
    `/api/v1/chat/messages/${encodeURIComponent(parentMessageId)}/replies${q ? `?${q}` : ""}`,
  );
}

export type SendMessageInput = {
  channelId: string;
  body: string;
  parentMessageId?: string | null;
  attachments?: {
    fileAssetId?: string | null;
    url: string;
    contentType: string;
    fileName: string;
    sizeBytes: number;
  }[];
  idempotencyKey?: string;
};

export function sendMessage(input: SendMessageInput): Promise<MessageDto> {
  const headers: Record<string, string> = {};
  if (input.idempotencyKey) headers["Idempotency-Key"] = input.idempotencyKey;
  return apiFetch<MessageDto>(
    `/api/v1/chat/channels/${encodeURIComponent(input.channelId)}/messages`,
    {
      method: "POST",
      body: JSON.stringify({
        body: input.body,
        parentMessageId: input.parentMessageId ?? null,
        attachments: input.attachments ?? [],
      }),
      headers,
    },
  );
}

export function editMessage(messageId: string, body: string): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/messages/${encodeURIComponent(messageId)}`, {
    method: "PUT",
    body: JSON.stringify({ body }),
  });
}

export function deleteMessage(messageId: string): Promise<void> {
  return apiFetch<void>(`/api/v1/chat/messages/${encodeURIComponent(messageId)}`, { method: "DELETE" });
}

// ── Pinning ────────────────────────────────────────────────────────────

export function pinMessage(messageId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/chat/messages/${encodeURIComponent(messageId)}/pin`,
    { method: "POST" },
  );
}

export function unpinMessage(messageId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/chat/messages/${encodeURIComponent(messageId)}/pin`,
    { method: "DELETE" },
  );
}

export function listPinnedMessages(channelId: string): Promise<MessageDto[]> {
  return apiFetch<MessageDto[]>(
    `/api/v1/chat/channels/${encodeURIComponent(channelId)}/pinned`,
  );
}

// ── Reactions ──────────────────────────────────────────────────────────

export function addReaction(messageId: string, emoji: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/chat/messages/${encodeURIComponent(messageId)}/reactions`,
    { method: "POST", body: JSON.stringify({ emoji }) },
  );
}

export function removeReaction(messageId: string, emoji: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/chat/messages/${encodeURIComponent(messageId)}/reactions/${encodeURIComponent(emoji)}`,
    { method: "DELETE" },
  );
}

// ── Presence ───────────────────────────────────────────────────────────

export type PresenceEntry = { userId: string; online: boolean };

export function getPresence(userIds: string[]): Promise<PresenceEntry[]> {
  const csv = userIds.filter((id) => !!id).join(",");
  if (csv.length === 0) return Promise.resolve([]);
  return apiFetch<PresenceEntry[]>(
    `/api/v1/realtime/presence?userIds=${encodeURIComponent(csv)}`,
  );
}

// ── Search ─────────────────────────────────────────────────────────────

export function searchMessages(params: {
  q: string;
  channelId?: string;
  page?: number;
  pageSize?: number;
}): Promise<MessageDto[]> {
  const qs = new URLSearchParams();
  qs.set("q", params.q);
  if (params.channelId) qs.set("channelId", params.channelId);
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  return apiFetch<MessageDto[]>(`/api/v1/chat/search?${qs.toString()}`);
}
