import { apiFetch } from "@/lib/api-client";

export type RoleDto = {
  id: string;
  name: string;
  description?: string | null;
  permissions?: string[] | null;
};

export type UpsertRoleInput = {
  /** Pass empty string to create a new role; existing GUID to update. */
  id: string;
  name: string;
  description?: string | null;
};

export type UpdateRolePermissionsInput = {
  roleId: string;
  permissions: string[];
};

const ROOT = "/api/v1/identity";

export async function listRoles(): Promise<RoleDto[]> {
  // The endpoint is paged (`PagedResponse<RoleDto>` → `{ items, … }`), but every
  // caller here wants the flat list. Unwrap defensively so a bare array still works.
  const result = await apiFetch<RoleDto[] | { items?: RoleDto[] }>(`${ROOT}/roles`);
  if (Array.isArray(result)) return result;
  return result.items ?? [];
}

export function getRole(id: string): Promise<RoleDto> {
  return apiFetch<RoleDto>(`${ROOT}/roles/${encodeURIComponent(id)}`);
}

export function getRoleWithPermissions(id: string): Promise<RoleDto> {
  // Note: this endpoint is mapped at `/{id:guid}/permissions` under the
  // identity group, NOT under `/roles/`. Server-side asymmetry preserved.
  return apiFetch<RoleDto>(`${ROOT}/${encodeURIComponent(id)}/permissions`);
}

export function upsertRole(input: UpsertRoleInput): Promise<RoleDto> {
  return apiFetch<RoleDto>(`${ROOT}/roles`, {
    method: "POST",
    body: JSON.stringify({
      id: input.id,
      name: input.name,
      description: input.description ?? null,
    }),
  });
}

export function deleteRole(id: string): Promise<void> {
  return apiFetch<void>(`${ROOT}/roles/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function updateRolePermissions(input: UpdateRolePermissionsInput): Promise<string> {
  return apiFetch<string>(`${ROOT}/${encodeURIComponent(input.roleId)}/permissions`, {
    method: "PUT",
    body: JSON.stringify({ roleId: input.roleId, permissions: input.permissions }),
  });
}
