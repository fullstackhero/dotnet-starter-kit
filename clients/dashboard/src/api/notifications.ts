import { apiFetch } from "@/lib/api-client";

// Mirrors FSH.Modules.Notifications.Contracts.v1.DTOs.NotificationDto.
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

export function listNotifications(params: { unreadOnly?: boolean; page?: number; pageSize?: number } = {}): Promise<NotificationDto[]> {
  const qs = new URLSearchParams();
  if (params.unreadOnly) qs.set("unreadOnly", "true");
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  const q = qs.toString();
  return apiFetch<NotificationDto[]>(`/api/v1/notifications/${q ? `?${q}` : ""}`);
}

export function getUnreadCount(): Promise<number> {
  return apiFetch<number>(`/api/v1/notifications/unread-count`);
}

export function markNotificationRead(notificationId: string): Promise<void> {
  return apiFetch<void>(
    `/api/v1/notifications/${encodeURIComponent(notificationId)}/read`,
    { method: "POST" },
  );
}

export function markAllNotificationsRead(): Promise<{ updated: number }> {
  return apiFetch<{ updated: number }>(`/api/v1/notifications/read-all`, { method: "POST" });
}
