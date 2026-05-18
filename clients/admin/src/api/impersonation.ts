import { apiFetch } from "@/lib/api-client";

export type StartImpersonationInput = {
  targetUserId: string;
  targetTenantId: string;
  reason: string;
  /** 1..60 inclusive; null lets the server use its configured default. */
  durationMinutes?: number;
};

export type ImpersonationResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  actorUserId: string;
  actorTenantId: string;
  impersonatedUserId: string;
  impersonatedTenantId: string;
};

/**
 * Issues a short-lived impersonation access token representing the target
 * user. The admin app never installs this token locally — it hands it off
 * to the dashboard via a URL hash so the dashboard can swap into the
 * impersonated session in a fresh tab.
 *
 * Note: the admin's apiFetch attaches the operator's current tenant header
 * by default, which the server uses for the cross-tenant authorization
 * check (root operators may impersonate any tenant; tenant admins only
 * their own). We do NOT override the tenant header here for that reason.
 */
export function startImpersonation(input: StartImpersonationInput): Promise<ImpersonationResponse> {
  return apiFetch<ImpersonationResponse>(`/api/v1/identity/impersonation/start`, {
    method: "POST",
    body: JSON.stringify({
      targetUserId: input.targetUserId,
      targetTenantId: input.targetTenantId,
      reason: input.reason,
      durationMinutes: input.durationMinutes ?? null,
    }),
  });
}
