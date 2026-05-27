import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  CircleAlert,
  Database,
  Flame,
  Globe,
  HardDrive,
  Pause,
  Play,
  RefreshCw,
  ShieldCheck,
  Timer,
  Zap,
} from "lucide-react";
import { getReadiness, type HealthEntry, type HealthSnapshot, type HealthStatus } from "@/api/health";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  EntityStatusBadge,
  PageHero,
  ToneIconTile,
  type EntityStatusTone,
  type ToneIconTileTone,
} from "@/components/list";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// Tone helpers — keep status-to-aesthetic mapping in one place so the
// hero, status pips, and per-check rows stay in lockstep. Treat "Healthy"
// as the green path; everything else gets warning or destructive tone.
// ────────────────────────────────────────────────────────────────────────

type Tone = "success" | "warning" | "danger";

function toneFor(status: HealthStatus): Tone {
  if (status === "Healthy") return "success";
  if (status === "Degraded") return "warning";
  return "danger";
}

const TONE_VAR: Record<Tone, string> = {
  success: "var(--color-success)",
  warning: "var(--color-warning)",
  danger: "var(--color-destructive)",
};

// Map this page's internal Tone to the shared design-system tone names.
// `success` and `warning` are identical; `danger` becomes `destructive`
// for tile tinting and `danger` for the status pill (the names diverged
// historically between the icon-tile and the pill primitives).
const TILE_TONE: Record<Tone, ToneIconTileTone> = {
  success: "success",
  warning: "warning",
  danger: "destructive",
};
const PILL_TONE: Record<Tone, EntityStatusTone> = {
  success: "success",
  warning: "warning",
  danger: "danger",
};

const HERO_COPY: Record<HealthStatus, { headline: string; subline: string }> = {
  Healthy: {
    headline: "All systems operational",
    subline: "Every dependency is responding within tolerance.",
  },
  Degraded: {
    headline: "Partial degradation",
    subline: "One or more checks are reporting elevated latency or warnings.",
  },
  Unhealthy: {
    headline: "Disruption detected",
    subline: "At least one critical dependency is unreachable. Investigate the failing checks below.",
  },
};

// ────────────────────────────────────────────────────────────────────────
// Per-check icon registry — keyed on the well-known names registered
// from BuildingBlocks/Web and the modules. Falls back to a generic glyph
// for unknown names so new modules render cleanly without code changes.
// ────────────────────────────────────────────────────────────────────────

function iconForCheck(name: string): React.ComponentType<{ className?: string }> {
  const k = name.toLowerCase();
  if (k === "self") return ShieldCheck;
  if (k === "redis") return Zap;
  if (k === "hangfire") return Timer;
  if (k.includes("postgres") || k.includes("db") || k.endsWith("-db")) return Database;
  if (k.includes("storage") || k.includes("disk")) return HardDrive;
  if (k.includes("http") || k.includes("api") || k.includes("webhook")) return Globe;
  return Flame;
}

// ────────────────────────────────────────────────────────────────────────
// Polling history — lightweight client-side ring buffer. No backend
// persistence; resets on reload. Gives the operator a sense of "has it
// been flaky for the last minute, or did it just go red?".
// ────────────────────────────────────────────────────────────────────────

type Tick = { status: HealthStatus; at: number };
const HISTORY_LIMIT = 24;

function useHistory(snapshot: HealthSnapshot | undefined): Tick[] {
  const [ticks, setTicks] = useState<Tick[]>([]);
  const lastSeenRef = useRef<string | null>(null);
  useEffect(() => {
    if (!snapshot) return;
    if (snapshot.fetchedAt === lastSeenRef.current) return;
    lastSeenRef.current = snapshot.fetchedAt;
    setTicks((prev) =>
      [...prev, { status: snapshot.status, at: Date.parse(snapshot.fetchedAt) }].slice(-HISTORY_LIMIT),
    );
  }, [snapshot]);
  return ticks;
}

// ────────────────────────────────────────────────────────────────────────
// Number formatting — tight, locale-aware. Always show 1 decimal for
// sub-second values, integer ms otherwise. Keeps the column alignment
// sane in tabular-nums.
// ────────────────────────────────────────────────────────────────────────

function formatLatency(ms: number): string {
  if (ms < 10) return `${ms.toFixed(1)} ms`;
  if (ms < 1000) return `${ms.toFixed(0)} ms`;
  return `${(ms / 1000).toFixed(2)} s`;
}

