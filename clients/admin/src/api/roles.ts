import { apiFetch } from "@/lib/api-client";

export type RoleDto = {
  id: string;
  name: string;
  description?: string | null;
  permissions?: string[] | null;
};

export async function listRoles(): Promise<RoleDto[]> {
  return apiFetch<RoleDto[]>(`/api/v1/identity/roles`);
}
