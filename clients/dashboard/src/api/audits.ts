import { apiFetch } from "@/lib/api-client";

// ────────────────────────────────────────────────────────────────────────
// Enum mirrors of the API contracts. Keep in lockstep with
// src/Modules/Auditing/Modules.Auditing.Contracts/AuditEnums.cs.
// ────────────────────────────────────────────────────────────────────────

// The API serializes single-value enums as their string name
// (global JsonStringEnumConverter). Mirror them as string unions.
export const AuditEventType = {
  None: "None",
  EntityChange: "EntityChange",
  Security: "Security",
  Activity: "Activity",
  Exception: "Exception",
} as const;
export type AuditEventType = (typeof AuditEventType)[keyof typeof AuditEventType];

export const AUDIT_EVENT_TYPE_LABELS: Record<AuditEventType, string> = {
  None: "Unknown",
  EntityChange: "Entity",
  Security: "Security",
  Activity: "Activity",
  Exception: "Exception",
};

export const AuditSeverity = {
  None: "None",
  Trace: "Trace",
  Debug: "Debug",
  Information: "Information",
  Warning: "Warning",
  Error: "Error",
  Critical: "Critical",
} as const;
export type AuditSeverity = (typeof AuditSeverity)[keyof typeof AuditSeverity];

export const AUDIT_SEVERITY_LABELS: Record<AuditSeverity, string> = {
  None: "—",
  Trace: "Trace",
  Debug: "Debug",
  Information: "Info",
  Warning: "Warn",
  Error: "Error",
  Critical: "Critical",
};

/** Ordinal rank for severities — string severities are not ordered, so use
 *  this for any threshold (`<`/`>`/`>=`/`<=`) comparison. */
const SEVERITY_RANK: Record<AuditSeverity, number> = {
  None: 0,
  Trace: 1,
  Debug: 2,
  Information: 3,
  Warning: 4,
  Error: 5,
  Critical: 6,
};

export function severityRank(severity: AuditSeverity): number {
  return SEVERITY_RANK[severity] ?? 0;
}

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
  eventType: AuditEventType;
  severity: AuditSeverity;
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
  /** keys are AuditEventType / AuditSeverity string names */
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

