import type { ParseKeys } from "i18next";

// Catalog keys for status enums the API sends. Kept free of runtime imports so
// tests/i18n/status-keys.spec.ts can load it: the spec reads each backend enum and fails when a
// member has no entry here or its key is missing from either catalog.

export const INVOICE_STATUS_KEYS = {
  Draft: "status.draft",
  Issued: "status.issued",
  Paid: "status.paid",
  Void: "status.void",
} as const satisfies Record<string, ParseKeys<"billing">>;

export const IMPERSONATION_STATUS_KEYS = {
  Active: "status.active",
  Ended: "status.ended",
  Revoked: "status.revoked",
  Expired: "status.expired",
} as const satisfies Record<string, ParseKeys<"impersonation">>;

/** The key for a status, or undefined for one the server added after this build. */
export function statusKey<K extends string>(keys: Readonly<Record<string, K>>, status: string): K | undefined {
  return Object.hasOwn(keys, status) ? keys[status] : undefined;
}
