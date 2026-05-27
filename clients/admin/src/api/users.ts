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

// -----------------------------
// Anonymous password-reset trio
//   forgot-password → reset-password → confirm-email
// -----------------------------

/**
 * Step 1 of the forgot-password flow. Server resolves the user by
 * (email, tenant), generates a reset token, and emails them the link.
 * Server always returns 200 regardless of whether the email exists —
 * never leak account presence to the UI.
 */
export async function requestPasswordReset(input: {
  email: string;
  tenant: string;
}): Promise<void> {
  await apiFetch<string>(`${IDENTITY}/forgot-password`, {
    method: "POST",
    skipAuth: true,
    headers: { tenant: input.tenant },
    body: JSON.stringify({ email: input.email }),
  });
}

/**
 * Step 2 — caller carries (token, email, tenant) from the emailed link
 * plus a new password from the form. Existing JWTs stay valid until
 * natural expiry; the UI should bounce to /login after success.
 */
export async function resetPassword(input: {
  email: string;
  password: string;
  token: string;
  tenant: string;
}): Promise<void> {
  await apiFetch<string>(`${IDENTITY}/reset-password`, {
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
 * Confirm-email link landing. Server expects (userId, code, tenant) as
 * query parameters from the registration email.
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
  return apiFetch<string>(`${IDENTITY}/confirm-email?${qs}`, {
    method: "GET",
    skipAuth: true,
    headers: { tenant: input.tenant },
  });
}
