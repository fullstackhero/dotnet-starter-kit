import { apiFetch } from "@/lib/api-client";

export type ImpersonationGrantStatus = "Active" | "Ended" | "Revoked" | "Expired";

export type ImpersonationGrantDto = {
  id: string;
  jti: string;
  actorUserId: string;
  actorUserName?: string | null;
  actorTenantId: string;
  impersonatedUserId: string;
  impersonatedUserName?: string | null;
  impersonatedTenantId: string;
  reason: string;
  startedAtUtc: string;
  expiresAtUtc: string;
  endedAtUtc?: string | null;
  revokedAtUtc?: string | null;
  revokedByUserId?: string | null;
  revokedByUserName?: string | null;
  revokeReason?: string | null;
  status: ImpersonationGrantStatus;
};

export type ListGrantsParams = {
  status?: ImpersonationGrantStatus;
  impersonatedTenantId?: string;
  actorUserId?: string;
  take?: number;
};

export async function listImpersonationGrants(
  params: ListGrantsParams = {},
): Promise<ImpersonationGrantDto[]> {
  const q = new URLSearchParams();
  if (params.status) q.set("Status", params.status);
  if (params.impersonatedTenantId) q.set("ImpersonatedTenantId", params.impersonatedTenantId);
  if (params.actorUserId) q.set("ActorUserId", params.actorUserId);
  q.set("Take", String(params.take ?? 100));
  return apiFetch<ImpersonationGrantDto[]>(
    `/api/v1/identity/impersonation/grants?${q.toString()}`,
  );
}

export async function revokeImpersonationGrant(
  id: string,
  reason?: string,
): Promise<ImpersonationGrantDto> {
  return apiFetch<ImpersonationGrantDto>(
    `/api/v1/identity/impersonation/grants/${encodeURIComponent(id)}/revoke`,
    {
      method: "POST",
      body: JSON.stringify({ reason: reason ?? null }),
    },
  );
}
