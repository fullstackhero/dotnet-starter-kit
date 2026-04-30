import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

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

const IDENTITY = "/api/v1/identity";
const BASE = `${IDENTITY}/sessions`;

// ─── Self-service ────────────────────────────────────────────────────

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

// ─── Admin / tenant-wide ─────────────────────────────────────────────

export type TenantSessionsParams = {
  includeInactive?: boolean;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
};

export function getTenantSessions(
  params: TenantSessionsParams = {},
): Promise<PagedResponse<UserSessionDto>> {
  const q = new URLSearchParams();
  if (params.includeInactive) q.set("includeInactive", "true");
  if (params.search) q.set("search", params.search);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 50));
  return apiFetch<PagedResponse<UserSessionDto>>(`${BASE}?${q.toString()}`);
}

/**
 * Admin: revoke a session belonging to a specific user. Maps to
 * DELETE /users/{userId}/sessions/{sessionId}.
 */
export function adminRevokeUserSessionById(userId: string, sessionId: string) {
  return apiFetch<void>(
    `${IDENTITY}/users/${encodeURIComponent(userId)}/sessions/${encodeURIComponent(sessionId)}`,
    { method: "DELETE" },
  );
}

export function adminRevokeAllUserSessions(userId: string) {
  return apiFetch<{ revokedCount: number }>(
    `${IDENTITY}/users/${encodeURIComponent(userId)}/sessions/revoke-all`,
    { method: "POST", body: JSON.stringify({}) },
  );
}
