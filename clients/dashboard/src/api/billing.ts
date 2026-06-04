import { apiFetch, ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";

export type QuotaResource =
  | "ApiCalls"
  | "StorageBytes"
  | "Users"
  | "WebhookDeliveries"
  | (string & {});

export type UsageSnapshotDto = {
  id: string;
  tenantId: string;
  periodYear: number;
  periodMonth: number;
  resource: QuotaResource;
  usedUnits: number;
  limitUnits: number;
  overage: number;
  capturedAtUtc: string;
};

export type InvoiceStatus = "Draft" | "Issued" | "Paid" | "Void" | (string & {});

export type InvoicePurpose = "Subscription" | "Usage" | (string & {});

export type InvoiceLineItemKind = "BaseFee" | "Overage" | "Adjustment" | (string & {});

export type InvoiceLineItemDto = {
  id: string;
  kind: InvoiceLineItemKind;
  resource?: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
};

export type InvoiceDto = {
  id: string;
  tenantId: string;
  invoiceNumber: string;
  periodYear: number;
  periodMonth: number;
  currency: string;
  subtotalAmount: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  issuedAtUtc?: string | null;
  dueAtUtc?: string | null;
  paidAtUtc?: string | null;
  voidedAtUtc?: string | null;
  notes?: string | null;
  lineItems: InvoiceLineItemDto[];
  purpose?: InvoicePurpose;
  periodStartUtc?: string | null;
  periodEndUtc?: string | null;
};

export type SubscriptionStatus = "Active" | "Suspended" | "Cancelled" | (string & {});

export type SubscriptionDto = {
  id: string;
  tenantId: string;
  planId: string;
  planKey: string;
  startUtc: string;
  endUtc?: string | null;
  status: SubscriptionStatus;
};

export type TenantExpiryState = "Active" | "InGrace" | "Expired" | (string & {});

export type TenantStatusDto = {
  id: string;
  name: string;
  isActive: boolean;
  validUpto: string;
  hasConnectionString: boolean;
  adminEmail: string;
  issuer?: string | null;
  plan?: string | null;
  expiryState: TenantExpiryState;
  graceEndsUtc: string;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

export type InvoiceSearchParams = {
  status?: InvoiceStatus;
  periodYear?: number;
  periodMonth?: number;
  pageNumber?: number;
  pageSize?: number;
};

export function getUsageSnapshots(params?: { periodYear?: number; periodMonth?: number }) {
  const query = new URLSearchParams();
  if (params?.periodYear) query.set("periodYear", String(params.periodYear));
  if (params?.periodMonth) query.set("periodMonth", String(params.periodMonth));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<UsageSnapshotDto[]>(`/api/v1/billing/usage${suffix}`);
}

/**
 * Paged tenant-scoped invoice search. The backend uses PascalCase search
 * keys (see frontend/shared.md) and returns a paged envelope.
 */
export function getMyInvoices(params: InvoiceSearchParams = {}) {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.periodYear) query.set("periodYear", String(params.periodYear));
  if (params.periodMonth) query.set("periodMonth", String(params.periodMonth));
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<PagedResult<InvoiceDto>>(`/api/v1/billing/invoices/me${suffix}`);
}

export function getMyInvoice(id: string) {
  return apiFetch<InvoiceDto>(`/api/v1/billing/invoices/${id}`);
}

export function getMySubscription() {
  return apiFetch<SubscriptionDto | null>("/api/v1/billing/subscriptions/me");
}

export function getMyStatus() {
  return apiFetch<TenantStatusDto>("/api/v1/tenants/me/status");
}

/**
 * Stream an invoice PDF and trigger a browser download named
 * `{invoiceNumber}.pdf`. apiFetch only returns parsed JSON, so we fetch
 * the blob directly here while replicating the same auth + tenant headers
 * apiFetch sets (Authorization bearer + lowercase `tenant`).
 */
export async function downloadInvoicePdf(id: string, invoiceNumber: string): Promise<void> {
  const accessToken = tokenStore.getAccessToken();
  if (!accessToken) {
    throw new ApiRequestError(401, "Not signed in");
  }

  const headers = new Headers({ Authorization: `Bearer ${accessToken}` });
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant) headers.set("tenant", tenant);

  const response = await fetch(`${env.apiBase}/api/v1/billing/invoices/${id}/pdf`, {
    headers,
  });

  if (!response.ok) {
    throw new ApiRequestError(response.status, `Failed to download invoice (${response.status})`);
  }

  const blob = await response.blob();
  const objectUrl = window.URL.createObjectURL(blob);
  try {
    const anchor = document.createElement("a");
    anchor.href = objectUrl;
    anchor.download = `${invoiceNumber}.pdf`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    window.URL.revokeObjectURL(objectUrl);
  }
}
