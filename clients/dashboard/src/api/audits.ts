import { apiFetch } from "@/lib/api-client";

// ────────────────────────────────────────────────────────────────────────
// Enum mirrors of the API contracts. Keep in lockstep with
// src/Modules/Auditing/Modules.Auditing.Contracts/AuditEnums.cs.
// ────────────────────────────────────────────────────────────────────────

export const AuditEventType = {
  None: 0,
  EntityChange: 1,
  Security: 2,
  Activity: 3,
  Exception: 4,
} as const;
export type AuditEventType = (typeof AuditEventType)[keyof typeof AuditEventType];

export const AUDIT_EVENT_TYPE_LABELS: Record<number, string> = {
  0: "Unknown",
  1: "Entity",
  2: "Security",
  3: "Activity",
  4: "Exception",
};

export const AuditSeverity = {
  None: 0,
  Trace: 1,
  Debug: 2,
  Information: 3,
  Warning: 4,
  Error: 5,
  Critical: 6,
} as const;
export type AuditSeverity = (typeof AuditSeverity)[keyof typeof AuditSeverity];

export const AUDIT_SEVERITY_LABELS: Record<number, string> = {
  0: "—",
  1: "Trace",
  2: "Debug",
  3: "Info",
  4: "Warn",
  5: "Error",
  6: "Critical",
};

/** Bitwise flags — keep in sync with AuditTag enum on the backend. */
export const AuditTag = {
  None: 0,
  PiiMasked: 1 << 0,
  OutOfQuota: 1 << 1,
  Sampled: 1 << 2,
  RetainedLong: 1 << 3,
  HealthCheck: 1 << 4,
  Authentication: 1 << 5,
  Authorization: 1 << 6,
} as const;
export type AuditTag = (typeof AuditTag)[keyof typeof AuditTag];

export const AUDIT_TAG_LABELS: Array<{ flag: number; name: string }> = [
  { flag: AuditTag.PiiMasked, name: "PII masked" },
  { flag: AuditTag.OutOfQuota, name: "Quota exceeded" },
  { flag: AuditTag.Sampled, name: "Sampled" },
  { flag: AuditTag.RetainedLong, name: "Retained" },
  { flag: AuditTag.HealthCheck, name: "Health check" },
  { flag: AuditTag.Authentication, name: "Auth" },
  { flag: AuditTag.Authorization, name: "Authz" },
];

export function decodeTags(mask: number): string[] {
  return AUDIT_TAG_LABELS.filter((t) => (mask & t.flag) !== 0).map((t) => t.name);
}

// ────────────────────────────────────────────────────────────────────────
// DTOs
// ────────────────────────────────────────────────────────────────────────

export type AuditSummaryDto = {
  id: string;
  occurredAtUtc: string;
  eventType: number;
  severity: number;
  tenantId?: string | null;
  userId?: string | null;
  userName?: string | null;
  traceId?: string | null;
  correlationId?: string | null;
  requestId?: string | null;
  source?: string | null;
  tags: number;
};

export type AuditDetailDto = AuditSummaryDto & {
  receivedAtUtc: string;
  spanId?: string | null;
  /** Server returns a JsonElement — comes through as already-parsed JSON. */
  payload: unknown;
};

export type AuditSummaryAggregateDto = {
  /** keys are stringified AuditEventType integers */
  eventsByType: Record<string, number>;
  eventsBySeverity: Record<string, number>;
  eventsBySource: Record<string, number>;
  eventsByTenant: Record<string, number>;
};

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

// ────────────────────────────────────────────────────────────────────────
// Query shape — matches GetAuditsQuery on the backend.
// ────────────────────────────────────────────────────────────────────────

export type ListAuditsQuery = {
  pageNumber?: number;
  pageSize?: number;
  fromUtc?: string;
  toUtc?: string;
  tenantId?: string;
  userId?: string;
  eventType?: AuditEventType;
  severity?: AuditSeverity;
  /** Bitmask of AuditTag values. */
  tags?: number;
  source?: string;
  correlationId?: string;
  traceId?: string;
  search?: string;
  sort?: string;
};

function toQueryString(query: Record<string, unknown>): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    // FluentValidation on the backend uses PascalCase parameter names by
    // default for [AsParameters]; ASP.NET Core's binder is case-insensitive,
    // so we send what's natural for JS callers.
    params.append(toPascal(key), String(value));
  }
  return params.toString();
}

function toPascal(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

// ────────────────────────────────────────────────────────────────────────
// Fetchers
// ────────────────────────────────────────────────────────────────────────

export async function listAudits(
  query: ListAuditsQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<AuditSummaryDto>> {
  const qs = toQueryString(query);
  return apiFetch<PagedResponse<AuditSummaryDto>>(
    `/api/v1/audits${qs ? `?${qs}` : ""}`,
    { signal },
  );
}

export async function getAuditById(
  id: string,
  signal?: AbortSignal,
): Promise<AuditDetailDto> {
  return apiFetch<AuditDetailDto>(`/api/v1/audits/${id}`, { signal });
}

export async function getAuditSummary(
  query: { fromUtc?: string; toUtc?: string; tenantId?: string },
  signal?: AbortSignal,
): Promise<AuditSummaryAggregateDto> {
  const qs = toQueryString(query);
  return apiFetch<AuditSummaryAggregateDto>(
    `/api/v1/audits/summary${qs ? `?${qs}` : ""}`,
    { signal },
  );
}

export async function getAuditsByCorrelation(
  correlationId: string,
  query: { fromUtc?: string; toUtc?: string } = {},
  signal?: AbortSignal,
): Promise<AuditSummaryDto[]> {
  const qs = toQueryString(query);
  return apiFetch<AuditSummaryDto[]>(
    `/api/v1/audits/by-correlation/${encodeURIComponent(correlationId)}${qs ? `?${qs}` : ""}`,
    { signal },
  );
}

export async function getAuditsByTrace(
  traceId: string,
  query: { fromUtc?: string; toUtc?: string } = {},
  signal?: AbortSignal,
): Promise<AuditSummaryDto[]> {
  const qs = toQueryString(query);
  return apiFetch<AuditSummaryDto[]>(
    `/api/v1/audits/by-trace/${encodeURIComponent(traceId)}${qs ? `?${qs}` : ""}`,
    { signal },
  );
}
