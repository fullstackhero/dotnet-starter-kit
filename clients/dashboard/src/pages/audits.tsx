import { useEffect, useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertTriangle,
  ChevronRight,
  CircleAlert,
  Database,
  ExternalLink,
  Filter,
  Hash,
  Loader2,
  RefreshCw,
  Search,
  Shield,
  ShieldCheck,
  Tag,
  X,
} from "lucide-react";
import {
  AuditEventType,
  AuditSeverity,
  AUDIT_EVENT_TYPE_LABELS,
  AUDIT_SEVERITY_LABELS,
  AUDIT_TAG_LABELS,
  decodeTags,
  getAuditById,
  getAuditsByCorrelation,
  getAuditSummary,
  listAudits,
  type AuditDetailDto,
  type AuditSummaryDto,
} from "@/api/audits";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHero } from "@/components/list";
import {
  Dialog,
  DialogClose,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
} from "@/components/ui/dialog";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 25;

// ────────────────────────────────────────────────────────────────────────
// Time-range presets — keep the SQL window predictable from the UI.
// ────────────────────────────────────────────────────────────────────────

type RangeKey = "24h" | "7d" | "30d" | "90d";
const RANGE_OPTIONS: Array<{ key: RangeKey; label: string; ms: number }> = [
  { key: "24h", label: "24h", ms: 24 * 60 * 60 * 1000 },
  { key: "7d", label: "7d", ms: 7 * 24 * 60 * 60 * 1000 },
  { key: "30d", label: "30d", ms: 30 * 24 * 60 * 60 * 1000 },
  { key: "90d", label: "90d", ms: 90 * 24 * 60 * 60 * 1000 },
];

function rangeBounds(key: RangeKey): { from: string; to: string } {
  const opt = RANGE_OPTIONS.find((r) => r.key === key) ?? RANGE_OPTIONS[1];
  const to = new Date();
  const from = new Date(to.getTime() - opt.ms);
  return { from: from.toISOString(), to: to.toISOString() };
}

// ────────────────────────────────────────────────────────────────────────
// Severity / event-type tone — keep visual language consistent across
// the row colour bar, drawer header, and severity pills.
// ────────────────────────────────────────────────────────────────────────

function severityTone(severity: number): "default" | "info" | "warning" | "danger" {
  if (severity >= AuditSeverity.Critical) return "danger";
  if (severity >= AuditSeverity.Error) return "danger";
  if (severity >= AuditSeverity.Warning) return "warning";
  if (severity >= AuditSeverity.Information) return "info";
  return "default";
}

function severityColorVar(severity: number): string {
  const tone = severityTone(severity);
  return tone === "danger"
    ? "var(--color-destructive)"
    : tone === "warning"
      ? "var(--color-warning)"
      : tone === "info"
        ? "var(--color-info)"
        : "var(--color-muted-foreground)";
}

function eventTypeIcon(eventType: number): React.ComponentType<React.SVGProps<SVGSVGElement>> {
  if (eventType === AuditEventType.Security) return Shield;
  if (eventType === AuditEventType.Exception) return CircleAlert;
  if (eventType === AuditEventType.EntityChange) return Database;
  if (eventType === AuditEventType.Activity) return Activity;
  return Hash;
}

// ────────────────────────────────────────────────────────────────────────
// Time formatting — mono ISO for the table, locale-aware for the drawer.
// ────────────────────────────────────────────────────────────────────────

function fmtIsoDense(iso: string): { date: string; time: string } {
  // 2026-04-30 14:32:11.234
  const d = new Date(iso);
  const yyyy = d.getUTCFullYear();
  const mm = String(d.getUTCMonth() + 1).padStart(2, "0");
  const dd = String(d.getUTCDate()).padStart(2, "0");
  const hh = String(d.getUTCHours()).padStart(2, "0");
  const mi = String(d.getUTCMinutes()).padStart(2, "0");
  const ss = String(d.getUTCSeconds()).padStart(2, "0");
  const ms = String(d.getUTCMilliseconds()).padStart(3, "0");
  return { date: `${yyyy}-${mm}-${dd}`, time: `${hh}:${mi}:${ss}.${ms}` };
}

function fmtRelative(iso: string, now: number = Date.now()): string {
  const delta = Math.max(0, Math.floor((now - Date.parse(iso)) / 1000));
  if (delta < 5) return "just now";
  if (delta < 60) return `${delta}s ago`;
  const m = Math.floor(delta / 60);
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  const days = Math.floor(h / 24);
  return `${days}d ago`;
}

// ────────────────────────────────────────────────────────────────────────
// Filter state — single source of truth, lifted into URL-synced shape.
// ────────────────────────────────────────────────────────────────────────

type FilterState = {
  range: RangeKey;
  eventType: number | null;
  severity: number | null;
  tagsMask: number;
  source: string;
  user: string;
  correlation: string;
  trace: string;
  search: string;
  page: number;
};

const INITIAL_FILTERS: FilterState = {
  range: "7d",
  eventType: null,
  severity: null,
  tagsMask: 0,
  source: "",
  user: "",
  correlation: "",
  trace: "",
  search: "",
  page: 1,
};

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

