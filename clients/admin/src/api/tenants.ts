import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

export type { PagedResponse } from "@/lib/api-types";

export type TenantExpiryState = "Active" | "InGrace" | "Expired" | (string & {});

export type TenantDto = {
  id: string;
  name: string;
  adminEmail: string;
  isActive: boolean;
  validUpto: string;
  issuer?: string;
  /** Present on the status endpoint (TenantStatusDto); absent on the list projection. */
  plan?: string | null;
  expiryState?: TenantExpiryState;
  graceEndsUtc?: string;
};

export type ListTenantsParams = {
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
};

export type CreateTenantInput = {
  id: string;
  name: string;
  adminEmail: string;
  adminPassword: string;
  issuer: string;
  connectionString?: string | null;
  /** Plan key to subscribe the tenant to. Omitted → server falls back to the default/trial plan. */
  planKey?: string | null;
};

export type RenewTenantResponse = {
  tenantId: string;
  validUpto: string;
  planKey: string;
  planChanged: boolean;
};

export type AdjustTenantValidityResponse = {
  tenantId: string;
  validUpto: string;
};

export type CreateTenantResponse = {
  id: string;
  provisioningCorrelationId?: string;
  status?: string;
};

export type TenantLifecycleResult = {
  tenantId: string;
  isActive: boolean;
};

export type TenantProvisioningStep = {
  step: string;
  status: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  error?: string | null;
};

export type TenantProvisioningStatus = {
  tenantId: string;
  status: string;
  correlationId: string;
  currentStep?: string | null;
  error?: string | null;
  createdUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  steps: TenantProvisioningStep[];
};

export async function listTenants(params: ListTenantsParams = {}): Promise<PagedResponse<TenantDto>> {
  const query = new URLSearchParams();
  query.set("PageNumber", String(params.pageNumber ?? 1));
  query.set("PageSize", String(params.pageSize ?? 10));
  if (params.sort) query.set("Sort", params.sort);
  return apiFetch<PagedResponse<TenantDto>>(`/api/v1/tenants/?${query.toString()}`);
}

export async function getTenantStatus(id: string): Promise<TenantDto> {
  return apiFetch<TenantDto>(`/api/v1/tenants/${encodeURIComponent(id)}/status`);
}

export async function getTenantProvisioningStatus(id: string): Promise<TenantProvisioningStatus> {
  return apiFetch<TenantProvisioningStatus>(`/api/v1/tenants/${encodeURIComponent(id)}/provisioning`);
}

export async function createTenant(input: CreateTenantInput): Promise<CreateTenantResponse> {
  return apiFetch<CreateTenantResponse>(`/api/v1/tenants/`, {
    method: "POST",
    body: JSON.stringify({
      id: input.id,
      name: input.name,
      adminEmail: input.adminEmail,
      adminPassword: input.adminPassword,
      issuer: input.issuer,
      connectionString: input.connectionString ?? null,
      planKey: input.planKey ?? null,
    }),
  });
}

/** Renew a tenant for one more plan term, optionally switching plans. */
export async function renewTenant(id: string, planKey?: string | null): Promise<RenewTenantResponse> {
  return apiFetch<RenewTenantResponse>(`/api/v1/tenants/${encodeURIComponent(id)}/renew`, {
    method: "POST",
    body: JSON.stringify({ tenantId: id, planKey: planKey ?? null }),
  });
}

/**
 * Operator override: set a tenant's ValidUpto directly with NO invoice
 * (comp/correction). Backdating is allowed server-side. Root-operator only —
 * gated by MultitenancyPermissions.Tenants.UpgradeSubscription, same as renew.
 */
export async function adjustTenantValidity(id: string, validUpto: string): Promise<AdjustTenantValidityResponse> {
  return apiFetch<AdjustTenantValidityResponse>(`/api/v1/tenants/${encodeURIComponent(id)}/adjust-validity`, {
    method: "POST",
    body: JSON.stringify({ tenantId: id, validUpto }),
  });
}

export async function changeTenantActivation(id: string, isActive: boolean): Promise<TenantLifecycleResult> {
  return apiFetch<TenantLifecycleResult>(`/api/v1/tenants/${encodeURIComponent(id)}/activation`, {
    method: "POST",
    body: JSON.stringify({ tenantId: id, isActive }),
  });
}

export async function retryTenantProvisioning(id: string): Promise<TenantProvisioningStatus> {
  return apiFetch<TenantProvisioningStatus>(`/api/v1/tenants/${encodeURIComponent(id)}/provisioning/retry`, {
    method: "POST",
  });
}

// ─────────────────────────────────────────────────────────────────────────
// Tenant theme / branding
//
// The theme endpoints are CURRENT-TENANT scoped server-side — they read
// the request's tenant header and act on that tenant's row. The admin
// operator is in the root tenant by default, so we explicitly send
// `tenant: <targetId>` to operate on a different tenant. The server's
// root-operator override middleware permits this for root callers.
// ─────────────────────────────────────────────────────────────────────────

export type PaletteDto = {
  primary: string;
  secondary: string;
  tertiary: string;
  background: string;
  surface: string;
  error: string;
  warning: string;
  success: string;
  info: string;
};

export type BrandAssetsDto = {
  logoUrl?: string | null;
  logoDarkUrl?: string | null;
  faviconUrl?: string | null;
  deleteLogo?: boolean;
  deleteLogoDark?: boolean;
  deleteFavicon?: boolean;
};

export type TypographyDto = {
  fontFamily: string;
  headingFontFamily: string;
  fontSizeBase: number;
  lineHeightBase: number;
};

export type LayoutDto = {
  borderRadius: string;
  defaultElevation: number;
};

export type TenantThemeDto = {
  lightPalette: PaletteDto;
  darkPalette: PaletteDto;
  brandAssets: BrandAssetsDto;
  typography: TypographyDto;
  layout: LayoutDto;
  isDefault: boolean;
};

export const DEFAULT_LIGHT_PALETTE: PaletteDto = {
  primary: "#2563EB",
  secondary: "#0F172A",
  tertiary: "#6366F1",
  background: "#F8FAFC",
  surface: "#FFFFFF",
  error: "#DC2626",
  warning: "#F59E0B",
  success: "#16A34A",
  info: "#0284C7",
};

export const DEFAULT_DARK_PALETTE: PaletteDto = {
  primary: "#38BDF8",
  secondary: "#94A3B8",
  tertiary: "#818CF8",
  background: "#0B1220",
  surface: "#111827",
  error: "#F87171",
  warning: "#FBBF24",
  success: "#22C55E",
  info: "#38BDF8",
};

/** Fetch a tenant's theme. Caller needs MultitenancyPermissions.Tenants.ViewTheme. */
export async function getTenantTheme(tenantId: string): Promise<TenantThemeDto> {
  return apiFetch<TenantThemeDto>(`/api/v1/tenants/theme`, {
    headers: { tenant: tenantId },
  });
}

/** Save a tenant's theme. Caller needs MultitenancyPermissions.Tenants.UpdateTheme. */
export async function updateTenantTheme(
  tenantId: string,
  theme: TenantThemeDto,
): Promise<void> {
  await apiFetch<void>(`/api/v1/tenants/theme`, {
    method: "PUT",
    headers: { tenant: tenantId },
    body: JSON.stringify(theme),
  });
}

/** Reset a tenant's theme to framework defaults. */
export async function resetTenantTheme(tenantId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/tenants/theme/reset`, {
    method: "POST",
    headers: { tenant: tenantId },
  });
}
