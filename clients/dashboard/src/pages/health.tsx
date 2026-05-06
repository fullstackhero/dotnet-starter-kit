import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHero } from "@/components/list";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────────
// Tone helpers — keep status-to-aesthetic mapping in one place so the
// hero, badges, and per-check cards stay in lockstep. Treat "Healthy"
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
    <div className="space-y-7">
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

      {/* ── Hero status panel ───────────────────────────────────────────── */}
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

      {/* ── Per-check grid ──────────────────────────────────────────────── */}
      <section
        aria-label="Dependencies"
        className="fsh-enter fsh-enter-3"
      >
        <div className="mb-3 flex items-baseline justify-between">
          <h2 className="text-display text-base font-semibold tracking-tight">
            Dependencies
          </h2>
          {snapshot && (
            <span className="font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
              {computed?.total ?? 0} checks · total{" "}
              <span className="text-[var(--color-foreground)]">
                {formatLatency(computed?.totalDuration ?? 0)}
              </span>
            </span>
          )}
        </div>

        {snapshot ? (
          snapshot.results.length === 0 ? (
            <EmptyChecks />
          ) : (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {snapshot.results.map((entry, idx) => (
                <CheckCard
                  key={entry.name}
                  entry={entry}
                  index={idx}
                  animationDelayMs={60 * idx}
                />
              ))}
            </div>
          )
        ) : (
          <ChecksSkeleton />
        )}
      </section>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Hero panel — the marquee status. A wide card with a saturated gloss in
