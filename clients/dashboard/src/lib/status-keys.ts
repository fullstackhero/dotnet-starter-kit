import type { ParseKeys } from "i18next";

// Catalog keys for status enums the API sends. Kept free of runtime imports so
// tests/i18n/status-keys.spec.ts can load it: the spec reads each backend enum and fails when a
// member has no entry here or its key is missing from either catalog.

export const INVOICE_STATUS_KEYS = {
  Draft: "invoices.status.Draft",
  Issued: "invoices.status.Issued",
  Paid: "invoices.status.Paid",
  Void: "invoices.status.Void",
} as const satisfies Record<string, ParseKeys<"subscription">>;

export const TOPUP_STATUS_KEYS = {
  Pending: "wallet.status.Pending",
  Invoiced: "wallet.status.Invoiced",
  Completed: "wallet.status.Completed",
  Rejected: "wallet.status.Rejected",
  Cancelled: "wallet.status.Cancelled",
} as const satisfies Record<string, ParseKeys<"subscription">>;

/** The key for a status, or undefined for one the server added after this build. */
export function statusKey<K extends string>(keys: Readonly<Record<string, K>>, status: string): K | undefined {
  return Object.hasOwn(keys, status) ? keys[status] : undefined;
}
