import { apiFetch } from "@/lib/api-client";

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
  lineItems: unknown[];
};

export type SubscriptionStatus = "Active" | "Canceled" | "Expired" | (string & {});

export type SubscriptionDto = {
  id: string;
  tenantId: string;
  planId: string;
  planKey: string;
  startUtc: string;
  endUtc?: string | null;
  status: SubscriptionStatus;
};

export function getUsageSnapshots(params?: { periodYear?: number; periodMonth?: number }) {
  const query = new URLSearchParams();
  if (params?.periodYear) query.set("periodYear", String(params.periodYear));
  if (params?.periodMonth) query.set("periodMonth", String(params.periodMonth));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<UsageSnapshotDto[]>(`/api/v1/billing/usage${suffix}`);
}

export function getMyInvoices() {
  return apiFetch<InvoiceDto[]>("/api/v1/billing/invoices/me");
}

export function getMySubscription() {
  return apiFetch<SubscriptionDto | null>("/api/v1/billing/subscriptions/me");
}
