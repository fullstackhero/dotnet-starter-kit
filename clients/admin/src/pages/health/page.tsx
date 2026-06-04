import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Activity, ChevronRight, Heart, RefreshCw } from "lucide-react";
import { getLiveness, getReadiness, type HealthEntry, type HealthResult, type HealthStatus } from "@/api/health";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { EntityPageHeader, ErrorBand, SettingsSection, StatStrip, Stat } from "@/components/list";
import { cn } from "@/lib/cn";

const REFRESH_INTERVAL_MS = 10_000;

export function HealthPage() {
  const live = useQuery({
    queryKey: ["health", "live"],
    queryFn: getLiveness,
    refetchInterval: REFRESH_INTERVAL_MS,
    refetchOnWindowFocus: false,
    retry: 1,
  });

  const ready = useQuery({
    queryKey: ["health", "ready"],
    queryFn: getReadiness,
    refetchInterval: REFRESH_INTERVAL_MS,
    refetchOnWindowFocus: false,
    retry: 1,
  });

  const isLoading = live.isLoading || ready.isLoading;
  const isFetching = live.isFetching || ready.isFetching;

  const liveStatus = (live.data?.status as HealthStatus | undefined) ?? "Unknown";
  const readyStatus = (ready.data?.status as HealthStatus | undefined) ?? "Unknown";

  const readyEntries = ready.data?.results ?? [];
  const checksHealthy = readyEntries.filter((e) => e.status === "Healthy").length;
  const checksDegraded = readyEntries.filter((e) => e.status === "Degraded").length;
  const checksFailing = readyEntries.filter((e) => e.status !== "Healthy" && e.status !== "Degraded").length;

  const refetchAll = () => {
    void live.refetch();
    void ready.refetch();
  };

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={Heart}
        tone="success"
        title="Health"
        description={
          <>
            Live process and dependency probes. Auto-refreshes every {REFRESH_INTERVAL_MS / 1000} seconds.
            The probe endpoints themselves (<code className="code-chip">/health/live</code>{" "}
            <code className="code-chip">/health/ready</code>) are unauthenticated so they can be
            scraped by load balancers and uptime monitors.
          </>
        }
      >
        <Button variant="outline" size="sm" disabled={isFetching} onClick={refetchAll} className="flex-1 sm:flex-none">
          <RefreshCw className={cn("mr-1.5 h-3.5 w-3.5", isFetching && "animate-spin")} />
          Refresh
        </Button>
      </EntityPageHeader>

      <StatStrip cols={4}>
        <Stat
          label="Liveness"
          value={<StatusGlyph status={liveStatus} />}
          hint="API process responding"
          tone={statusToTone(liveStatus)}
        />
        <Stat
          label="Readiness"
          value={<StatusGlyph status={readyStatus} />}
          hint="Dependencies reachable"
          tone={statusToTone(readyStatus)}
        />
        <Stat
          label="Checks healthy"
          value={isLoading ? "—" : checksHealthy.toString()}
          hint={`of ${readyEntries.length || "—"} registered`}
          tone={checksHealthy > 0 ? "success" : "default"}
        />
        <Stat
          label="Checks failing"
          value={isLoading ? "—" : (checksFailing + checksDegraded).toString()}
          hint={
            checksDegraded > 0
              ? `${checksDegraded} degraded · ${checksFailing} unhealthy`
              : `${checksFailing} unhealthy`
          }
          tone={checksFailing > 0 ? "danger" : checksDegraded > 0 ? "warning" : "default"}
        />
      </StatStrip>

      {live.isError && (
        <ErrorBand
          kind="liveness"
          message={live.error instanceof Error ? live.error.message : "Liveness probe failed."}
        />
      )}
      {ready.isError && (
        <ErrorBand
          kind="readiness"
          message={ready.error instanceof Error ? ready.error.message : "Readiness probe failed."}
        />
      )}

      <ProbeSection
        title="Liveness"
        path="/health/live"
        result={live.data}
        loading={live.isLoading}
        description="Process is up and serving requests. No external dependencies are checked."
      />

      <ProbeSection
        title="Readiness"
        path="/health/ready"
        result={ready.data}
        loading={ready.isLoading}
        description="All registered dependency checks must report Healthy for the probe to return 200. A 503 here flips the API out of the load-balancer pool."
      />
    </div>
  );
}

// ─── subcomponents ──────────────────────────────────────────────────────

