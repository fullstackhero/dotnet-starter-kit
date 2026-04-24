import { apiFetch } from "@/lib/api-client";

export type TenantDto = {
  id: string;
  name: string;
  adminEmail: string;
  isActive: boolean;
  validUpto: string;
  issuer?: string;
};

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
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
  issuer: string;
  connectionString?: string | null;
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
      issuer: input.issuer,
      connectionString: input.connectionString ?? null,
    }),
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
