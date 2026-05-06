import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

// -----------------------------
// Types
// -----------------------------

export type UserDto = {
  id?: string;
  userName?: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumber?: string;
  imageUrl?: string;
};

export type UserRoleDto = {
  roleId?: string;
  roleName?: string;
  description?: string;
  enabled: boolean;
};

export type RoleDto = {
  id: string;
  name: string;
  description?: string | null;
  permissions?: string[] | null;
};

export type SearchUsersParams = {
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean | null;
  emailConfirmed?: boolean | null;
  roleId?: string | null;
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

export type UpsertRoleInput = {
  id: string;
  name: string;
  description?: string;
};

// -----------------------------
// Users
// -----------------------------

export async function searchUsers(params: SearchUsersParams = {}): Promise<PagedResponse<UserDto>> {
  const query = new URLSearchParams();
  query.set("PageNumber", String(params.pageNumber ?? 1));
  query.set("PageSize", String(params.pageSize ?? 20));
  if (params.sort) query.set("Sort", params.sort);
  if (params.search) query.set("Search", params.search);
  if (params.isActive !== null && params.isActive !== undefined) {
    query.set("IsActive", String(params.isActive));
  }
  if (params.emailConfirmed !== null && params.emailConfirmed !== undefined) {
    query.set("EmailConfirmed", String(params.emailConfirmed));
  }
  if (params.roleId) query.set("RoleId", params.roleId);
  return apiFetch<PagedResponse<UserDto>>(`/api/v1/identity/users/search?${query.toString()}`);
}

export async function getUserById(id: string): Promise<UserDto> {
  return apiFetch<UserDto>(`/api/v1/identity/users/${encodeURIComponent(id)}`);
}

export async function getUserRoles(id: string): Promise<UserRoleDto[]> {
  return apiFetch<UserRoleDto[]>(`/api/v1/identity/users/${encodeURIComponent(id)}/roles`);
}

export async function assignUserRoles(userId: string, userRoles: UserRoleDto[]): Promise<string> {
  return apiFetch<string>(`/api/v1/identity/users/${encodeURIComponent(userId)}/roles`, {
    method: "POST",
    body: JSON.stringify({ userId, userRoles }),
  });
}

export async function toggleUserStatus(userId: string, activate: boolean): Promise<void> {
  await apiFetch<void>(`/api/v1/identity/users/${encodeURIComponent(userId)}`, {
    method: "PATCH",
    body: JSON.stringify({ userId, activateUser: activate }),
  });
}

export async function deleteUser(userId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/identity/users/${encodeURIComponent(userId)}`, {
    method: "DELETE",
  });
}

export async function registerUser(input: RegisterUserInput): Promise<RegisterUserResponse> {
  return apiFetch<RegisterUserResponse>(`/api/v1/identity/register`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// -----------------------------
// Roles
// -----------------------------

export async function listRoles(): Promise<RoleDto[]> {
  const result = await apiFetch<RoleDto[] | { items?: RoleDto[] }>(`/api/v1/identity/roles`);
  if (Array.isArray(result)) return result;
  return result.items ?? [];
}

export async function getRoleById(id: string): Promise<RoleDto> {
  return apiFetch<RoleDto>(`/api/v1/identity/roles/${encodeURIComponent(id)}`);
}

export async function getRoleWithPermissions(id: string): Promise<RoleDto> {
  return apiFetch<RoleDto>(`/api/v1/identity/${encodeURIComponent(id)}/permissions`);
}

export async function upsertRole(input: UpsertRoleInput): Promise<RoleDto> {
  return apiFetch<RoleDto>(`/api/v1/identity/roles`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateRolePermissions(roleId: string, permissions: string[]): Promise<string> {
  return apiFetch<string>(`/api/v1/identity/${encodeURIComponent(roleId)}/permissions`, {
    method: "PUT",
    body: JSON.stringify({ roleId, permissions }),
  });
}

export async function deleteRole(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/identity/roles/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// -----------------------------
// User sessions (admin)
// -----------------------------

export type AdminUserSessionDto = {
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

export async function getUserSessionsAdmin(userId: string): Promise<AdminUserSessionDto[]> {
  return apiFetch<AdminUserSessionDto[]>(
    `/api/v1/identity/users/${encodeURIComponent(userId)}/sessions`,
  );
}

export async function adminRevokeUserSession(userId: string, sessionId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/identity/users/${encodeURIComponent(userId)}/sessions/${encodeURIComponent(sessionId)}`,
    { method: "DELETE" },
  );
}