export function AuditsPage() {
  const { user } = useAuth();
  const [filters, setFilters] = useState<FilterState>(INITIAL_FILTERS);
  const [searchInput, setSearchInput] = useState("");
  const [drawerId, setDrawerId] = useState<string | null>(null);

  // Debounce the search input so we don't hammer the API on every keystroke.
  useEffect(() => {
    const t = window.setTimeout(() => {
      setFilters((f) => ({ ...f, search: searchInput, page: 1 }));
    }, 300);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  const window_ = useMemo(() => rangeBounds(filters.range), [filters.range]);

  const auditsQuery = useQuery({
    queryKey: ["audits", "list", filters, window_],
    queryFn: ({ signal }) =>
      listAudits(
        {
          pageNumber: filters.page,
          pageSize: PAGE_SIZE,
          fromUtc: window_.from,
          toUtc: window_.to,
          eventType: (filters.eventType ?? undefined) as AuditEventType | undefined,
          severity: (filters.severity ?? undefined) as AuditSeverity | undefined,
          tags: filters.tagsMask || undefined,
          source: filters.source || undefined,
          userId: filters.user || undefined,
          correlationId: filters.correlation || undefined,
          traceId: filters.trace || undefined,
          search: filters.search || undefined,
        },
        signal,
      ),
    placeholderData: keepPreviousData,
    staleTime: 5_000,
  });

  const summaryQuery = useQuery({
    queryKey: ["audits", "summary", window_],
    queryFn: ({ signal }) =>
      getAuditSummary({ fromUtc: window_.from, toUtc: window_.to }, signal),
    staleTime: 30_000,
  });

  const totals = useMemo(() => {
    const s = summaryQuery.data;
    if (!s) return null;
    const grand = Object.values(s.eventsByType).reduce((a, b) => a + b, 0);
    return {
      grand,
      byType: {
        activity: s.eventsByType[String(AuditEventType.Activity)] ?? 0,
        entity: s.eventsByType[String(AuditEventType.EntityChange)] ?? 0,
        security: s.eventsByType[String(AuditEventType.Security)] ?? 0,
        exception: s.eventsByType[String(AuditEventType.Exception)] ?? 0,
      },
      bySeverity: {
        info: s.eventsBySeverity[String(AuditSeverity.Information)] ?? 0,
        warn: s.eventsBySeverity[String(AuditSeverity.Warning)] ?? 0,
        err: s.eventsBySeverity[String(AuditSeverity.Error)] ?? 0,
        crit: s.eventsBySeverity[String(AuditSeverity.Critical)] ?? 0,
      },
      bySource: Object.entries(s.eventsBySource)
        .sort((a, b) => b[1] - a[1])
        .slice(0, 5),
    };
  }, [summaryQuery.data]);

  const paged = auditsQuery.data;
  const items = paged?.items ?? [];

  const onResetFilters = () => {
    setFilters(INITIAL_FILTERS);
    setSearchInput("");
  };

  const activeChipCount =
    (filters.eventType !== null ? 1 : 0) +
    (filters.severity !== null ? 1 : 0) +
    (filters.tagsMask ? 1 : 0) +
    (filters.source ? 1 : 0) +
    (filters.user ? 1 : 0) +
    (filters.correlation ? 1 : 0) +
    (filters.trace ? 1 : 0);

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="System · Audit"
        tenant={user?.tenant ?? "—"}
        title="Audit trail"
        subtitle="Activity, security, entity-change, and exception events across the platform. Window enforced server-side; max 90 days."
        actions={
          <Button
            variant="outline"
            size="sm"
            disabled={auditsQuery.isFetching}
            onClick={() => {
              void auditsQuery.refetch();
              void summaryQuery.refetch();
            }}
          >
            <RefreshCw
              className={cn("mr-1.5 h-3.5 w-3.5", auditsQuery.isFetching && "animate-spin")}
            />
            Refresh
          </Button>
        }
      />

      {/* ── Summary strip ─────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-2">
        <SummaryStrip
          loading={summaryQuery.isLoading}
          total={totals?.grand ?? 0}
          byType={totals?.byType}
          bySeverity={totals?.bySeverity}
          topSources={totals?.bySource ?? []}
          range={filters.range}
        />
      </section>

      {/* ── Filter bar ────────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-3 space-y-3">
        <FilterBar
          filters={filters}
          searchInput={searchInput}
          activeChipCount={activeChipCount}
          onPatch={(p) => setFilters((f) => ({ ...f, ...p, page: 1 }))}
          onSearchInput={setSearchInput}
          onReset={onResetFilters}
        />
      </section>

      {/* ── Table ────────────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-4">
        <AuditsTable
          rows={items}
          loading={auditsQuery.isLoading}
          fetching={auditsQuery.isFetching}
          error={auditsQuery.isError ? (auditsQuery.error as Error)?.message : null}
          onRowClick={(row) => setDrawerId(row.id)}
        />
      </section>

      {/* ── Pagination ───────────────────────────────────────────────── */}
      {paged && paged.totalCount > 0 && (
        <section className="fsh-enter fsh-enter-4">
          <PaginationFooter
            page={filters.page}
            pageSize={PAGE_SIZE}
            totalCount={paged.totalCount}
            totalPages={paged.totalPages}
            shown={items.length}
            fetching={auditsQuery.isFetching}
            onPage={(p) => setFilters((f) => ({ ...f, page: p }))}
          />
        </section>
      )}

      {/* ── Detail drawer ────────────────────────────────────────────── */}
      <AuditDetailDrawer
        auditId={drawerId}
        onClose={() => setDrawerId(null)}
        onJumpAudit={(id) => setDrawerId(id)}
        onJumpCorrelation={(cid) => {
          setDrawerId(null);
          setFilters((f) => ({ ...INITIAL_FILTERS, range: f.range, correlation: cid }));
        }}
        onJumpTrace={(tid) => {
          setDrawerId(null);
          setFilters((f) => ({ ...INITIAL_FILTERS, range: f.range, trace: tid }));
        }}
      />
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Summary strip — compact totals bar above the filters. Shows grand
// total, breakdown by type as a horizontal bar, severity dots, and
// top-5 sources as right-aligned mono chips.
// ────────────────────────────────────────────────────────────────────────

function SummaryStrip({
  loading,
  total,
  byType,
  bySeverity,
  topSources,
  range,
}: {
  loading: boolean;
  total: number;
  byType?: { activity: number; entity: number; security: number; exception: number };
  bySeverity?: { info: number; warn: number; err: number; crit: number };
  topSources: Array<[string, number]>;
  range: RangeKey;
}) {
  if (loading || !byType || !bySeverity) {
    return (
      <Card>
        <CardContent className="grid grid-cols-1 gap-4 px-6 py-4 lg:grid-cols-[auto_1fr_auto] lg:items-center">
          <Skeleton className="h-9 w-24" />
          <Skeleton className="h-3 w-full" />
          <Skeleton className="h-7 w-72" />
        </CardContent>
      </Card>
    );
  }

  // Stack-bar segments — fall back to a single muted segment when the
  // window has zero activity, so the strip still has visual mass.
  const t = Math.max(1, total);
  const segments = [
    { key: "activity", value: byType.activity, color: "var(--color-primary)" },
    { key: "entity", value: byType.entity, color: "var(--color-info)" },
    { key: "security", value: byType.security, color: "var(--color-success)" },
    { key: "exception", value: byType.exception, color: "var(--color-destructive)" },
  ];

  return (
    <Card className="relative overflow-hidden">
      <CardContent className="grid grid-cols-1 gap-5 px-6 py-4 lg:grid-cols-[auto_1fr_auto] lg:items-center">
        <div>
          <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            Window {range}
          </div>
          <div className="mt-1 text-display text-2xl font-semibold tabular-nums leading-none">
            {new Intl.NumberFormat("en-US").format(total)}
          </div>
          <div className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">events</div>
        </div>

        <div className="space-y-2">
          <div className="flex h-2 w-full overflow-hidden rounded-full bg-[var(--color-muted)]">
            {segments.map((s) => (
              <div
                key={s.key}
                title={`${s.key}: ${s.value}`}
                style={{
                  width: `${(s.value / t) * 100}%`,
                  backgroundColor: s.color,
                  transition: "width 600ms var(--ease-out-cubic)",
                }}
              />
            ))}
          </div>
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            <Legend dot="var(--color-primary)" label="Activity" value={byType.activity} />
            <Legend dot="var(--color-info)" label="Entity" value={byType.entity} />
            <Legend dot="var(--color-success)" label="Security" value={byType.security} />
            <Legend dot="var(--color-destructive)" label="Exception" value={byType.exception} />
            <span aria-hidden className="ml-auto h-3 w-px bg-[var(--color-border)]" />
            <SeverityDot color="var(--color-warning)" label="Warn" value={bySeverity.warn} />
            <SeverityDot color="var(--color-destructive)" label="Err" value={bySeverity.err + bySeverity.crit} />
          </div>
        </div>

        <div className="flex flex-col items-end gap-1.5">
          <div className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            Top sources
          </div>
          <div className="flex max-w-[28rem] flex-wrap justify-end gap-1.5">
            {topSources.length === 0 && (
              <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">—</span>
            )}
            {topSources.map(([name, count]) => (
              <span
                key={name}
                className="inline-flex items-center gap-1.5 rounded-full bg-[var(--color-muted)] px-2 py-0.5 font-mono text-[10.5px]"
                title={name}
              >
                <span className="max-w-[12rem] truncate text-[var(--color-foreground)]">{name}</span>
                <span className="tabular-nums text-[var(--color-muted-foreground)]">{count}</span>
              </span>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function Legend({ dot, label, value }: { dot: string; label: string; value: number }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: dot }} />
      <span>{label}</span>
      <span className="tabular-nums text-[var(--color-foreground)]">{value}</span>
    </span>
  );
}

function SeverityDot({ color, label, value }: { color: string; label: string; value: number }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span className="inline-block h-1.5 w-1.5 rounded-full" style={{ backgroundColor: color }} />
      <span>{label}</span>
      <span className="tabular-nums text-[var(--color-foreground)]">{value}</span>
    </span>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Filter bar — time range presets up top, search + advanced fields below.
// ────────────────────────────────────────────────────────────────────────

function FilterBar({
  filters,
  searchInput,
  activeChipCount,
  onPatch,
  onSearchInput,
  onReset,
}: {
  filters: FilterState;
  searchInput: string;
  activeChipCount: number;
  onPatch: (patch: Partial<FilterState>) => void;
  onSearchInput: (v: string) => void;
  onReset: () => void;
}) {
  const [advancedOpen, setAdvancedOpen] = useState(false);

  return (
    <Card>
      <CardContent className="space-y-3 px-5 py-4">
        {/* Row 1: range presets + search box + advanced toggle */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-1 rounded-lg bg-[var(--color-muted)] p-0.5">
            {RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.key}
                type="button"
                onClick={() => onPatch({ range: opt.key })}
                className={cn(
                  "rounded-md px-3 py-1 font-mono text-[11px] uppercase tracking-[0.08em] transition-colors",
                  filters.range === opt.key
                    ? "bg-[var(--color-card)] text-[var(--color-foreground)] shadow-[0_1px_0_oklch(0_0_0_/_0.05)]"
                    : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>

          <div className="relative flex-1 min-w-[14rem]">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
            <Input
              value={searchInput}
              onChange={(e) => onSearchInput(e.target.value)}
              placeholder="Search payload, source, user…"
              className="pl-9"
            />
            {searchInput && (
              <button
                type="button"
                onClick={() => onSearchInput("")}
                aria-label="Clear search"
                className="absolute right-2 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            )}
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setAdvancedOpen((v) => !v)}
            className="gap-1.5"
          >
            <Filter className="h-3.5 w-3.5" />
            Advanced
            {activeChipCount > 0 && (
              <Badge variant="brand" className="ml-1 px-1.5 py-0 text-[10px]">
                {activeChipCount}
              </Badge>
            )}
          </Button>

          {activeChipCount > 0 && (
            <button
              type="button"
              onClick={onReset}
              className="font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
            >
              Reset
            </button>
          )}
        </div>

        {/* Row 2: event type + severity chips */}
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            Type
          </span>
          {[AuditEventType.Activity, AuditEventType.Security, AuditEventType.EntityChange, AuditEventType.Exception].map((t) => (
            <Chip
              key={t}
              active={filters.eventType === t}
              onClick={() =>
                onPatch({ eventType: filters.eventType === t ? null : t })
              }
            >
              {AUDIT_EVENT_TYPE_LABELS[t]}
            </Chip>
          ))}

          <span aria-hidden className="mx-1 h-4 w-px bg-[var(--color-border)]" />

          <span className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            Severity
          </span>
          {[AuditSeverity.Information, AuditSeverity.Warning, AuditSeverity.Error, AuditSeverity.Critical].map((s) => (
            <Chip
              key={s}
              active={filters.severity === s}
              tone={severityTone(s)}
              onClick={() =>
                onPatch({ severity: filters.severity === s ? null : s })
              }
            >
              {AUDIT_SEVERITY_LABELS[s]}
            </Chip>
          ))}
        </div>

        {/* Row 3: advanced (collapsible) */}
        {advancedOpen && (
          <div className="grid grid-cols-1 gap-2 border-t border-[var(--color-border)] pt-3 sm:grid-cols-2 lg:grid-cols-3">
            <FieldInput
              label="Source"
              placeholder="api.identity.RegisterUser"
              value={filters.source}
              onChange={(v) => onPatch({ source: v })}
            />
            <FieldInput
              label="User ID"
              placeholder="00000000-0000-…"
              value={filters.user}
              onChange={(v) => onPatch({ user: v })}
            />
            <FieldInput
              label="Correlation"
              placeholder="0HMxxxx…"
              value={filters.correlation}
              onChange={(v) => onPatch({ correlation: v })}
            />
            <FieldInput
              label="Trace"
              placeholder="hex traceparent"
              value={filters.trace}
              onChange={(v) => onPatch({ trace: v })}
            />
            <div className="sm:col-span-2 lg:col-span-1">
              <div className="mb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                Tags
              </div>
              <div className="flex flex-wrap gap-1.5">
                {AUDIT_TAG_LABELS.map((t) => {
                  const active = (filters.tagsMask & t.flag) !== 0;
                  return (
                    <Chip
                      key={t.flag}
                      active={active}
                      onClick={() =>
                        onPatch({
                          tagsMask: active
                            ? filters.tagsMask & ~t.flag
                            : filters.tagsMask | t.flag,
                        })
                      }
                    >
                      {t.name}
                    </Chip>
                  );
                })}
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function Chip({
  active,
  tone,
  onClick,
  children,
}: {
  active: boolean;
  tone?: "default" | "info" | "warning" | "danger";
  onClick: () => void;
  children: React.ReactNode;
}) {
  const activeColor =
    tone === "danger"
      ? "var(--color-destructive)"
      : tone === "warning"
        ? "var(--color-warning)"
        : tone === "info"
          ? "var(--color-info)"
          : "var(--color-primary)";
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex h-7 items-center gap-1 rounded-full px-2.5 text-[11px] font-medium tracking-tight",
        "border transition-colors",
        active
          ? "border-transparent text-[var(--color-primary-foreground)] shadow-[var(--highlight-top)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
      )}
      style={active ? { backgroundColor: activeColor } : undefined}
    >
      {children}
    </button>
  );
}

function FieldInput({
  label,
  placeholder,
  value,
  onChange,
}: {
  label: string;
  placeholder: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <label className="block">
      <div className="mb-1 font-mono text-[10.5px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="h-8 font-mono text-[12px]"
      />
    </label>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Table — dense forensic-style rows. Each row has a 2px tone bar tied to
// severity, mono timestamp split into date/time, type icon, source, and
// user. Click anywhere in the row to open the detail drawer.
// ────────────────────────────────────────────────────────────────────────

function AuditsTable({
  rows,
  loading,
  fetching,
  error,
  onRowClick,
}: {
  rows: AuditSummaryDto[];
  loading: boolean;
  fetching: boolean;
  error: string | null;
  onRowClick: (row: AuditSummaryDto) => void;
}) {
  if (loading) {
    return (
      <Card>
        <CardContent className="p-0">
          {[0, 1, 2, 3, 4, 5, 6, 7].map((i) => (
            <div
              key={i}
              className="flex items-center gap-4 border-t border-[var(--color-border)] px-5 py-3 first:border-t-0"
            >
              <Skeleton className="h-3 w-32" />
              <Skeleton className="h-3 w-16" />
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-3 w-44" />
              <Skeleton className="ml-auto h-3 w-24" />
            </div>
          ))}
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-2 px-6 py-10 text-center">
          <CircleAlert className="h-5 w-5 text-[var(--color-destructive)]" />
          <div className="text-sm font-medium tracking-tight">Failed to load audits</div>
          <p className="max-w-md text-xs leading-relaxed text-[var(--color-muted-foreground)]">
            {error}
          </p>
        </CardContent>
      </Card>
    );
  }

  if (rows.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-2 px-6 py-12 text-center">
          <ShieldCheck className="h-5 w-5 text-[var(--color-muted-foreground)]" />
          <div className="text-sm font-medium tracking-tight">No audits in this window</div>
          <p className="max-w-md text-xs leading-relaxed text-[var(--color-muted-foreground)]">
            Try widening the time range or relaxing the filters. Activity events
            arrive as soon as the platform handles a request.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={cn("relative overflow-hidden", fetching && "opacity-95")}>
      {fetching && (
        <div className="absolute right-3 top-3 z-10">
          <Loader2 className="h-3.5 w-3.5 animate-spin text-[var(--color-muted-foreground)]" />
        </div>
      )}
      <CardContent className="p-0">
        <div className="overflow-x-auto">
          <table className="w-full table-auto border-collapse text-left text-sm">
            <thead>
              <tr className="border-b border-[var(--color-border)] text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
                <Th>Timestamp</Th>
                <Th>Type</Th>
                <Th>Severity</Th>
                <Th>Source</Th>
                <Th>User</Th>
                <Th>Correlation</Th>
                <Th>Tags</Th>
                <Th className="w-8"> </Th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row, idx) => (
                <Row key={row.id} row={row} striped={idx % 2 === 1} onClick={() => onRowClick(row)} />
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}

function Th({ children, className }: { children: React.ReactNode; className?: string }) {
  return <th className={cn("font-mono px-4 py-2.5 font-medium", className)}>{children}</th>;
}

function Row({
  row,
  striped,
  onClick,
}: {
  row: AuditSummaryDto;
  striped: boolean;
  onClick: () => void;
}) {
  const ts = fmtIsoDense(row.occurredAtUtc);
  const Icon = eventTypeIcon(row.eventType);
  const tone = severityTone(row.severity);
  const toneColor = severityColorVar(row.severity);
  const tags = decodeTags(row.tags);

  return (
    <tr
      onClick={onClick}
      className={cn(
        "group/row relative cursor-pointer border-t border-[var(--color-border)] transition-colors",
        striped && "bg-[var(--color-surface-1)]",
        "hover:bg-[var(--color-accent)]",
      )}
    >
      {/* Severity tone bar — 2px on the leftmost edge */}
      <td className="relative px-4 py-2.5">
        <span
          aria-hidden
          className="absolute left-0 top-0 h-full w-[2px]"
          style={{ backgroundColor: toneColor }}
        />
        <div className="flex flex-col leading-tight">
          <span className="font-mono text-[12px] tabular-nums text-[var(--color-foreground)]">
            {ts.time}
          </span>
          <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
            {ts.date}
          </span>
        </div>
      </td>
      <td className="px-4 py-2.5">
        <span className="inline-flex items-center gap-1.5 text-[12px] font-medium tracking-tight">
          <Icon className="h-3.5 w-3.5" style={{ color: toneColor }} aria-hidden />
          {AUDIT_EVENT_TYPE_LABELS[row.eventType]}
        </span>
      </td>
      <td className="px-4 py-2.5">
        <Badge variant={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}>
          {AUDIT_SEVERITY_LABELS[row.severity]}
        </Badge>
      </td>
      <td className="px-4 py-2.5">
        <code className="font-mono text-[11.5px] text-[var(--color-foreground)]">
          {row.source ?? "—"}
        </code>
      </td>
      <td className="max-w-[14rem] px-4 py-2.5">
        <div className="truncate">
          <span className="text-[12.5px]">{row.userName ?? "—"}</span>
          {row.userId && (
            <div className="font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
              {row.userId.slice(0, 8)}…
            </div>
          )}
        </div>
      </td>
      <td className="px-4 py-2.5">
        {row.correlationId ? (
          <code
            title={row.correlationId}
            className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]"
          >
            {row.correlationId.length > 14 ? `${row.correlationId.slice(0, 14)}…` : row.correlationId}
          </code>
        ) : (
          <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">—</span>
        )}
      </td>
      <td className="px-4 py-2.5">
        {tags.length === 0 ? (
          <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">—</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {tags.slice(0, 2).map((name) => (
              <span
                key={name}
                className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10px]"
              >
                <Tag className="h-2.5 w-2.5" />
                {name}
              </span>
            ))}
            {tags.length > 2 && (
              <span className="font-mono text-[10px] text-[var(--color-muted-foreground)]">
                +{tags.length - 2}
              </span>
            )}
          </div>
        )}
      </td>
      <td className="px-2 py-2.5">
        <ChevronRight
          className="h-4 w-4 text-[var(--color-muted-foreground)] transition-transform group-hover/row:translate-x-0.5 group-hover/row:text-[var(--color-foreground)]"
          aria-hidden
        />
      </td>
    </tr>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Pagination footer (custom — list/Pagination doesn't expose page jumps)
// ────────────────────────────────────────────────────────────────────────

function PaginationFooter({
  page,
  pageSize,
  totalCount,
  totalPages,
  shown,
  fetching,
  onPage,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  shown: number;
  fetching: boolean;
  onPage: (p: number) => void;
}) {
  const start = (page - 1) * pageSize + 1;
  const end = start + shown - 1;
  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
        {start.toLocaleString()}–{end.toLocaleString()} of{" "}
        <span className="text-[var(--color-foreground)] tabular-nums">
          {totalCount.toLocaleString()}
        </span>
        {" · "}
        page {page} / {totalPages}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={page <= 1 || fetching} onClick={() => onPage(page - 1)}>
          Previous
        </Button>
        <Button variant="outline" size="sm" disabled={page >= totalPages || fetching} onClick={() => onPage(page + 1)}>
          Next
        </Button>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Detail drawer — right-side panel with the full payload, metadata grid,
// and "jump to correlated/traced events" actions. Re-uses the existing
// Dialog primitive but overrides positioning so it slides in from the
// right instead of opening centered.
// ────────────────────────────────────────────────────────────────────────

function AuditDetailDrawer({
  auditId,
  onClose,
  onJumpAudit,
  onJumpCorrelation,
  onJumpTrace,
}: {
  auditId: string | null;
  onClose: () => void;
  onJumpAudit: (id: string) => void;
  onJumpCorrelation: (id: string) => void;
  onJumpTrace: (id: string) => void;
}) {
  const open = auditId !== null;

  const detail = useQuery({
    queryKey: ["audit", "detail", auditId],
    queryFn: ({ signal }) => getAuditById(auditId!, signal),
    enabled: open,
    staleTime: 60_000,
  });

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogPortal>
        <DialogOverlay />
        <DialogPrimitive.Content
          className={cn(
            // Slide-in panel from the right edge.
            "fixed right-0 top-0 z-50 h-screen w-full max-w-[640px] outline-none",
            "card-shell rounded-none border-y-0 border-r-0",
            "shadow-[-12px_0_40px_-16px_oklch(0_0_0_/_0.35)]",
            "data-[state=open]:animate-in data-[state=open]:slide-in-from-right",
            "data-[state=closed]:animate-out data-[state=closed]:slide-out-to-right",
            "duration-[var(--duration-default)]",
          )}
        >
          <DialogTitle className="sr-only">Audit detail</DialogTitle>
          <DialogDescription className="sr-only">
            Full payload, identifiers, and related actions for the selected audit event.
          </DialogDescription>

          <div className="flex h-full flex-col">
            <DrawerHeader detail={detail.data} loading={detail.isLoading} />

            <div className="flex-1 overflow-y-auto px-6 pb-6">
              {detail.isLoading ? (
                <DrawerSkeleton />
              ) : detail.isError ? (
                <DrawerError message={(detail.error as Error)?.message} />
              ) : detail.data ? (
                <DrawerBody
                  detail={detail.data}
                  onJumpAudit={onJumpAudit}
                  onJumpCorrelation={onJumpCorrelation}
                  onJumpTrace={onJumpTrace}
                />
              ) : null}
            </div>
          </div>

          <DialogClose
            aria-label="Close"
            className={cn(
              "absolute right-4 top-4 grid h-8 w-8 place-items-center rounded-md",
              "text-[var(--color-muted-foreground)] transition-colors",
              "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <X className="h-4 w-4" />
          </DialogClose>
        </DialogPrimitive.Content>
      </DialogPortal>
    </Dialog>
  );
}

function DrawerHeader({ detail, loading }: { detail?: AuditDetailDto; loading: boolean }) {
  if (loading || !detail) {
    return (
      <div className="border-b border-[var(--color-border)] px-6 py-5">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="mt-2 h-7 w-48" />
        <Skeleton className="mt-1 h-3 w-72" />
      </div>
    );
  }

  const Icon = eventTypeIcon(detail.eventType);
  const tone = severityTone(detail.severity);
  const toneColor = severityColorVar(detail.severity);
  const ts = fmtIsoDense(detail.occurredAtUtc);
  const tags = decodeTags(detail.tags);

  return (
    <div className="relative border-b border-[var(--color-border)] px-6 py-5">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: `linear-gradient(180deg, oklch(from ${toneColor} l c h / 0.08), transparent 60%)`,
        }}
      />
      <div className="relative">
        <div className="flex items-center gap-2">
          <span
            aria-hidden
            className="grid h-7 w-7 place-items-center rounded-md ring-1 ring-inset"
            style={{
              background: `linear-gradient(135deg, oklch(from ${toneColor} l c h / 0.20), oklch(from ${toneColor} l c h / 0.02))`,
              color: toneColor,
              boxShadow: `inset 0 0 0 1px oklch(from ${toneColor} l c h / 0.25)`,
            }}
          >
            <Icon className="h-3.5 w-3.5" />
          </span>
          <Badge variant={tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "info" ? "info" : "default"}>
            {AUDIT_SEVERITY_LABELS[detail.severity]}
          </Badge>
          <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {AUDIT_EVENT_TYPE_LABELS[detail.eventType]}
          </span>
        </div>
        <div className="mt-2 flex items-baseline gap-3">
          <h2 className="text-display text-xl font-semibold leading-tight tracking-tight">
            {detail.source ?? "Audit event"}
          </h2>
        </div>
        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          <span>{ts.date} {ts.time} UTC</span>
          <span aria-hidden>·</span>
          <span>{fmtRelative(detail.occurredAtUtc)}</span>
        </div>
        {tags.length > 0 && (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {tags.map((t) => (
              <span
                key={t}
                className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-2 py-0.5 font-mono text-[10.5px]"
              >
                <Tag className="h-2.5 w-2.5" />
                {t}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function DrawerBody({
  detail,
  onJumpAudit,
  onJumpCorrelation,
  onJumpTrace,
}: {
  detail: AuditDetailDto;
  onJumpAudit: (id: string) => void;
  onJumpCorrelation: (id: string) => void;
  onJumpTrace: (id: string) => void;
}) {
  return (
    <div className="space-y-5 pt-5">
      {/* Identity grid */}
      <section>
        <SectionLabel>Identity</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow label="Tenant" value={detail.tenantId ?? "—"} mono />
          <DefRow label="User" value={detail.userName ?? detail.userId ?? "—"} />
          <DefRow label="User ID" value={detail.userId ?? "—"} mono />
          <DefRow label="Source" value={detail.source ?? "—"} mono />
        </dl>
      </section>

      {/* Trace grid + jump links */}
      <section>
        <SectionLabel>Trace</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow label="Trace ID" value={detail.traceId ?? "—"} mono />
          <DefRow label="Span ID" value={detail.spanId ?? "—"} mono />
          <DefRow label="Correlation ID" value={detail.correlationId ?? "—"} mono />
          <DefRow label="Request ID" value={detail.requestId ?? "—"} mono />
        </dl>
        <div className="mt-3 flex flex-wrap gap-2">
          {detail.correlationId && (
            <Button
              variant="soft"
              size="sm"
              onClick={() => onJumpCorrelation(detail.correlationId!)}
            >
              <ExternalLink className="mr-1.5 h-3 w-3" /> All by correlation
            </Button>
          )}
          {detail.traceId && (
            <Button variant="soft" size="sm" onClick={() => onJumpTrace(detail.traceId!)}>
              <ExternalLink className="mr-1.5 h-3 w-3" /> All by trace
            </Button>
          )}
        </div>
      </section>

      {/* Related events — every audit sharing this correlation ID,
          rendered as a vertical timeline. Click another row to swap
          the drawer to that audit without closing. */}
      {detail.correlationId && (
        <RelatedEventsSection
          currentId={detail.id}
          correlationId={detail.correlationId}
          currentOccurredAtUtc={detail.occurredAtUtc}
          onJumpAudit={onJumpAudit}
        />
      )}

      {/* Payload */}
      <section>
        <div className="flex items-center justify-between">
          <SectionLabel>Payload</SectionLabel>
          <CopyButton value={JSON.stringify(detail.payload, null, 2)} />
        </div>
        <pre className="mt-2 max-h-[60vh] overflow-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-1)] p-3 font-mono text-[11px] leading-snug text-[var(--color-foreground)]">
          {JSON.stringify(detail.payload, null, 2)}
        </pre>
      </section>

      {/* Reception window */}
      <section>
        <SectionLabel>Pipeline</SectionLabel>
        <dl className="mt-2 grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
          <DefRow
            label="Occurred"
            value={`${fmtIsoDense(detail.occurredAtUtc).date} ${fmtIsoDense(detail.occurredAtUtc).time}`}
            mono
          />
          <DefRow
            label="Received"
            value={`${fmtIsoDense(detail.receivedAtUtc).date} ${fmtIsoDense(detail.receivedAtUtc).time}`}
            mono
          />
          <DefRow
            label="Sink delay"
            value={`${Math.max(0, Date.parse(detail.receivedAtUtc) - Date.parse(detail.occurredAtUtc))} ms`}
            mono
          />
          <DefRow label="Audit ID" value={detail.id} mono />
        </dl>
      </section>
    </div>
  );
}

function DrawerSkeleton() {
  return (
    <div className="space-y-5 pt-5">
      {[0, 1, 2].map((i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-3 w-24" />
          <div className="grid grid-cols-2 gap-2">
            {[0, 1, 2, 3].map((j) => (
              <Skeleton key={j} className="h-4 w-full" />
            ))}
          </div>
        </div>
      ))}
      <Skeleton className="h-48 w-full rounded-lg" />
    </div>
  );
}

function DrawerError({ message }: { message?: string }) {
  return (
    <div className="flex flex-col items-center gap-2 pt-12 text-center">
      <AlertTriangle className="h-5 w-5 text-[var(--color-destructive)]" />
      <div className="text-sm font-medium tracking-tight">Could not load audit</div>
      <p className="max-w-md text-xs leading-relaxed text-[var(--color-muted-foreground)]">
        {message ?? "The server returned an error fetching this audit. The record may have been purged by the retention job."}
      </p>
    </div>
  );
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
      {children}
    </div>
  );
}

function DefRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="font-mono text-[10px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd className={cn("text-[12.5px]", mono && "font-mono break-all")}>{value}</dd>
    </div>
  );
}

function RelatedEventsSection({
  currentId,
  correlationId,
  currentOccurredAtUtc,
  onJumpAudit,
}: {
  currentId: string;
  correlationId: string;
  currentOccurredAtUtc: string;
  onJumpAudit: (id: string) => void;
}) {
  const related = useQuery({
    queryKey: ["audit", "by-correlation", correlationId],
    queryFn: ({ signal }) => getAuditsByCorrelation(correlationId, {}, signal),
    enabled: !!correlationId,
    staleTime: 30_000,
  });

  // Newest → oldest, capped at 12 to keep the timeline bounded for
  // chatty correlations. Memoised against the underlying response so a
  // re-render doesn't re-sort.
  const sorted = useMemo(() => {
    const items = related.data ?? [];
    return [...items]
      .sort((a, b) => Date.parse(b.occurredAtUtc) - Date.parse(a.occurredAtUtc))
      .slice(0, 12);
  }, [related.data]);
  const others = sorted.filter((r) => r.id !== currentId);
  const currentMs = Date.parse(currentOccurredAtUtc);

  return (
    <section>
      <div className="flex items-baseline justify-between">
        <SectionLabel>Related events</SectionLabel>
        {!related.isLoading && (
          <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {sorted.length} on this correlation
          </span>
        )}
      </div>

      {related.isLoading ? (
        <div className="mt-2 space-y-2">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-10 w-full rounded-md" />
          ))}
        </div>
      ) : others.length === 0 ? (
        <p className="mt-2 text-[11.5px] text-[var(--color-muted-foreground)]">
          No other events share this correlation. The full lifecycle of this
          request is contained in the payload above.
        </p>
      ) : (
        <ol className="mt-2 relative pl-4">
          {/* Vertical rail — fades top + bottom so it reads as a slice
              of a longer timeline rather than a hard-bounded list. */}
          <span
            aria-hidden
            className="absolute left-1.5 top-1 bottom-1 w-px bg-gradient-to-b from-transparent via-[var(--color-border)] to-transparent"
          />
          {sorted.map((row) => {
            const isCurrent = row.id === currentId;
            const tone = severityColorVar(row.severity);
            const RowIcon = eventTypeIcon(row.eventType);
            const deltaSec = Math.round((Date.parse(row.occurredAtUtc) - currentMs) / 1000);
            const deltaLabel =
              isCurrent
                ? "this event"
                : deltaSec === 0
                  ? "0s"
                  : deltaSec > 0
                    ? `+${deltaSec}s`
                    : `${deltaSec}s`;
            return (
              <li key={row.id} className="relative pl-4 pb-2 last:pb-0">
                {/* Node dot */}
                <span
                  aria-hidden
                  className={cn(
                    "absolute -left-0 top-2 h-2.5 w-2.5 rounded-full ring-2 ring-[var(--color-card)]",
                    isCurrent && "shadow-[0_0_0_3px_oklch(from_var(--color-primary)_l_c_h_/_0.18)]",
                  )}
                  style={{ background: isCurrent ? "var(--color-primary)" : tone }}
                />
                <button
                  type="button"
                  onClick={() => !isCurrent && onJumpAudit(row.id)}
                  disabled={isCurrent}
                  className={cn(
                    "group/related flex w-full items-center gap-3 rounded-md px-2 py-1.5 text-left transition-colors",
                    isCurrent
                      ? "bg-[var(--color-primary-soft)] cursor-default"
                      : "hover:bg-[var(--color-accent)] cursor-pointer",
                  )}
                >
                  <RowIcon className="h-3.5 w-3.5 shrink-0" style={{ color: tone }} aria-hidden />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-baseline gap-2">
                      <span className={cn("truncate text-[12px] font-medium tracking-tight", isCurrent && "text-[var(--color-primary)]")}>
                        {row.source ?? AUDIT_EVENT_TYPE_LABELS[row.eventType]}
                      </span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
                        {AUDIT_SEVERITY_LABELS[row.severity]}
                      </span>
                    </span>
                    <span className="font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
                      {fmtIsoDense(row.occurredAtUtc).time} · {deltaLabel}
                    </span>
                  </span>
                  {!isCurrent && (
                    <ChevronRight className="h-3 w-3 text-[var(--color-muted-foreground)] transition-transform group-hover/related:translate-x-0.5" />
                  )}
                </button>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}

function CopyButton({ value }: { value: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      className="font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      onClick={async () => {
        try {
          await navigator.clipboard.writeText(value);
          setCopied(true);
          window.setTimeout(() => setCopied(false), 1200);
        } catch {
          /* clipboard unavailable */
        }
      }}
    >
      {copied ? "Copied" : "Copy"}
    </button>
  );
}
