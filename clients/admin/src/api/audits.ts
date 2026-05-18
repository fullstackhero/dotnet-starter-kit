import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

export type AuditEventType = "None" | "EntityChange" | "Security" | "Activity" | "Exception";

export type AuditSeverity =
  | "None"
  | "Trace"
  | "Debug"
  | "Information"
  | "Warning"
  | "Error"
  | "Critical";

export type AuditTag =
  | "None"
  | "PiiMasked"
  | "OutOfQuota"
  | "Sampled"
  | "RetainedLong"
  | "HealthCheck"
  | "Authentication"
  | "Authorization";

export const AUDIT_EVENT_TYPES: AuditEventType[] = [
  "EntityChange",
  "Security",
  "Activity",
  "Exception",
];

export const AUDIT_SEVERITIES: AuditSeverity[] = [
  "Trace",
  "Debug",
  "Information",
  "Warning",
  "Error",
  "Critical",
];

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
  tags: AuditTag | number;
};

export type AuditDetailDto = AuditSummaryDto & {
  receivedAtUtc: string;
  spanId?: string | null;
  payload: unknown;
};

export type AuditSummaryAggregateDto = {
  eventsByType: Partial<Record<AuditEventType, number>>;
  eventsBySeverity: Partial<Record<AuditSeverity, number>>;
  eventsBySource: Record<string, number>;
  eventsByTenant: Record<string, number>;
};

export type ListAuditsParams = {
  pageNumber?: number;
  pageSize?: number;
  sort?: string;
  fromUtc?: string;
  toUtc?: string;
  tenantId?: string;
  userId?: string;
  eventType?: AuditEventType;
  severity?: AuditSeverity;
  source?: string;
  correlationId?: string;
  traceId?: string;
  search?: string;
};

const ROOT = "/api/v1/audits";

// ──────────────────────────────────────────────────────────────────────
// Enum normalization
//
// The server serializes audit enums as INTEGERS by default (System.Text.Json
// has no JsonStringEnumConverter registered for these). The client surface
// types them as string unions, so every code path that does
// `eventType.toUpperCase()` / `severity === "Warning"` would explode at
// runtime. Rather than fix every call site or change the server-side
// contract (which other consumers may also rely on), we normalize at the
// API boundary — one place, one fix.
//
// Index mirrors the C# enum declarations in
// `src/Modules/Auditing/Modules.Auditing.Contracts/AuditEnums.cs`.
// ──────────────────────────────────────────────────────────────────────

const EVENT_TYPE_BY_INT: readonly AuditEventType[] = [
  "None",
  "EntityChange",
  "Security",
  "Activity",
  "Exception",
];

const SEVERITY_BY_INT: readonly AuditSeverity[] = [
  "None",
  "Trace",
  "Debug",
  "Information",
  "Warning",
  "Error",
  "Critical",
];

function coerceEventType(raw: unknown): AuditEventType {
  if (typeof raw === "string") return raw as AuditEventType;
  if (typeof raw === "number") return EVENT_TYPE_BY_INT[raw] ?? "None";
  return "None";
}

function coerceSeverity(raw: unknown): AuditSeverity {
  if (typeof raw === "string") return raw as AuditSeverity;
  if (typeof raw === "number") return SEVERITY_BY_INT[raw] ?? "None";
  return "None";
}

function normalizeSummary<T extends AuditSummaryDto>(dto: T): T {
  return {
    ...dto,
    eventType: coerceEventType((dto as { eventType: unknown }).eventType),
    severity: coerceSeverity((dto as { severity: unknown }).severity),
  };
}

export async function listAudits(params: ListAuditsParams = {}): Promise<PagedResponse<AuditSummaryDto>> {
  const q = new URLSearchParams();
  q.set("PageNumber", String(params.pageNumber ?? 1));
  q.set("PageSize", String(params.pageSize ?? 25));
  if (params.sort) q.set("Sort", params.sort);
  if (params.fromUtc) q.set("FromUtc", params.fromUtc);
  if (params.toUtc) q.set("ToUtc", params.toUtc);
  if (params.tenantId) q.set("TenantId", params.tenantId);
  if (params.userId) q.set("UserId", params.userId);
  if (params.eventType) q.set("EventType", params.eventType);
  if (params.severity) q.set("Severity", params.severity);
  if (params.source) q.set("Source", params.source);
  if (params.correlationId) q.set("CorrelationId", params.correlationId);
  if (params.traceId) q.set("TraceId", params.traceId);
  if (params.search?.trim()) q.set("Search", params.search.trim());
  const page = await apiFetch<PagedResponse<AuditSummaryDto>>(`${ROOT}/?${q.toString()}`);
  return { ...page, items: page.items.map(normalizeSummary) };
}

export async function getAudit(id: string): Promise<AuditDetailDto> {
  const dto = await apiFetch<AuditDetailDto>(`${ROOT}/${encodeURIComponent(id)}`);
  return normalizeSummary(dto);
}

export async function getAuditSummary(params: {
  fromUtc?: string;
  toUtc?: string;
  tenantId?: string;
} = {}): Promise<AuditSummaryAggregateDto> {
  const q = new URLSearchParams();
  if (params.fromUtc) q.set("FromUtc", params.fromUtc);
  if (params.toUtc) q.set("ToUtc", params.toUtc);
  if (params.tenantId) q.set("TenantId", params.tenantId);
  const qs = q.toString();
  const raw = await apiFetch<{
    eventsByType: Record<string, number>;
    eventsBySeverity: Record<string, number>;
    eventsBySource: Record<string, number>;
    eventsByTenant: Record<string, number>;
  }>(`${ROOT}/summary${qs ? `?${qs}` : ""}`);

  // The server keys the histograms by the same integer enum form. Translate
  // them to the string union so the rest of the UI can index them by name.
  const eventsByType: Partial<Record<AuditEventType, number>> = {};
  for (const [k, v] of Object.entries(raw.eventsByType ?? {})) {
    const key = coerceEventType(/^\d+$/.test(k) ? Number(k) : k);
    eventsByType[key] = (eventsByType[key] ?? 0) + v;
  }
  const eventsBySeverity: Partial<Record<AuditSeverity, number>> = {};
  for (const [k, v] of Object.entries(raw.eventsBySeverity ?? {})) {
    const key = coerceSeverity(/^\d+$/.test(k) ? Number(k) : k);
    eventsBySeverity[key] = (eventsBySeverity[key] ?? 0) + v;
  }

  return {
    eventsByType,
    eventsBySeverity,
    eventsBySource: raw.eventsBySource ?? {},
    eventsByTenant: raw.eventsByTenant ?? {},
  };
}
