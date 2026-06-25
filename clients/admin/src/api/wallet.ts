import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

// ─── shared enums ────────────────────────────────────────────────────

export type TopupRequestStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Completed"
  | (string & {});

// ─── top-up requests ─────────────────────────────────────────────────

export type TopupRequestDto = {
  id: string;
  tenantId: string;
  amount: number;
  currency: string;
  note?: string | null;
  status: TopupRequestStatus;
  invoiceId?: string | null;
  requestedBy?: string | null;
  decisionNote?: string | null;
  createdAtUtc: string;
  decidedAtUtc?: string | null;
  completedAtUtc?: string | null;
};

export type ListTopupRequestsParams = {
  tenantId?: string;
  status?: TopupRequestStatus;
  pageNumber?: number;
  pageSize?: number;
};

export function listTopupRequests(
  params: ListTopupRequestsParams = {},
): Promise<PagedResponse<TopupRequestDto>> {
  const query = new URLSearchParams();
  if (params.tenantId) query.set("tenantId", params.tenantId);
  if (params.status) query.set("status", params.status);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  return apiFetch<PagedResponse<TopupRequestDto>>(
    `/api/v1/billing/wallet/topup-requests?${query.toString()}`,
  );
}

/** Approve a pending request — generates an invoice and returns its id. */
export function approveTopupRequest(id: string, note?: string): Promise<string> {
  return apiFetch<string>(
    `/api/v1/billing/wallet/topup-requests/${encodeURIComponent(id)}/approve`,
    {
      method: "POST",
      body: JSON.stringify({ note: note?.trim() ? note.trim() : null }),
    },
  );
}

/** Reject a pending request — returns the request id. */
export function rejectTopupRequest(id: string, reason?: string): Promise<string> {
  return apiFetch<string>(
    `/api/v1/billing/wallet/topup-requests/${encodeURIComponent(id)}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ reason: reason?.trim() ? reason.trim() : null }),
    },
  );
}
