import { useMemo, useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronRight, RefreshCw, ScrollText, X } from "lucide-react";
import {
  AUDIT_EVENT_TYPES,
  AUDIT_SEVERITIES,
  listAudits,
  getAuditSummary,
  type AuditEventType,
  type AuditSeverity,
  type AuditSummaryDto,
} from "@/api/audits";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  EntityPageHeader,
  ErrorBand,
  Pagination,
  StatStrip,
  Stat,
  Select,
  FilterBar,
  LoadingRow,
} from "@/components/list";
import { EmptyState } from "@/components/empty-state";
import { ApiRequestError } from "@/lib/api-client";
import { formatNumber } from "@/lib/format";
import { AuditingPermissions } from "@/lib/permissions";
import { AuditDetailSheet } from "@/pages/audits/detail";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 25;

// Track the search box in a local debounced state so we don't fire a query
// per keystroke — debounce to ~250ms before pushing to the URL.
const SEARCH_DEBOUNCE_MS = 250;

export function AuditsListPage() {
  const { t } = useTranslation("audits");
  const { user } = useAuth();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [params, setParams] = useSearchParams();

  const canCrossTenant = (user?.permissions ?? []).includes(
    AuditingPermissions.AuditTrails.ViewCrossTenant,
  );

  const pageNumber = Number(params.get("page") ?? "1") || 1;
  const eventType = (params.get("type") as AuditEventType | null) ?? "";
  const severity = (params.get("sev") as AuditSeverity | null) ?? "";
  const tenantId = params.get("tenant") ?? "";
  const correlationId = params.get("corr") ?? "";
  const search = params.get("q") ?? "";

  // Local search box state + debounced sync.
  const [searchInput, setSearchInput] = useState(search);
  useEffect(() => setSearchInput(search), [search]);
  useEffect(() => {
    const handle = setTimeout(() => {
      if (searchInput === search) return;
      const next = new URLSearchParams(params);
      if (searchInput.trim()) next.set("q", searchInput.trim());
      else next.delete("q");
      next.set("page", "1");
      setParams(next, { replace: true });
    }, SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchInput]);

  const query = useQuery({
    queryKey: ["audits", { pageNumber, eventType, severity, tenantId, correlationId, search }],
    queryFn: () =>
      listAudits({
        pageNumber,
        pageSize: PAGE_SIZE,
        eventType: eventType || undefined,
        severity: severity || undefined,
        tenantId: tenantId || undefined,
        correlationId: correlationId || undefined,
        search: search || undefined,
        sort: "-OccurredAtUtc",
      }),
    placeholderData: keepPreviousData,
  });

  const summary = useQuery({
    queryKey: ["audits", "summary", { tenantId }],
    queryFn: () => getAuditSummary({ tenantId: tenantId || undefined }),
  });

  const data = query.data;
  const items: AuditSummaryDto[] = data?.items ?? [];

  const summaryStats = useMemo(() => {
    const s = summary.data;
    if (!s) return { total: 0, errors: 0, security: 0, exceptions: 0 };
    const total = Object.values(s.eventsByType).reduce((a, b) => a + (b ?? 0), 0);
    const errors = (s.eventsBySeverity.Error ?? 0) + (s.eventsBySeverity.Critical ?? 0);
    const security = s.eventsByType.Security ?? 0;
    const exceptions = s.eventsByType.Exception ?? 0;
    return { total, errors, security, exceptions };
  }, [summary.data]);

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value && value.length > 0) next.set(key, value);
    else next.delete(key);
    next.set("page", "1");
    setParams(next, { replace: true });
  };

  const setPage = (n: number) => {
    const next = new URLSearchParams(params);
    next.set("page", String(n));
    setParams(next, { replace: true });
  };

  const clearAll = () => {
    setParams(new URLSearchParams(), { replace: true });
    setSearchInput("");
  };

  const activeFilters = [eventType, severity, tenantId, correlationId, search].filter(Boolean).length;

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={ScrollText}
        title={t("list.title")}
        total={data?.totalCount ?? null}
        unit={t("list.unit")}
        description={t("list.description")}
      >
        <Button
          variant="outline"
          size="sm"
          disabled={query.isFetching}
          onClick={() => query.refetch()}
          className="flex-1 sm:flex-none"
        >
          <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", query.isFetching && "animate-spin")} />
          {t("list.refresh")}
        </Button>
      </EntityPageHeader>

      <StatStrip cols={4}>
        <Stat label={t("list.stat.total")} value={summary.isLoading ? "—" : formatNumber(summaryStats.total)} hint={t("list.stat.totalHint")} />
        <Stat label={t("list.stat.errors")} value={summary.isLoading ? "—" : formatNumber(summaryStats.errors)} hint={t("list.stat.errorsHint")} tone={summaryStats.errors > 0 ? "danger" : "default"} />
        <Stat label={t("list.stat.security")} value={summary.isLoading ? "—" : formatNumber(summaryStats.security)} hint={t("list.stat.securityHint")} tone={summaryStats.security > 0 ? "info" : "default"} />
        <Stat label={t("list.stat.exceptions")} value={summary.isLoading ? "—" : formatNumber(summaryStats.exceptions)} hint={t("list.stat.exceptionsHint")} tone={summaryStats.exceptions > 0 ? "warning" : "default"} />
      </StatStrip>

      <FilterBar
        trailing={
          activeFilters > 0 ? (
            <Button variant="ghost" size="sm" onClick={clearAll} className="text-xs">
              <X className="mr-1 h-3.5 w-3.5" /> {t("list.clearAll", { count: activeFilters })}
            </Button>
          ) : undefined
        }
      >
        <div className="min-w-[16rem] flex-1">
          <Input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={t("list.searchPlaceholder")}
            aria-label={t("list.searchAria")}
            className="h-8"
          />
        </div>
        <Select
          value={eventType}
          onValueChange={(v) => setParam("type", v || null)}
          options={AUDIT_EVENT_TYPES.map((type) => ({ value: type, label: type }))}
          emptyLabel={t("list.allEventTypes")}
          className="min-w-[10rem]"
        />
        <Select
          value={severity}
          onValueChange={(v) => setParam("sev", v || null)}
          options={AUDIT_SEVERITIES.map((s) => ({ value: s, label: s }))}
          emptyLabel={t("list.allSeverities")}
          className="min-w-[10rem]"
        />
        {canCrossTenant && (
          <div className="min-w-[12rem]">
            <Input
              value={tenantId}
              onChange={(e) => setParam("tenant", e.target.value || null)}
              placeholder={t("list.tenantPlaceholder")}
              aria-label={t("list.tenantAria")}
              className="h-8 font-mono text-xs"
            />
          </div>
        )}
        <div className="min-w-[14rem]">
          <Input
            value={correlationId}
            onChange={(e) => setParam("corr", e.target.value || null)}
            placeholder={t("list.correlationPlaceholder")}
            aria-label={t("list.correlationAria")}
            className="h-8 font-mono text-xs"
          />
        </div>
      </FilterBar>

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : t("list.loadError")
          }
        />
      )}

      {query.isLoading && <LoadingRow label={t("list.loading")} />}

      {!query.isLoading && items.length === 0 && !query.isError && (
        <EmptyState
          icon={ScrollText}
          kicker={t("list.empty.kicker")}
          title={t("list.empty.title")}
          description={
            activeFilters > 0
              ? t("list.empty.withFilters")
              : t("list.empty.noData")
          }
          action={
            activeFilters > 0 ? (
              <Button variant="outline" onClick={clearAll}>
                {t("list.empty.clearFilters")}
              </Button>
            ) : undefined
          }
        />
      )}

      {items.length > 0 && (
        <ol className="divide-y divide-[var(--color-border)] border-y border-[var(--color-border)]">
          {items.map((event) => (
            <AuditRow key={event.id} event={event} onClick={() => setSelectedId(event.id)} />
          ))}
        </ol>
      )}

      {data && data.totalPages > 1 && (
        <Pagination
          page={data.pageNumber}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          shown={items.length}
          fetching={query.isFetching}
          hasPrev={data.hasPrevious}
          hasNext={data.hasNext}
          onPrev={() => setPage(Math.max(1, pageNumber - 1))}
          onNext={() => setPage(pageNumber + 1)}
          noun={t("list.noun")}
        />
      )}

      {/* Audit detail side sheet */}
      <AuditDetailSheet auditId={selectedId} onClose={() => setSelectedId(null)} />
    </div>
  );
}

