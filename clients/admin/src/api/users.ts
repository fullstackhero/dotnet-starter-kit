import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

export type UserDto = {
  id: string;
  userName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumber?: string | null;
  imageUrl?: string | null;
  twoFactorEnabled?: boolean;
};

export type UserRoleDto = {
  roleId: string;
  roleName: string;
  description?: string | null;
  enabled: boolean;
};

export type SearchUsersParams = {
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
  roleId?: string;
  /**
   * When set, sends a `tenant` header overriding the operator's active tenant
   * for this request only. Used by impersonation flows so a root operator can
   * browse another tenant's users without flipping their global session.
   */
  tenantId?: string;
};

export type RegisterUserInput = {
  firstName: string;
  lastName: string;
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
};

export type RegisterUserResponse = {
  userId: string;
  message?: string;
};

const BASE = "/api/v1/identity/users";
const IDENTITY = "/api/v1/identity";

/**
 * Returns the permission strings the current user holds. The JWT only carries
 * role names — permissions are resolved server-side per role on this endpoint,
 * so client-side route guards must call it after login (and after a refresh
 * if grants may have changed).
 */
export async function getMyPermissions(): Promise<string[]> {
  return (await apiFetch<string[] | null>(`${IDENTITY}/permissions`)) ?? [];
}

export async function getMyProfile(): Promise<UserDto> {
  return apiFetch<UserDto>(`${IDENTITY}/profile`);
}

export async function setProfileImage(imageUrl: string | null): Promise<void> {
  await apiFetch<void>(`${IDENTITY}/profile/image`, {
    method: "PUT",
    body: JSON.stringify({ imageUrl }),
  });
}

export async function changePassword(input: {
  password: string;
  newPassword: string;
  confirmNewPassword: string;
}): Promise<string> {
  return apiFetch<string>(`${BASE}/change-password`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function searchUsers(params: SearchUsersParams = {}): Promise<PagedResponse<UserDto>> {
  const q = new URLSearchParams();
  q.set("PageNumber", String(params.pageNumber ?? 1));
  q.set("PageSize", String(params.pageSize ?? 10));
  if (params.sort) q.set("Sort", params.sort);
  if (params.search?.trim()) q.set("Search", params.search.trim());
  if (params.isActive !== undefined) q.set("IsActive", String(params.isActive));
  if (params.emailConfirmed !== undefined) q.set("EmailConfirmed", String(params.emailConfirmed));
  if (params.roleId) q.set("RoleId", params.roleId);
  return apiFetch<PagedResponse<UserDto>>(`${BASE}/search?${q.toString()}`, {
    headers: params.tenantId ? { tenant: params.tenantId } : undefined,
  });
}

export async function getUser(id: string): Promise<UserDto> {
  return apiFetch<UserDto>(`${BASE}/${encodeURIComponent(id)}`);
}

export async function getUserRoles(id: string): Promise<UserRoleDto[]> {
  return apiFetch<UserRoleDto[]>(`${BASE}/${encodeURIComponent(id)}/roles`);
}

export async function registerUser(input: RegisterUserInput): Promise<RegisterUserResponse> {
  return apiFetch<RegisterUserResponse>(`${BASE}/register`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function toggleUserStatus(id: string, activateUser: boolean): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(id)}`, {
    method: "PATCH",
    body: JSON.stringify({ userId: id, activateUser }),
  });
}

export async function assignUserRoles(id: string, roles: UserRoleDto[]): Promise<string> {
  return apiFetch<string>(`${BASE}/${encodeURIComponent(id)}/roles`, {
    method: "POST",
    body: JSON.stringify({ userId: id, userRoles: roles }),
  });
}
