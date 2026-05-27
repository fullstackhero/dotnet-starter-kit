import { apiFetch } from "@/lib/api-client";

export type NotificationDto = {
  id: string;
  type: string;
  title: string;
  body?: string | null;
  link?: string | null;
  source: string;
  metadataJson: string;
  readAtUtc?: string | null;
  createdAtUtc: string;
};

const ROOT = "/api/v1/notifications";

export function listNotifications(params: { unreadOnly?: boolean; page?: number; pageSize?: number } = {}): Promise<NotificationDto[]> {
  const qs = new URLSearchParams();
  if (params.unreadOnly) qs.set("unreadOnly", "true");
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  const q = qs.toString();
  return apiFetch<NotificationDto[]>(`${ROOT}/${q ? `?${q}` : ""}`);
}

export function getUnreadCount(): Promise<number> {
  return apiFetch<number>(`${ROOT}/unread-count`);
}

export function markNotificationRead(notificationId: string): Promise<void> {
  return apiFetch<void>(`${ROOT}/${encodeURIComponent(notificationId)}/read`, { method: "POST" });
}

export function markAllNotificationsRead(): Promise<{ updated: number }> {
  return apiFetch<{ updated: number }>(`${ROOT}/read-all`, { method: "POST" });
}
