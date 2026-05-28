import { apiFetch, ApiRequestError } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";

// ─── shared enums ────────────────────────────────────────────────────

export type InvoiceStatus = "Draft" | "Issued" | "Paid" | "Void" | (string & {});

export type SubscriptionStatus = "Active" | "Suspended" | "Cancelled" | (string & {});

export type InvoiceLineItemKind = "BaseFee" | "Overage" | "Adjustment" | (string & {});

export type PlanInterval = "Monthly" | "Yearly" | (string & {});

export type InvoicePurpose = "Usage" | "Subscription" | (string & {});

export type QuotaResource =
  | "ApiCalls"
  | "StorageBytes"
  | "Users"
  | "ActiveFeatureFlags"
  | (string & {});

// ─── plans ───────────────────────────────────────────────────────────

export type BillingPlanDto = {
  id: string;
  key: string;
  name: string;
  currency: string;
  monthlyBasePrice: number;
  overageRates: Partial<Record<QuotaResource, number>>;
  isActive: boolean;
  interval: PlanInterval;
  annualPrice?: number | null;
};

export type CreatePlanInput = {
  key: string;
  name: string;
  currency: string;
  monthlyBasePrice: number;
  overageRates?: Partial<Record<QuotaResource, number>> | null;
  interval?: PlanInterval;
  annualPrice?: number | null;
};

export type UpdatePlanInput = {
  planId: string;
  name: string;
  monthlyBasePrice: number;
  overageRates?: Partial<Record<QuotaResource, number>> | null;
  interval?: PlanInterval;
  annualPrice?: number | null;
};

/** Price charged for one billing term: annual price (or 12x monthly) for yearly plans, else monthly. */
export function planTermPrice(plan: Pick<BillingPlanDto, "interval" | "monthlyBasePrice" | "annualPrice">): number {
  return plan.interval === "Yearly"
    ? plan.annualPrice ?? plan.monthlyBasePrice * 12
    : plan.monthlyBasePrice;
}

export function getPlans(includeInactive = false): Promise<BillingPlanDto[]> {
  const query = new URLSearchParams({ includeInactive: includeInactive ? "true" : "false" });
  return apiFetch<BillingPlanDto[]>(`/api/v1/billing/plans?${query.toString()}`);
}

export function createPlan(input: CreatePlanInput): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/plans`, {
    method: "POST",
    body: JSON.stringify({
      key: input.key,
      name: input.name,
      currency: input.currency,
      monthlyBasePrice: input.monthlyBasePrice,
      overageRates: input.overageRates ?? null,
      interval: input.interval ?? "Monthly",
      annualPrice: input.annualPrice ?? null,
    }),
  });
}

export function updatePlan(input: UpdatePlanInput): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/plans/${encodeURIComponent(input.planId)}`, {
    method: "PUT",
    body: JSON.stringify({
      planId: input.planId,
      name: input.name,
      monthlyBasePrice: input.monthlyBasePrice,
      overageRates: input.overageRates ?? null,
      interval: input.interval ?? "Monthly",
      annualPrice: input.annualPrice ?? null,
    }),
  });
}

// ─── subscriptions ───────────────────────────────────────────────────

export type SubscriptionDto = {
  id: string;
  tenantId: string;
  planId: string;
  planKey: string;
  startUtc: string;
  endUtc?: string | null;
  status: SubscriptionStatus;
};

export type AssignSubscriptionInput = {
  tenantId: string;
  planKey: string;
};

export function getSubscription(tenantId?: string): Promise<SubscriptionDto | null> {
  const query = new URLSearchParams();
  if (tenantId) query.set("tenantId", tenantId);
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<SubscriptionDto | null>(`/api/v1/billing/subscriptions${suffix}`);
}

export function assignSubscription(input: AssignSubscriptionInput): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/subscriptions`, {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// ─── invoices ────────────────────────────────────────────────────────

export type InvoiceLineItemDto = {
  id: string;
  kind: InvoiceLineItemKind;
  resource?: QuotaResource | null;
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
  purpose: InvoicePurpose;
  periodStartUtc?: string | null;
  periodEndUtc?: string | null;
};

export type ListInvoicesParams = {
  tenantId?: string;
  status?: InvoiceStatus;
  periodYear?: number;
  periodMonth?: number;
  pageNumber?: number;
  pageSize?: number;
};

export function listInvoices(params: ListInvoicesParams = {}): Promise<PagedResponse<InvoiceDto>> {
  const query = new URLSearchParams();
  if (params.tenantId) query.set("tenantId", params.tenantId);
  if (params.status) query.set("status", params.status);
  if (params.periodYear) query.set("periodYear", String(params.periodYear));
  if (params.periodMonth) query.set("periodMonth", String(params.periodMonth));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  return apiFetch<PagedResponse<InvoiceDto>>(`/api/v1/billing/invoices?${query.toString()}`);
}

export function getInvoice(invoiceId: string): Promise<InvoiceDto> {
  return apiFetch<InvoiceDto>(`/api/v1/billing/invoices/${encodeURIComponent(invoiceId)}`);
}

/**
 * Fetch the invoice PDF as a blob and trigger a browser download named
 * `{invoiceNumber}.pdf`. The endpoint streams `application/pdf`, so it can't
 * go through `apiFetch` (which only parses JSON). We replicate apiFetch's
 * auth + tenant headers by hand so cross-tenant viewing works identically to
 * how the detail page loads the invoice via `getInvoice`.
 */
export async function downloadInvoicePdf(invoiceId: string, invoiceNumber: string): Promise<void> {
  const headers = new Headers({ Accept: "application/pdf" });

  const accessToken = tokenStore.getAccessToken();
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant) {
    headers.set("tenant", tenant);
  }

  const response = await fetch(
    `${env.apiBase}/api/v1/billing/invoices/${encodeURIComponent(invoiceId)}/pdf`,
    { headers },
  );

  if (!response.ok) {
    throw new ApiRequestError(response.status, `Failed to download invoice PDF (${response.status})`);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  try {
    const link = document.createElement("a");
    link.href = objectUrl;
    link.download = `${invoiceNumber}.pdf`;
    document.body.appendChild(link);
    link.click();
    link.remove();
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

export function generateInvoices(periodYear: number, periodMonth: number): Promise<{ generated: number }> {
  return apiFetch<{ generated: number }>(`/api/v1/billing/invoices/generate`, {
    method: "POST",
    body: JSON.stringify({ periodYear, periodMonth }),
  });
}

export function issueInvoice(invoiceId: string, dueAtUtc: string | null): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/invoices/${encodeURIComponent(invoiceId)}/issue`, {
    method: "POST",
    body: JSON.stringify({ dueAtUtc }),
  });
}

export function markInvoicePaid(invoiceId: string): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/invoices/${encodeURIComponent(invoiceId)}/pay`, {
    method: "POST",
  });
}

export function voidInvoice(invoiceId: string, reason: string | null): Promise<string> {
  return apiFetch<string>(`/api/v1/billing/invoices/${encodeURIComponent(invoiceId)}/void`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}