export async function adminRevokeAllUserSessions(
  userId: string,
): Promise<{ revokedCount: number }> {
  return apiFetch<{ revokedCount: number }>(
    `/api/v1/identity/users/${encodeURIComponent(userId)}/sessions/revoke-all`,
    { method: "POST", body: JSON.stringify({}) },
  );
}

// -----------------------------
// Groups
// -----------------------------

export type GroupDto = {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  isSystemGroup: boolean;
  memberCount: number;
  roleIds?: string[] | null;
  roleNames?: string[] | null;
  createdAt: string;
};

export type GroupMemberDto = {
  userId: string;
  userName?: string | null;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  addedAt: string;
  addedBy?: string | null;
};

export type CreateGroupInput = {
  name: string;
  description?: string;
  isDefault: boolean;
  roleIds?: string[];
};

export type UpdateGroupInput = {
  name: string;
  description?: string;
  isDefault: boolean;
  roleIds?: string[];
};

export async function listGroups(search?: string): Promise<GroupDto[]> {
  const q = search ? `?search=${encodeURIComponent(search)}` : "";
  const result = await apiFetch<GroupDto[] | { items?: GroupDto[] }>(
    `/api/v1/identity/groups${q}`,
  );
  if (Array.isArray(result)) return result;
  return result.items ?? [];
}

export async function getGroupById(id: string): Promise<GroupDto> {
  return apiFetch<GroupDto>(`/api/v1/identity/groups/${encodeURIComponent(id)}`);
}

export async function createGroup(input: CreateGroupInput): Promise<GroupDto> {
  return apiFetch<GroupDto>(`/api/v1/identity/groups`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function updateGroup(id: string, input: UpdateGroupInput): Promise<GroupDto> {
  return apiFetch<GroupDto>(`/api/v1/identity/groups/${encodeURIComponent(id)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteGroup(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/identity/groups/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export async function getGroupMembers(groupId: string): Promise<GroupMemberDto[]> {
  return apiFetch<GroupMemberDto[]>(
    `/api/v1/identity/groups/${encodeURIComponent(groupId)}/members`,
  );
}

export async function addUsersToGroup(
  groupId: string,
  userIds: string[],
): Promise<{ addedCount: number; alreadyMemberUserIds: string[] }> {
  return apiFetch<{ addedCount: number; alreadyMemberUserIds: string[] }>(
    `/api/v1/identity/groups/${encodeURIComponent(groupId)}/members`,
    { method: "POST", body: JSON.stringify({ userIds }) },
  );
}

export async function removeUserFromGroup(groupId: string, userId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/identity/groups/${encodeURIComponent(groupId)}/members/${encodeURIComponent(userId)}`,
    { method: "DELETE" },
  );
}

// -----------------------------
// Impersonation
// -----------------------------

export type ImpersonationResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  actorUserId: string;
  actorTenantId: string;
  impersonatedUserId: string;
  impersonatedTenantId: string;
};

export type EndImpersonationResponse = {
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  accessTokenExpiresAt: string;
};

export async function startImpersonation(input: {
  targetUserId: string;
  targetTenantId: string;
  reason?: string;
}): Promise<ImpersonationResponse> {
  return apiFetch<ImpersonationResponse>(`/api/v1/identity/impersonation/start`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function endImpersonation(): Promise<EndImpersonationResponse> {
  return apiFetch<EndImpersonationResponse>(`/api/v1/identity/impersonation/end`, {
    method: "POST",
  });
}