function splitLatency(ms: number): [string, string] {
  if (ms < 10) return [ms.toFixed(1), "ms"];
  if (ms < 1000) return [ms.toFixed(0), "ms"];
  return [(ms / 1000).toFixed(2), "s"];
}

function formatRelative(iso: string, now: number = Date.now()): string {
  const delta = Math.max(0, Math.floor((now - Date.parse(iso)) / 1000));
  if (delta < 5) return "just now";
  if (delta < 60) return `${delta}s ago`;
  const m = Math.floor(delta / 60);
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  return `${h}h ago`;
}

// ────────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────────

const POLL_INTERVAL_MS = 10_000;

export function HealthPage() {
  const [autoRefresh, setAutoRefresh] = useState(true);

  const query = useQuery({
    queryKey: ["health", "ready"],
    queryFn: ({ signal }) => getReadiness(signal),
    refetchInterval: autoRefresh ? POLL_INTERVAL_MS : false,
    refetchOnWindowFocus: false,
    staleTime: 0,
    retry: 1,
  });

  const snapshot = query.data;
  const history = useHistory(snapshot);

  // Force re-render every 5s so the "x seconds ago" label stays fresh
  // without re-fetching the report.
  const [, force] = useState(0);
  useEffect(() => {
    const id = window.setInterval(() => force((n) => n + 1), 5_000);
    return () => window.clearInterval(id);
  }, []);

  const computed = useMemo(() => {
    if (!snapshot) return null;
    const total = snapshot.results.length;
    const failing = snapshot.results.filter((r) => r.status !== "Healthy").length;
    const slowest = snapshot.results.reduce<HealthEntry | undefined>(
      (acc, r) => (acc && acc.durationMs > r.durationMs ? acc : r),
      undefined,
    );
    const totalDuration = snapshot.results.reduce((s, r) => s + r.durationMs, 0);
    return { total, failing, slowest, totalDuration };
  }, [snapshot]);

  return (
    <div className="space-y-7 pb-12">
      <PageHero
        eyebrow="System · Health"
        title="Health"
        subtitle={
          <>
            Live readiness probe across every registered dependency. Polled every{" "}
            <span className="font-mono">{POLL_INTERVAL_MS / 1000}s</span>.
          </>
        }
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setAutoRefresh((v) => !v)}
              title={autoRefresh ? "Pause auto-refresh" : "Resume auto-refresh"}
            >
              {autoRefresh ? (
                <Pause className="mr-1.5 h-3.5 w-3.5" />
              ) : (
                <Play className="mr-1.5 h-3.5 w-3.5" />
              )}
              {autoRefresh ? "Live" : "Paused"}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={query.isFetching}
              onClick={() => void query.refetch()}
            >
              <RefreshCw
                className={cn("mr-1.5 h-3.5 w-3.5", query.isFetching && "animate-spin")}
              />
              Refresh
            </Button>
          </>
        }
      />

      <section className="fsh-enter fsh-enter-2">
        {snapshot ? (
          <HeroPanel
            snapshot={snapshot}
            history={history}
            slowestMs={computed?.slowest?.durationMs ?? 0}
            failing={computed?.failing ?? 0}
            total={computed?.total ?? 0}
          />
        ) : query.isError ? (
          <ErrorPanel message={(query.error as Error)?.message} />
        ) : (
          <HeroSkeleton />
        )}
      </section>

      {/* ── Dependencies list ───────────────────────────────────────────── */}
      <section aria-label="Dependencies" className="fsh-enter fsh-enter-3">
        <div className="mb-3 flex items-baseline justify-between gap-3">
          <h2 className="font-display text-[16px] font-semibold tracking-tight text-[var(--color-foreground)]">
            Dependencies
          </h2>
          {snapshot && (
            <span className="text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
              {computed?.total ?? 0} checks · total{" "}
              <span className="font-mono tabular-nums text-[var(--color-foreground)]">
                {formatLatency(computed?.totalDuration ?? 0)}
              </span>
            </span>
          )}
        </div>

        {snapshot ? (
          snapshot.results.length === 0 ? (
            <EmptyChecks />
          ) : (
            <DependencyList entries={snapshot.results} />
          )
        ) : (
          <ChecksSkeleton />
        )}
      </section>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Hero panel — calm dentalOS surface. Outfit headline carries the
