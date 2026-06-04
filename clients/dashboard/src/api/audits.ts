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
  /** Hide a single event type (e.g. "Activity" to drop system-level HTTP noise). */
  excludeEventType?: AuditEventType;
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
// Human-readable summaries — turn the terse `source` (e.g. "api.chat.ListMyChannels"
// or "IdentityDbContext") into a plain-English predicate so rows read like an
// activity feed ("Mukesh · viewed chat channels") without opening the details.
// The list DTO carries no field-level before/after, so entity changes summarize
// at the record level; the detail drawer still shows the full payload.
// ────────────────────────────────────────────────────────────────────────

// Past-tense verb for the leading word of a PascalCase action name.
const ACTION_VERBS: Record<string, string> = {
  list: "viewed", get: "viewed", fetch: "viewed", read: "viewed", search: "searched", export: "exported",
  create: "created", add: "added", register: "registered", invite: "invited", upload: "uploaded", generate: "generated",
  update: "updated", edit: "updated", set: "updated", change: "changed", adjust: "adjusted", rename: "renamed", reorder: "reordered", renew: "renewed",
  delete: "deleted", remove: "removed", revoke: "revoked", void: "voided", purge: "purged",
  assign: "assigned", toggle: "toggled", mark: "marked", confirm: "confirmed", resend: "re-sent",
  issue: "issued", refresh: "refreshed", enroll: "enrolled in", disable: "disabled", enable: "enabled",
  restore: "restored", pin: "pinned", unpin: "unpinned", join: "joined", leave: "left", react: "reacted to",
  impersonate: "impersonated", start: "started", end: "ended", download: "downloaded", capture: "captured",
};

// Whole-action phrases that don't decompose into a clean verb + object.
const ACTION_OVERRIDES: Record<string, string> = {
  IssueJwtToken: "signed in",
  RefreshToken: "refreshed their session",
  SseToken: "connected to the live stream",
  GetUnreadNotificationCount: "checked notifications",
  ListMyChannels: "opened chat",
};

function splitPascal(value: string): string[] {
  return value
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
    .split(/[\s._-]+/)
    .filter(Boolean);
}

function pastTense(verb: string): string {
  if (ACTION_VERBS[verb]) return ACTION_VERBS[verb];
  if (verb.endsWith("e")) return `${verb}d`;
  if (verb.endsWith("y")) return `${verb.slice(0, -1)}ied`;
  return `${verb}ed`;
}

function humanizeAction(action: string, area?: string): string {
  if (ACTION_OVERRIDES[action]) return ACTION_OVERRIDES[action];
  const words = splitPascal(action);
  if (words.length === 0) return area ? `accessed ${area}` : "performed an action";
  const verb = pastTense(words[0].toLowerCase());
  const object = words
    .slice(1)
    .filter((w) => w.toLowerCase() !== "my")
    .join(" ")
    .toLowerCase();
  const target = object || area || "";
  return target ? `${verb} ${target}` : verb;
}

/** Plain-English predicate for an audit row — pairs with the actor shown alongside
 *  it: `${actor} ${auditPredicate(row)}` → "Mukesh signed in". */
export function auditPredicate(row: Pick<AuditSummaryDto, "eventType" | "source">): string {
  const src = (row.source ?? "").trim();

  if (row.eventType === AuditEventType.Exception) {
    const parts = src.replace(/^api\./i, "").split(".").filter(Boolean);
    const where = parts.length ? splitPascal(parts[parts.length - 1]).join(" ").toLowerCase() : "";
    return where ? `hit an error in ${where}` : "hit an error";
  }

  if (row.eventType === AuditEventType.EntityChange) {
    const area = src.replace(/DbContext$/i, "").trim();
    const label = area ? splitPascal(area).join(" ").toLowerCase() : "some";
    return `changed ${label} records`;
  }

  // Activity / Security — "api.<area>.<Action>" (or a single segment).
  const parts = src.replace(/^api\./i, "").split(".").filter(Boolean);
  if (parts.length === 0) return "performed an action";
  if (parts.length === 1) return `accessed ${splitPascal(parts[0]).join(" ").toLowerCase()}`;
  const action = parts[parts.length - 1];
  const area = parts[0];
  return humanizeAction(action, area);
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

