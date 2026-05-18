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

export function listAudits(params: ListAuditsParams = {}): Promise<PagedResponse<AuditSummaryDto>> {
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
  return apiFetch<PagedResponse<AuditSummaryDto>>(`${ROOT}/?${q.toString()}`);
}

export function getAudit(id: string): Promise<AuditDetailDto> {
  return apiFetch<AuditDetailDto>(`${ROOT}/${encodeURIComponent(id)}`);
}

export function getAuditSummary(params: {
  fromUtc?: string;
  toUtc?: string;
  tenantId?: string;
} = {}): Promise<AuditSummaryAggregateDto> {
  const q = new URLSearchParams();
  if (params.fromUtc) q.set("FromUtc", params.fromUtc);
  if (params.toUtc) q.set("ToUtc", params.toUtc);
  if (params.tenantId) q.set("TenantId", params.tenantId);
  const qs = q.toString();
  return apiFetch<AuditSummaryAggregateDto>(`${ROOT}/summary${qs ? `?${qs}` : ""}`);
}