// the upper-left that's tinted by the current overall status, the big
// headline, a vitals strip, and a slim history ribbon.
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
  const Icon = snapshot.status === "Healthy" ? CheckCircle2 : snapshot.status === "Degraded" ? AlertTriangle : CircleAlert;
  const toneColor = TONE_VAR[tone];

  return (
    <Card className="relative overflow-hidden">
      {/* Atmospheric tint — radial in upper-left, color follows status. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: `radial-gradient(120% 80% at -10% -20%, oklch(from ${toneColor} l c h / 0.18), transparent 55%), radial-gradient(80% 60% at 110% 110%, oklch(from var(--color-primary) l c h / 0.10), transparent 60%)`,
        }}
      />
      {/* Hairline inner ring so the gloss reads as a contained surface. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 rounded-[inherit] ring-1 ring-inset"
        style={{ boxShadow: "inset 0 1px 0 oklch(1 0 0 / 0.06)" }}
      />

      <CardContent className="relative grid grid-cols-1 gap-6 px-6 py-6 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
        <div>
          <div className="flex items-center gap-2.5">
            <span
              aria-hidden
              className="grid h-9 w-9 place-items-center rounded-lg ring-1 ring-inset"
              style={{
                background: `linear-gradient(135deg, oklch(from ${toneColor} l c h / 0.20), oklch(from ${toneColor} l c h / 0.04))`,
                color: toneColor,
                boxShadow: `inset 0 0 0 1px oklch(from ${toneColor} l c h / 0.25)`,
              }}
            >
              <Icon className="h-5 w-5" />
            </span>
            <Badge variant={tone === "success" ? "success" : tone === "warning" ? "warning" : "danger"}>
              {snapshot.status === "Healthy" && (
                <span
                  aria-hidden
                  className="pulse-dot inline-block h-1.5 w-1.5 rounded-full"
                  style={{ backgroundColor: toneColor, color: toneColor }}
                />
              )}
              {snapshot.status}
            </Badge>
            <span
              className="font-mono text-[10.5px] uppercase tracking-[0.12em]"
              style={{ color: "var(--color-muted-foreground)" }}
            >
              HTTP {snapshot.httpStatus}
            </span>
          </div>

          <h2 className="text-display mt-3 text-[26px] font-semibold leading-tight tracking-tight">
            {copy.headline}
          </h2>
          <p className="mt-1.5 max-w-xl text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            {copy.subline}
          </p>

          {/* Vitals strip */}
          <dl className="mt-5 grid grid-cols-2 gap-x-8 gap-y-3 sm:grid-cols-4">
            <Vital label="Checks" value={`${total - failing}/${total}`} hint="passing" />
            <Vital
              label="Round-trip"
              value={formatLatency(snapshot.roundTripMs)}
              hint="end-to-end"
            />
            <Vital
              label="Slowest"
              value={formatLatency(slowestMs)}
              hint="single check"
            />
            <Vital
              label="Last poll"
              value={formatRelative(snapshot.fetchedAt)}
              hint={new Date(snapshot.fetchedAt).toLocaleTimeString("en-US", { hour12: false })}
              mono={false}
            />
          </dl>
        </div>

        {/* History ribbon — last N polls as a vertical stack of bars on lg, a
            horizontal strip below. Each cell is the status of one tick. */}
        <div className="lg:w-[180px]">
          <div className="mb-1.5 flex items-center justify-between">
            <span className="font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
              Recent polls
            </span>
            <Activity className="h-3 w-3 text-[var(--color-muted-foreground)]" aria-hidden />
          </div>
          <HistoryRibbon ticks={history} />
          <p className="mt-1.5 font-mono text-[10.5px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
            session · {history.length}/{HISTORY_LIMIT}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

function Vital({
  label,
  value,
  hint,
  mono = true,
}: {
  label: string;
  value: React.ReactNode;
  hint?: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div>
      <dt className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd
        className={cn(
          "mt-1 text-[15px] font-semibold tracking-tight tabular-nums",
          mono && "font-mono",
        )}
      >
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

function HistoryRibbon({ ticks }: { ticks: Tick[] }) {
  // Pad with placeholders so the ribbon has a consistent length even on
  // first paint — empty cells render as muted ticks.
  const filled: Array<Tick | null> = [...Array(HISTORY_LIMIT - ticks.length).fill(null), ...ticks];
  return (
    <div className="flex gap-[3px]" role="img" aria-label="Recent poll history">
      {filled.map((tick, i) => (
        <span
          key={i}
          className="h-7 flex-1 rounded-[2px] transition-colors"
          title={tick ? `${tick.status} · ${new Date(tick.at).toLocaleTimeString()}` : "no data"}
          style={{
            backgroundColor: tick
              ? TONE_VAR[toneFor(tick.status)]
              : "var(--color-muted)",
            opacity: tick ? (tick.status === "Healthy" ? 0.8 : 1) : 0.45,
          }}
        />
      ))}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Per-check card — telemetry rack-unit faceplate.
//
// Aesthetic vocabulary: each check reads as one slot in an instrument
// panel. A subtle tone-tinted rail along the top edge identifies the
// status. The slot is labelled (SLOT 01, 02 …) and the dependency name
// is rendered in mono — it IS a service identifier, treat it like one.
// The dominant readout is the latency, big and tabular, paired with a
// log-curved gauge bar that tints to the status tone. A glowing LED
// chip on the right communicates state at a glance and breathes
// gently. Detail metadata renders as `KEY=value` env-var chips.
// ────────────────────────────────────────────────────────────────────────

function splitLatency(ms: number): [string, string] {
  if (ms < 10) return [ms.toFixed(1), "ms"];
  if (ms < 1000) return [ms.toFixed(0), "ms"];
  return [(ms / 1000).toFixed(2), "s"];
}

function CheckCard({
  entry,
  index,
  animationDelayMs,
}: {
  entry: HealthEntry;
  index: number;
  animationDelayMs: number;
}) {
  const tone = toneFor(entry.status);
  const Icon = iconForCheck(entry.name);
  const toneColor = TONE_VAR[tone];
  const detailEntries = entry.details
    ? Object.entries(entry.details).filter(([k]) => k !== "tag")
    : [];

  // sqrt-scaled fill against a 500ms soft budget. A linear bar parks
  // every fast check at <5% (visually dead). sqrt gives 1ms ~= 4%,
  // 50ms ~= 31%, 250ms ~= 71%, 500ms = 100%, and saturates beyond.
  const pct = Math.min(100, Math.sqrt(Math.max(0, entry.durationMs) / 500) * 100);
  const [num, unit] = splitLatency(entry.durationMs);

  return (
    <article
      className="health-card fsh-enter"
      aria-label={`${entry.name} — ${entry.status}`}
      style={
        {
          animationDelay: `${animationDelayMs}ms`,
          "--tone": toneColor,
        } as React.CSSProperties
      }
    >
      <span aria-hidden className="health-card__rail" />

      <header className="health-card__header">
        <div className="health-card__id">
          <span className="health-card__icon" aria-hidden>
            <Icon className="h-3.5 w-3.5" />
          </span>
          <div className="health-card__title-block">
            <span className="health-card__index">
              SLOT · {String(index + 1).padStart(2, "0")}
            </span>
            <h3 className="health-card__name" title={entry.name}>
              {entry.name}
            </h3>
          </div>
        </div>

        <span className="health-card__led-block" aria-label={entry.status}>
          <span className="health-card__led" aria-hidden />
          <span className="health-card__led-label">{entry.status}</span>
        </span>
      </header>

      {entry.description ? (
        <p className="health-card__desc">{entry.description}</p>
      ) : (
        <p className="health-card__desc health-card__desc--muted">
          No description registered.
        </p>
      )}

      <div className="health-card__readout">
        <div className="health-card__numeric">
          <span className="health-card__num">{num}</span>
          <span className="health-card__unit">{unit}</span>
        </div>
        <span className="health-card__metric-label">latency</span>
      </div>

      <div className="health-card__bar" aria-hidden>
        <span className="health-card__bar-fill" style={{ width: `${pct}%` }} />
      </div>

      {detailEntries.length > 0 && (
        <div className="health-card__details">
          {detailEntries.slice(0, 4).map(([k, v]) => (
            <span
              className="health-card__chip"
              key={k}
              title={`${k}: ${String(v)}`}
            >
              <span className="health-card__chip-key">{k}</span>
              <span className="health-card__chip-eq">=</span>
              <span className="health-card__chip-val">{String(v)}</span>
            </span>
          ))}
          {detailEntries.length > 4 && (
            <span className="health-card__chip-more">
              +{detailEntries.length - 4}
            </span>
          )}
        </div>
      )}
    </article>
  );
}

// ────────────────────────────────────────────────────────────────────────
// Skeletons + error
// ────────────────────────────────────────────────────────────────────────

function HeroSkeleton() {
  return (
    <Card>
      <CardContent className="grid grid-cols-1 gap-6 px-6 py-6 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <Skeleton className="h-9 w-9 rounded-lg" />
            <Skeleton className="h-5 w-20 rounded-full" />
          </div>
          <Skeleton className="h-7 w-72" />
          <Skeleton className="h-4 w-96 max-w-full" />
          <div className="grid grid-cols-2 gap-x-8 gap-y-3 pt-3 sm:grid-cols-4">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="space-y-1.5">
                <Skeleton className="h-3 w-16" />
                <Skeleton className="h-5 w-20" />
                <Skeleton className="h-3 w-14" />
              </div>
            ))}
          </div>
        </div>
        <Skeleton className="h-7 w-[180px]" />
      </CardContent>
    </Card>
  );
}

function ChecksSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
      {[0, 1, 2, 3, 4, 5].map((i) => (
        <div
          key={i}
          className="health-card health-card--skeleton"
          style={{ ["--tone"]: "var(--color-muted-foreground)" } as React.CSSProperties}
        >
          <div className="health-card__header">
            <div className="health-card__id">
              <Skeleton className="h-7 w-7 rounded-md" />
              <div className="space-y-1.5">
                <Skeleton className="h-2 w-14" />
                <Skeleton className="h-3 w-28" />
              </div>
            </div>
            <Skeleton className="h-5 w-20 rounded-full" />
          </div>
          <Skeleton className="h-3 w-full" />
          <div className="flex items-baseline justify-between gap-2">
            <Skeleton className="h-7 w-20" />
            <Skeleton className="h-2 w-12" />
          </div>
          <Skeleton className="h-1 w-full rounded-full" />
        </div>
      ))}
    </div>
  );
}

function ErrorPanel({ message }: { message?: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-[var(--color-destructive)]">
          <CircleAlert className="h-4 w-4" />
          Health endpoint unreachable
        </CardTitle>
        <CardDescription>
          {message ??
            "The browser couldn't reach /health/ready. The API may be down, or a network policy is blocking the request."}
        </CardDescription>
      </CardHeader>
    </Card>
  );
}

function EmptyChecks() {
  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center gap-2 py-10 text-center">
        <ShieldCheck className="h-5 w-5 text-[var(--color-muted-foreground)]" />
        <div className="text-sm font-medium tracking-tight">No checks registered</div>
        <p className="max-w-sm text-xs leading-relaxed text-[var(--color-muted-foreground)]">
          Modules can register dependency checks via{" "}
          <code className="font-mono">AddHealthChecks()</code>. None are reporting yet.
        </p>
      </CardContent>
    </Card>
  );
}
