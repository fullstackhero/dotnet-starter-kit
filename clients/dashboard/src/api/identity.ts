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
  twoFactorEnabled?: boolean;
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

export async function confirmUserEmail(userId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/identity/users/${encodeURIComponent(userId)}/confirm-email`,
    { method: "POST" },
  );
}

export async function resendUserConfirmationEmail(userId: string): Promise<void> {
  await apiFetch<void>(
    `/api/v1/identity/users/${encodeURIComponent(userId)}/resend-confirmation-email`,
    { method: "POST" },
  );
}

/**
 * Persist a durable avatar URL on the authenticated user. Typically the publicUrl returned
 * by the Files module after a presigned upload; pass null/empty to clear.
 */
export async function setProfileImage(imageUrl: string | null): Promise<void> {
  await apiFetch<void>(`/api/v1/identity/profile/image`, {
    method: "PUT",
    body: JSON.stringify({ imageUrl }),
  });
}

/** Fetch the authenticated user's full profile (name, email, phone, imageUrl, etc.). */
/**
 * The signed-in user's effective permissions. The JWT carries only role names;
 * permissions are resolved server-side per role here. The auth context calls
 * this after login and on subject changes so gated UI reflects the live grants.
 */
export async function getMyPermissions(): Promise<string[]> {
  return (await apiFetch<string[] | null>(`/api/v1/identity/permissions`)) ?? [];
}

export async function getMyProfile(): Promise<UserDto> {
  return apiFetch<UserDto>("/api/v1/identity/profile");
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

// One row in the host's permission catalog. Mirrors
// FSH.Modules.Identity.Contracts.DTOs.PermissionCatalogEntryDto. The catalog
// endpoint is the single source of truth — every module's permissions land
// here at startup, filtered to the caller's tenant context (Admin set for
// regular tenants; Admin + Root set for the root tenant).
export type PermissionCatalogEntryDto = {
  name: string;
  description: string;
  resource: string;
  action: string;
  isBasic: boolean;
  isRoot: boolean;
};

export async function getPermissionsCatalog(): Promise<PermissionCatalogEntryDto[]> {
  return apiFetch<PermissionCatalogEntryDto[]>(`/api/v1/identity/permissions/catalog`);
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

// -----------------------------
// Profile update (PUT /identity/profile)
// -----------------------------

export type UpdateProfileInput = {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
};

/**
 * Updates the authenticated user's profile. Maps to UpdateUserCommand
 * server-side. Image and email changes go through their own dedicated
 * endpoints — this is for the editable profile fields surfaced in
 * settings/profile. Reads the current profile first so unset optional
 * fields keep their existing values instead of being nulled.
 */
export async function updateMyProfile(input: UpdateProfileInput): Promise<void> {
  const profile = await getMyProfile();
  await apiFetch<unknown>(`/api/v1/identity/profile`, {
    method: "PUT",
    body: JSON.stringify({
      id: profile.id,
      firstName: input.firstName ?? profile.firstName ?? null,
      lastName: input.lastName ?? profile.lastName ?? null,
      phoneNumber: input.phoneNumber ?? profile.phoneNumber ?? null,
      email: profile.email,
      deleteCurrentImage: false,
    }),
  });
}

// -----------------------------
// Password reset trio (anonymous; require explicit tenant header)
// -----------------------------

/**
 * Step 1 of the forgot-password flow. The server resolves the user by
 * (email, tenant), generates a reset token, and emails a link of the form
 * `<host>/reset-password?token=...&email=...&tenant=...`. Server always
 * returns 200 regardless of whether the email exists — never leak account
 * presence to the caller.
 */
export async function requestPasswordReset(input: {
  email: string;
  tenant: string;
}): Promise<void> {
  await apiFetch<string>(`/api/v1/identity/forgot-password`, {
    method: "POST",
    skipAuth: true,
    headers: { tenant: input.tenant },
    body: JSON.stringify({ email: input.email }),
  });
}

/**
 * Step 2 of the forgot-password flow. The caller carries (token, email,
 * tenant) from the emailed link plus a new password from the form. The
 * server validates the token via UserManager.ResetPasswordAsync and
 * persists the new hash. Existing JWTs remain valid until natural expiry —
 * the UI should bounce the user to /login to acquire a fresh session.
 */
export async function resetPassword(input: {
  email: string;
  password: string;
  token: string;
  tenant: string;
}): Promise<void> {
  await apiFetch<string>(`/api/v1/identity/reset-password`, {
    method: "POST",
    skipAuth: true,
    headers: { tenant: input.tenant },
    body: JSON.stringify({
      email: input.email,
      password: input.password,
      token: input.token,
    }),
  });
}

/**
 * Confirm-email link landing. The server expects (userId, code, tenant)
 * as query parameters — these come from the email-confirmation link
 * produced by the registration flow. Returns the server's confirmation
 * message on 2xx.
 */
export async function confirmEmail(input: {
  userId: string;
  code: string;
  tenant: string;
}): Promise<string> {
  const qs = new URLSearchParams({
    userId: input.userId,
    code: input.code,
    tenant: input.tenant,
  }).toString();
  return apiFetch<string>(`/api/v1/identity/confirm-email?${qs}`, {
    method: "GET",
    skipAuth: true,
    headers: { tenant: input.tenant },
  });
}

// -----------------------------
// Password change (authenticated)
// -----------------------------

export async function changePassword(input: {
  password: string;
  newPassword: string;
  confirmNewPassword: string;
}): Promise<void> {
  await apiFetch<string>(`/api/v1/identity/change-password`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// -----------------------------
// Two-factor enrollment (TOTP)
// -----------------------------

export type TwoFactorEnrollmentResponse = {
  sharedKey: string;
  authenticatorUri: string;
};

/**
 * Begin (or rotate) TOTP enrollment. 2FA is NOT enabled until the user
 * confirms with a code via verifyEnrollTwoFactor — this just hands back
 * the secret + otpauth:// URI so the QR can render.
 */
export async function enrollTwoFactor(): Promise<TwoFactorEnrollmentResponse> {
  return apiFetch<TwoFactorEnrollmentResponse>(`/api/v1/identity/2fa/enroll`, {
    method: "POST",
  });
}

export async function verifyEnrollTwoFactor(code: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(`/api/v1/identity/2fa/verify`, {
    method: "POST",
    body: JSON.stringify({ code }),
  });
}

export async function disableTwoFactor(currentPassword: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(`/api/v1/identity/2fa/disable`, {
    method: "POST",
    body: JSON.stringify({ currentPassword }),
  });
}