function ProbeSection({
  title,
  path,
  result,
  loading,
  description,
}: {
  title: string;
  path: string;
  result: HealthResult | undefined;
  loading: boolean;
  description: string;
}) {
  return (
    <SettingsSection
      title={title}
      description={description}
      footer={
        <div className="flex items-center justify-between">
          {result && <StatusBadge status={result.status} />}
          <code className="code-chip ml-auto">{path}</code>
        </div>
      }
    >
      {loading ? (
        <div className="py-6 text-sm text-[var(--color-muted-foreground)]">Probing…</div>
      ) : !result || result.results.length === 0 ? (
        <div className="flex items-center gap-3 py-5">
          <Activity className="h-4 w-4 text-[var(--color-muted-foreground)]" />
          <div>
            <div className="text-sm font-medium">No dependency checks reported.</div>
            <div className="text-xs text-[var(--color-muted-foreground)]">
              The probe responded with status <code className="code-chip">{result?.status ?? "—"}</code>.
            </div>
          </div>
        </div>
      ) : (
        <ul className="-mx-5 divide-y divide-[var(--color-border)] border-t border-[var(--color-border)]">
          {result.results.map((entry) => (
            <CheckRow key={entry.name} entry={entry} />
          ))}
        </ul>
      )}
    </SettingsSection>
  );
}

function CheckRow({ entry }: { entry: HealthEntry }) {
  const [open, setOpen] = useState(false);
  const hasDetails = !!entry.details && Object.keys(entry.details).length > 0;

  const rowInner = (
    <>
      <StatusDot status={entry.status} />
      <div className="min-w-0">
        <div className="truncate font-mono text-[13px] font-medium">{entry.name}</div>
        {entry.description && (
          <div className="mt-0.5 truncate text-xs text-[var(--color-muted-foreground)]">
            {entry.description}
          </div>
        )}
      </div>
      <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
        {entry.durationMs.toFixed(1)}ms
      </span>
      {hasDetails ? (
        <ChevronRight
          className={cn(
            "h-4 w-4 text-[var(--color-muted-foreground)] transition-transform",
            open && "rotate-90",
          )}
        />
      ) : (
        <span className="h-4 w-4" aria-hidden />
      )}
    </>
  );

  return (
    <li>
      {hasDetails ? (
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          aria-expanded={open}
          className="grid w-full grid-cols-[auto_1fr_auto_auto] items-center gap-4 px-5 py-3.5 text-left transition-colors hover:bg-[var(--color-muted)]/50 focus-visible:bg-[var(--color-muted)]/50 focus-visible:outline-none"
        >
          {rowInner}
        </button>
      ) : (
        <div className="grid grid-cols-[auto_1fr_auto_auto] items-center gap-4 px-5 py-3.5">
          {rowInner}
        </div>
      )}
      {hasDetails && open && (
        <div className="border-t border-[var(--color-border)] bg-[var(--color-surface-2)] px-5 py-3">
          <dl className="grid grid-cols-1 gap-x-6 gap-y-1.5 sm:grid-cols-2">
            {Object.entries(entry.details ?? {}).map(([k, v]) => (
              <div key={k} className="flex items-baseline justify-between gap-3 border-b border-dashed border-[var(--color-border)] py-1.5">
                <dt className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                  {k}
                </dt>
                <dd className="truncate font-mono text-[12px] text-[var(--color-foreground)]">
                  {String(v)}
                </dd>
              </div>
            ))}
          </dl>
        </div>
      )}
    </li>
  );
}

function StatusBadge({ status }: { status: HealthStatus }) {
  const variant =
    status === "Healthy" ? "success" : status === "Degraded" ? "warning" : "danger";
  return <Badge variant={variant}>{status}</Badge>;
}

function StatusDot({ status }: { status: HealthStatus }) {
  const tone = statusToColor(status);
  return (
    <span
      aria-hidden
      title={status}
      className={cn("h-2 w-2 rounded-full", tone)}
    />
  );
}

function StatusGlyph({ status }: { status: HealthStatus }) {
  if (status === "Healthy") {
    return (
      <span className="inline-flex items-center gap-2">
        <span className="pulse-dot" aria-hidden />
        <span>Healthy</span>
      </span>
    );
  }
  return <span>{status}</span>;
}

function statusToTone(s: HealthStatus): "default" | "success" | "warning" | "danger" {
  if (s === "Healthy") return "success";
  if (s === "Degraded") return "warning";
  if (s === "Unknown") return "default";
  return "danger";
}

function statusToColor(s: HealthStatus): string {
  if (s === "Healthy") return "bg-[var(--color-success)]";
  if (s === "Degraded") return "bg-[var(--color-warning)]";
  return "bg-[var(--color-destructive)]";
}