// overall status; a vitals row sits beneath it with stat values in mono
// tabular numerals; a slim pip-bar history ribbon docks on the right
// (lg+) or under the vitals row (sm). Background is a single soft tone-
// tinted radial in the upper-left, low opacity, no second channel.
// ────────────────────────────────────────────────────────────────────────

function HeroPanel({
  snapshot,
  history,
  slowestMs,
  failing,
  total,
}: {
  snapshot: HealthSnapshot;
  history: Tick[];
  slowestMs: number;
  failing: number;
  total: number;
}) {
  const tone = toneFor(snapshot.status);
  const copy = HERO_COPY[snapshot.status];
  const Icon =
    snapshot.status === "Healthy"
      ? CheckCircle2
      : snapshot.status === "Degraded"
        ? AlertTriangle
        : CircleAlert;
  const toneColor = TONE_VAR[tone];

  return (
    <div className="relative overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
      {/* Atmospheric tint — a single soft glow in the upper-left, color
          follows status. Calmer than the previous two-channel hero. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: `radial-gradient(80% 60% at -5% -15%, oklch(from ${toneColor} l c h / 0.10), transparent 60%)`,
        }}
      />

      <div className="relative grid gap-7 px-7 py-7 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
        <div>
          {/* Status meta strip — tile + status word + HTTP code */}
          <div className="flex items-center gap-3">
            <ToneIconTile icon={Icon} tone={TILE_TONE[tone]} size="lg" />
            <div className="flex items-baseline gap-2">
              <EntityStatusBadge tone={PILL_TONE[tone]} withDot>
                {snapshot.status}
              </EntityStatusBadge>
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
                HTTP {snapshot.httpStatus}
              </span>
            </div>
          </div>

          {/* Outfit display headline */}
          <h2 className="mt-4 font-display text-[26px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)]">
            {copy.headline}
          </h2>
          <p className="mt-1.5 max-w-[52ch] text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            {copy.subline}
          </p>

          {/* Vitals row */}
          <dl className="mt-6 grid grid-cols-2 gap-x-8 gap-y-4 sm:grid-cols-4">
            <Vital label="Checks" value={`${total - failing}/${total}`} hint="passing" />
            <Vital
              label="Round-trip"
              value={formatLatency(snapshot.roundTripMs)}
              hint="end-to-end"
            />
            <Vital label="Slowest" value={formatLatency(slowestMs)} hint="single check" />
            <Vital
              label="Last poll"
              value={formatRelative(snapshot.fetchedAt)}
              hint={new Date(snapshot.fetchedAt).toLocaleTimeString("en-US", {
                hour12: false,
              })}
            />
          </dl>
        </div>

        {/* History ribbon — last N polls as a tight pip-bar strip.
            Vertical on lg+, horizontal on smaller widths. */}
        <div className="lg:w-[176px]">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              Recent polls
            </span>
            <Activity className="size-3 text-[var(--color-muted-foreground)]" aria-hidden />
          </div>
          <HistoryPips ticks={history} />
          <p className="mt-2 font-mono text-[10.5px] tabular-nums text-[var(--color-muted-foreground)]">
            {history.length} / {HISTORY_LIMIT} this session
          </p>
        </div>
      </div>
    </div>
  );
}

function Vital({
  label,
  value,
  hint,
}: {
  label: string;
  value: React.ReactNode;
  hint?: React.ReactNode;
}) {
  return (
    <div>
      <dt className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd className="mt-1 font-mono text-[15px] font-semibold tabular-nums tracking-tight text-[var(--color-foreground)]">
        {value}
      </dd>
      {hint && (
        <div className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]">
          {hint}
        </div>
      )}
    </div>
  );
}

/**
 * HistoryPips — tight pip-bar visualisation. One pip per recorded poll,
 * tone-tinted by that poll's status. Empty slots render at the front of
 * the strip so the ribbon has consistent width even on first paint.
 */
