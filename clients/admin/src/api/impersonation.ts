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
 *
 * ---
 * Why there is no `endImpersonation()` in this file (compare dashboard):
 *
 * The admin never holds an impersonation session — the impersonation
 * token lives in the dashboard tab the operator opened with the hash
 * handoff. So "ending impersonation" from the admin's perspective is
 * actually server-side REVOCATION of the grant, not a session swap.
 * That's covered by:
 *
 *   POST /api/v1/identity/impersonation/grants/{id}/revoke
 *
 * which is wired via `revokeImpersonationGrant` in impersonation-grants.ts
 * and rendered on the /impersonation page + the tenant-detail inline
 * active-grants card. After revoke, the JWT validation hook short-circuits
 * any further requests carrying that impersonation token via the
 * HybridCache-backed revocation lookup — the dashboard tab effectively
 * loses the session on its next API call.
 *
 * If the admin ever installs impersonation tokens locally (e.g. an
 * in-place "view as user" mode), this file should pick up the
 * `endImpersonation()` call from dashboard's identity.ts and wire it to
 * a "Stop impersonating" button in the topbar.
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