function AuditRow({ event, onClick }: { event: AuditSummaryDto; onClick: () => void }) {
  const { t } = useTranslation("audits");
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className="group grid w-full grid-cols-[auto_8rem_auto_1fr_auto] items-center gap-4 px-1 py-3 text-left transition-colors hover:bg-[var(--color-muted)]/50 focus:outline-none focus-visible:bg-[var(--color-muted)]/50"
      >
        <SeverityDot severity={event.severity} />
        <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {formatTimestamp(event.occurredAtUtc)}
        </span>
        <Badge
          variant={eventTypeVariant(event.eventType)}
          className="justify-self-start font-mono uppercase tracking-[0.14em]"
        >
          {event.eventType}
        </Badge>
        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-2">
            <span className="truncate text-sm font-medium">
              {event.source ?? "—"}
            </span>
            {event.userName && (
              <span className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                · {event.userName}
              </span>
            )}
            {event.tenantId && (
              <code className="code-chip">{event.tenantId}</code>
            )}
          </div>
          {event.correlationId && (
            <div className="mt-0.5 truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
              {t("row.corr", { id: event.correlationId })}
            </div>
          )}
        </div>
        <ChevronRight className="h-4 w-4 text-[var(--color-muted-foreground)] transition-transform group-hover:translate-x-0.5" />
      </button>
    </li>
  );
}

// ─── helpers ────────────────────────────────────────────────────────────

function SeverityDot({ severity }: { severity: AuditSeverity }) {
  const tone =
    severity === "Critical" || severity === "Error"
      ? "bg-[var(--color-destructive)]"
      : severity === "Warning"
        ? "bg-[var(--color-warning)]"
        : severity === "Information"
          ? "bg-[var(--color-info)]"
          : "bg-[var(--color-muted-foreground)]/50";
  return <span aria-hidden title={severity} className={cn("h-2 w-2 rounded-full", tone)} />;
}

function eventTypeVariant(type: AuditEventType) {
  switch (type) {
    case "Security":
      return "info" as const;
    case "Exception":
      return "danger" as const;
    case "EntityChange":
      return "brand" as const;
    case "Activity":
      return "muted" as const;
    default:
      return "outline" as const;
  }
}

function formatTimestamp(value: string): string {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  // Tight ISO-like local format: 04-30 14:22:01
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}