function HistoryPips({ ticks }: { ticks: Tick[] }) {
  const filled: Array<Tick | null> = [
    ...Array(HISTORY_LIMIT - ticks.length).fill(null),
    ...ticks,
  ];
  return (
    <div className="flex gap-[3px]" role="img" aria-label="Recent poll history">
      {filled.map((tick, i) => (
        <span
          key={i}
          className="h-6 flex-1 rounded-[2px] transition-colors"
          title={
            tick
              ? `${tick.status} · ${new Date(tick.at).toLocaleTimeString()}`
              : "no data"
          }
          style={{
            backgroundColor: tick
              ? TONE_VAR[toneFor(tick.status)]
              : "var(--color-border-strong)",
            opacity: tick ? (tick.status === "Healthy" ? 0.85 : 1) : 0.35,
          }}
        />
      ))}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Dependencies — one card with collapsible rows. Each row is a horizontal
// scan-line that opens to reveal details on click. Built in the same row-
// based vocabulary as the new permissions editor so the two feel like
// siblings: status pip + icon + name (Outfit) + latency (mono tabular)
// + chevron, with hover/expanded states tinted by the row's status.
// ────────────────────────────────────────────────────────────────────────

function DependencyList({ entries }: { entries: HealthEntry[] }) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const toggle = (name: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name);
      else next.add(name);
      return next;
    });
  };

  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
      <div className="divide-y divide-[var(--color-border)]">
        {entries.map((entry) => (
          <DependencyRow
            key={entry.name}
            entry={entry}
            isExpanded={expanded.has(entry.name)}
            onToggle={() => toggle(entry.name)}
          />
        ))}
      </div>
    </div>
  );
}

function DependencyRow({
  entry,
  isExpanded,
  onToggle,
}: {
  entry: HealthEntry;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  const tone = toneFor(entry.status);
  const Icon = iconForCheck(entry.name);
  const toneColor = TONE_VAR[tone];
  const [num, unit] = splitLatency(entry.durationMs);

  // Same sqrt-scaled fill against a 500ms soft budget the rack-unit gauge
  // used — keeps the visual feel of "this check is in/out of budget".
  const pct = Math.min(100, Math.sqrt(Math.max(0, entry.durationMs) / 500) * 100);

  const detailEntries = entry.details
    ? Object.entries(entry.details).filter(([k]) => k !== "tag")
    : [];

  return (
    <div>
      <div
        role="button"
        tabIndex={0}
        aria-expanded={isExpanded}
        onClick={onToggle}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onToggle();
          }
        }}
        className={cn(
          "group/depr flex w-full cursor-pointer items-center gap-4 px-5 py-3.5 text-left",
          "transition-colors duration-[var(--duration-fast)]",
          isExpanded
            ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.03)]"
            : "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.025)]",
        )}
      >
        {/* Status pip — calm, no pulse */}
        <span
          aria-hidden
          className="grid size-2 shrink-0 rounded-full ring-2"
          style={{
            backgroundColor: toneColor,
            boxShadow: `0 0 0 3px oklch(from ${toneColor} l c h / 0.18)`,
          }}
        />

        {/* Icon tile */}
        <span
          aria-hidden
          className="grid size-8 shrink-0 place-items-center rounded-lg"
          style={{
            backgroundColor: `oklch(from ${toneColor} l c h / 0.08)`,
            color: toneColor,
            boxShadow: `inset 0 0 0 1px oklch(from ${toneColor} l c h / 0.18)`,
          }}
        >
          <Icon className="size-3.5" />
        </span>

        {/* Name + description */}
        <div className="min-w-0 flex-1">
          <h3
            className="truncate font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]"
            title={entry.name}
          >
            {entry.name}
          </h3>
          {entry.description ? (
            <p className="mt-0.5 truncate text-[11.5px] text-[var(--color-muted-foreground)]">
              {entry.description}
            </p>
          ) : (
            <p className="mt-0.5 truncate text-[11.5px] italic text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
              No description registered.
            </p>
          )}
        </div>

        {/* Latency readout */}
        <div className="flex shrink-0 items-baseline gap-1">
          <span className="font-mono text-[15px] font-semibold tabular-nums text-[var(--color-foreground)]">
            {num}
          </span>
          <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {unit}
          </span>
        </div>

        {/* Status word — small caps */}
        <span
          className="hidden shrink-0 text-[10.5px] font-semibold uppercase tracking-wider sm:inline"
          style={{ color: toneColor }}
        >
          {entry.status}
        </span>

        {/* Chevron */}
        <ChevronDown
          aria-hidden
          className={cn(
            "size-4 shrink-0 text-[var(--color-muted-foreground)] transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
            isExpanded && "rotate-180",
          )}
        />
      </div>

      {isExpanded && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] bg-[oklch(from_var(--color-secondary)_l_c_h_/_0.4)] px-5 py-5">
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:items-start">
            {/* Latency budget */}
            <div>
              <div className="flex items-baseline justify-between gap-3">
                <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  Latency budget
                </span>
                <span className="font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                  {formatLatency(entry.durationMs)} / 500 ms
                </span>
              </div>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-[var(--color-border-strong)]">
                <div
                  className="h-full rounded-full transition-[width]"
                  style={{
                    width: `${pct}%`,
                    backgroundColor: toneColor,
                  }}
                />
              </div>
              <p className="mt-2 text-[11px] leading-relaxed text-[var(--color-muted-foreground)]">
                Scaled against a soft 500 ms readiness budget. Bars in the upper
                third are early-warning territory.
              </p>
            </div>

            {/* Detail key/value table */}
            <div>
              <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Detail
              </span>
              {detailEntries.length === 0 ? (
                <p className="mt-2 text-[12px] italic text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
                  No additional details reported.
                </p>
              ) : (
                <dl className="mt-2 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)] rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]">
                  {detailEntries.slice(0, 6).map(([k, v]) => (
                    <div
                      key={k}
                      className="grid grid-cols-[max-content_minmax(0,1fr)] items-baseline gap-3 px-3 py-2"
                    >
                      <dt className="font-mono text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                        {k}
                      </dt>
                      <dd
                        className="truncate text-right font-mono text-[12px] tabular-nums text-[var(--color-foreground)]"
                        title={String(v)}
                      >
                        {String(v)}
                      </dd>
                    </div>
                  ))}
                  {detailEntries.length > 6 && (
                    <div className="px-3 py-1.5 text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                      +{detailEntries.length - 6} more
                    </div>
                  )}
                </dl>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Skeletons + error / empty
// ────────────────────────────────────────────────────────────────────────

function HeroSkeleton() {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-7 py-7">
      <div className="grid gap-7 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <Skeleton className="size-10 rounded-lg" />
            <Skeleton className="h-5 w-24 rounded-full" />
          </div>
          <Skeleton className="h-7 w-80" />
          <Skeleton className="h-4 w-96 max-w-full" />
          <div className="grid grid-cols-2 gap-x-8 gap-y-4 pt-3 sm:grid-cols-4">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="space-y-1.5">
                <Skeleton className="h-3 w-14" />
                <Skeleton className="h-5 w-20" />
                <Skeleton className="h-3 w-12" />
              </div>
            ))}
          </div>
        </div>
        <Skeleton className="h-6 w-[176px]" />
      </div>
    </div>
  );
}

