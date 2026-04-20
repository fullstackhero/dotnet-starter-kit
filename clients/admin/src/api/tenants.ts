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

export async function listTenants(params: ListTenantsParams = {}): Promise<PagedResponse<TenantDto>> {
  const query = new URLSearchParams();
  query.set("PageNumber", String(params.pageNumber ?? 1));
  query.set("PageSize", String(params.pageSize ?? 10));
  if (params.sort) query.set("Sort", params.sort);
  return apiFetch<PagedResponse<TenantDto>>(`/api/v1/tenants/?${query.toString()}`);
}
