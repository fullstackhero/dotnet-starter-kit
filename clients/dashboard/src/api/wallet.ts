import { apiFetch } from "@/lib/api-client";
import type { PagedResult } from "@/api/billing";

// Wallet transaction kinds. The backend serializes enums as STRINGS
// (see project_api_string_enums); mirror them as a string union with an
// open `(string & {})` fallback so unknown future members don't break the type.
export type WalletTransactionKind =
  | "Credit"
  | "Debit"
  | "Adjustment"
  | (string & {});

export type WalletStatus = "Active" | "Suspended" | "Closed" | (string & {});

export type WalletTransactionDto = {
  id: string;
  amount: number;
  kind: WalletTransactionKind;
  description: string;
  referenceId?: string | null;
  createdAtUtc: string;
};

export type WalletDto = {
  id: string;
  tenantId: string;
  currency: string;
  balance: number;
  status: WalletStatus;
  createdAtUtc: string;
  recentTransactions: WalletTransactionDto[];
};

export type TopupRequestStatus =
  | "Pending"
  | "Invoiced"
  | "Completed"
  | "Rejected"
  | "Cancelled"
  | (string & {});

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

export type CreateTopupRequestInput = {
  amount: number;
  note?: string;
};

export type TopupRequestSearchParams = {
  status?: TopupRequestStatus;
  pageNumber?: number;
  pageSize?: number;
};

/** The current tenant's prepaid WhatsApp wallet (balance + recent ledger). */
export function getMyWallet() {
  return apiFetch<WalletDto>("/api/v1/billing/wallet/me");
}

/** Submit a top-up request for the current tenant. Returns the new request id. */
export function createTopupRequest(input: CreateTopupRequestInput) {
  return apiFetch<string>("/api/v1/billing/wallet/topup-requests", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

/** Paged list of the current tenant's own top-up requests, newest first. */
export function getMyTopupRequests(params: TopupRequestSearchParams = {}) {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<PagedResult<TopupRequestDto>>(
    `/api/v1/billing/wallet/topup-requests/me${suffix}`,
  );
}