function ChecksSkeleton() {
  return (
    <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
      <div className="divide-y divide-[var(--color-border)]">
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="flex items-center gap-4 px-5 py-3.5">
            <Skeleton className="size-2 rounded-full" />
            <Skeleton className="size-8 rounded-lg" />
            <div className="flex-1 space-y-1.5">
              <Skeleton className="h-3.5 w-32" />
              <Skeleton className="h-3 w-56 max-w-full" />
            </div>
            <Skeleton className="h-5 w-16" />
            <Skeleton className="size-4 rounded" />
          </div>
        ))}
      </div>
    </div>
  );
}

function ErrorPanel({ message }: { message?: string }) {
  return (
    <div
      role="alert"
      className={cn(
        "fsh-enter flex items-start gap-3 rounded-xl border px-5 py-4",
        "border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)]",
        "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.04)]",
      )}
    >
      <ToneIconTile icon={CircleAlert} tone="destructive" size="md" />
      <div className="min-w-0 flex-1">
        <p className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-destructive)]">
          Health endpoint unreachable
        </p>
        <p className="mt-1 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          {message ??
            "The browser couldn't reach /health/ready. The API may be down, or a network policy is blocking the request."}
        </p>
      </div>
    </div>
  );
}

function EmptyChecks() {
  return (
    <div className="flex flex-col items-center justify-center gap-2.5 rounded-xl border border-dashed border-[var(--color-border)] bg-[var(--color-card)] px-5 py-12 text-center">
      <ToneIconTile icon={ShieldCheck} tone="muted" size="lg" className="rounded-full" />
      <p className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
        No checks registered
      </p>
      <p className="max-w-sm text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
        Modules can register dependency checks via{" "}
        <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] text-[var(--color-foreground)]">
          AddHealthChecks()
        </code>
        . None are reporting yet.
      </p>
    </div>
  );
}
