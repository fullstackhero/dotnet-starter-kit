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

const ROOT = "/api/v1/identity";

export async function getMySessions(): Promise<UserSessionDto[]> {
  return apiFetch<UserSessionDto[]>(`${ROOT}/sessions/me`);
}

export async function revokeMySession(sessionId: string): Promise<void> {
  await apiFetch<void>(`${ROOT}/sessions/${encodeURIComponent(sessionId)}`, {
    method: "DELETE",
  });
}

export async function revokeAllMySessions(): Promise<{ revokedCount: number }> {
  return apiFetch<{ revokedCount: number }>(`${ROOT}/sessions/revoke-all`, {
    method: "POST",
    body: JSON.stringify({}),
  });
}

export async function getUserSessions(userId: string): Promise<UserSessionDto[]> {
  return apiFetch<UserSessionDto[]>(`${ROOT}/users/${encodeURIComponent(userId)}/sessions`);
}

export async function adminRevokeUserSession(userId: string, sessionId: string): Promise<void> {
  await apiFetch<void>(
    `${ROOT}/users/${encodeURIComponent(userId)}/sessions/${encodeURIComponent(sessionId)}`,
    { method: "DELETE" },
  );
}

export async function adminRevokeAllUserSessions(userId: string): Promise<{ revokedCount: number }> {
  return apiFetch<{ revokedCount: number }>(
    `${ROOT}/users/${encodeURIComponent(userId)}/sessions/revoke-all`,
    { method: "POST", body: JSON.stringify({}) },
  );
}
