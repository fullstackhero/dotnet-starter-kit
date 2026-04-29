import { apiFetch } from "@/lib/api-client";

export type UserSessionDto = {
  id: string;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  ipAddress?: string | null;
  deviceType?: string | null;
  browser?: string | null;
  browserVersion?: string | null;
  operatingSystem?: string | null;
  osVersion?: string | null;
  createdAt: string;
  lastActivityAt: string;
  expiresAt: string;
  isActive: boolean;
  isCurrentSession: boolean;
};

const BASE = "/api/v1/identity/sessions";

export function getMySessions() {
  return apiFetch<UserSessionDto[]>(`${BASE}/me`);
}

export function revokeSession(sessionId: string) {
  return apiFetch<void>(`${BASE}/${sessionId}`, { method: "DELETE" });
}

export function revokeAllOtherSessions() {
  return apiFetch<{ revokedCount: number }>(`${BASE}/revoke-all`, {
    method: "POST",
    body: JSON.stringify({}),
  });
}
