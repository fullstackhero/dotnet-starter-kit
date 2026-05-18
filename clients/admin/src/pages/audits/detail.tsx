import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowLeft,
  Check,
  ClipboardCheck,
  Copy,
  FileText,
  Fingerprint,
} from "lucide-react";
import { getAudit, type AuditDetailDto } from "@/api/audits";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader, ErrorBand, LoadingRow } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * Audit detail — forensic record view.
 *
 * Layout is purpose-built for the data shape (12+ short metadata fields
 * + one large JSON payload) rather than the generic FormShell aside-rail
 * pattern, which left huge horizontal whitespace and let long stack-trace
 * lines blow out the page width.
 *
 * Sections, top to bottom:
 *   1. Identity strip — h1 + event-type + severity chips + occurred-at
 *   2. Correlation strip — trace / span / correlation / request IDs as
 *      copyable chips. Operators paste these into Grafana / Loki / Jaeger
 *      as their first move, so they get top billing.
 *   3. Context grid — auto-fit fact tiles for everything else.
 *   4. Payload viewer — full-width JSON pane with bounded inner scroll
 *      (both axes) so no payload, however ugly, ever pushes the page wider
 *      than the viewport.
 *
 * Page caps at `max-w-7xl mx-auto` so it doesn't sprawl on ultra-wide
 * monitors; sections themselves are full width inside that cap.
 */
export function AuditDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const query = useQuery({
    queryKey: ["audits", id],
    queryFn: () => getAudit(id!),
    enabled: Boolean(id),
  });

  const event = query.data;

  return (
    <div className="mx-auto w-full max-w-7xl space-y-6">
      <PageHeader
        crumbs={[
          { label: "\\ Audits" },
          { label: id ? `${id.slice(0, 8)}…` : "—", muted: true },
        ]}
        trailing={event ? formatEventType(event.eventType).toUpperCase() : "—"}
        title={event ? `${formatEventType(event.eventType)} event` : "Audit event"}
        description={
          event
            ? "Forensic record · captured by the audit pipeline. Use the correlation strip below to cross-reference logs and traces."
            : "Loading event details…"
        }
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Back
          </Button>
        }
      />

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load event."
          }
        />
      )}

      {query.isLoading && !event && <LoadingRow label="Loading event" />}

      {event && (
        <>
          <IdentityBand event={event} />
          <CorrelationBand event={event} />
          <ContextGrid event={event} />
          <PayloadPanel payload={event.payload} />
        </>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 1. Identity band — first thing the operator reads. eventType + severity
//    chips, the source line, and a quick contextual line. Acts like a
//    rich page subtitle and gives the page its sense of identity at a
//    glance without making the user scan a metadata table first.
// ─────────────────────────────────────────────────────────────────────────

function IdentityBand({ event }: { event: AuditDetailDto }) {
  const sev = severityTone(event.severity);
  const eventLabel = formatEventType(event.eventType);
  return (
    <section
      className="card-shell flex flex-wrap items-center gap-x-5 gap-y-3 px-5 py-4 sm:px-6"
      aria-label="Event identity"
    >
      <Badge
        variant={eventTypeVariant(eventLabel)}
        className="font-mono uppercase tracking-[0.16em]"
      >
        {eventLabel}
      </Badge>
      <Badge
        variant={sev.variant}
        className="font-mono uppercase tracking-[0.16em]"
      >
        {event.severity}
      </Badge>
      {event.source && (
        <span className="min-w-0 truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
          <span className="opacity-60">source · </span>
          <span className="text-[var(--color-foreground)]">{event.source}</span>
        </span>
      )}
      <span className="ml-auto shrink-0 font-mono text-[11px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
        {formatTimestamp(event.occurredAtUtc)}
      </span>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 2. Correlation band — the most-used widget on the page. Each chip is a
//    self-contained copy-to-clipboard target so operators can fire IDs
//    into Grafana / Loki / Jaeger without text-selecting from a table row.
// ─────────────────────────────────────────────────────────────────────────

function CorrelationBand({ event }: { event: AuditDetailDto }) {
  const slots: Array<{ label: string; value: string | null | undefined }> = [
    { label: "Trace id", value: event.traceId },
    { label: "Span id", value: event.spanId },
    { label: "Correlation id", value: event.correlationId },
    { label: "Request id", value: event.requestId },
  ];

  return (
    <section className="card-shell overflow-hidden" aria-label="Correlation identifiers">
      <header className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3 sm:px-6">
        <span className="flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.22em]">
          <Fingerprint className="h-3 w-3 text-[var(--color-accent-signal)]" aria-hidden />
          {"\\ Correlation"}
        </span>
        <span className="text-[10.5px] text-[var(--color-muted-foreground)]">
          paste into your observability stack
        </span>
      </header>
      <div className="grid grid-cols-1 divide-y divide-[var(--color-border)] sm:grid-cols-2 sm:divide-x sm:divide-y-0 lg:grid-cols-4 lg:divide-y-0">
        {slots.map((s) => (
          <CorrelationChip key={s.label} label={s.label} value={s.value ?? null} />
        ))}
      </div>
    </section>
  );
}

function CorrelationChip({ label, value }: { label: string; value: string | null }) {
  const [copied, setCopied] = useState(false);
  const hasValue = value && value !== "—";

  const onCopy = async () => {
    if (!hasValue) return;
    try {
      await navigator.clipboard.writeText(value!);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1200);
    } catch {
      /* noop */
    }
  };

  return (
    <button
      type="button"
      onClick={onCopy}
      disabled={!hasValue}
      className={cn(
        "group/chip flex min-w-0 items-start gap-3 px-5 py-3.5 text-left transition-colors sm:px-6",
        hasValue
          ? "hover:bg-[var(--color-muted)]/50"
          : "opacity-60",
      )}
      aria-label={hasValue ? `Copy ${label}` : `${label} not available`}
    >
      <div className="min-w-0 flex-1 space-y-1">
        <div className="meta truncate text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <div className="truncate font-mono text-[12px] text-[var(--color-foreground)]">
          {hasValue ? value : "—"}
        </div>
      </div>
      {hasValue && (
        <span
          aria-hidden
          className={cn(
            "grid h-6 w-6 shrink-0 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-all",
            "opacity-0 group-hover/chip:opacity-100 group-focus-visible/chip:opacity-100",
            copied && "opacity-100 text-[var(--color-success)]",
          )}
        >
          {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
        </span>
      )}
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 3. Context grid — auto-fit fact tiles. minmax(15rem, 1fr) gives ~4 cols
//    on a wide editor pane, ~2 on a tablet, ~1 on phone, without any
//    media queries. Each tile is self-contained so it can wrap freely.
// ─────────────────────────────────────────────────────────────────────────

function ContextGrid({ event }: { event: AuditDetailDto }) {
  const userLine = event.userName
    ? `${event.userName} (${event.userId})`
    : event.userId ?? "—";

  const tags = renderTagsInline(event.tags as number);

  const tiles: Array<{ label: string; value: React.ReactNode; mono?: boolean }> = [
    { label: "Occurred at", value: formatTimestamp(event.occurredAtUtc), mono: true },
    { label: "Received at", value: formatTimestamp(event.receivedAtUtc), mono: true },
    { label: "Tenant", value: event.tenantId ?? "—", mono: true },
    { label: "User", value: userLine, mono: true },
    { label: "Source", value: event.source ?? "—", mono: true },
    { label: "Tags", value: tags },
  ];

  return (
    <section className="card-shell overflow-hidden" aria-label="Event context">
      <header className="flex items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3 sm:px-6">
        <span className="flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.22em]">
          <AlertTriangle className="h-3 w-3 text-[var(--color-accent-signal)]" aria-hidden />
          {"\\ Context"}
        </span>
        <span className="text-[10.5px] text-[var(--color-muted-foreground)]">
          who, where, when — the surrounding facts
        </span>
      </header>
      <div
        className="grid gap-px bg-[var(--color-border)]"
        style={{ gridTemplateColumns: "repeat(auto-fit, minmax(15rem, 1fr))" }}
      >
        {tiles.map((t) => (
          <FactTile key={t.label} label={t.label} value={t.value} mono={t.mono} />
        ))}
      </div>
    </section>
  );
}

function FactTile({
  label,
  value,
  mono,
}: {
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="min-w-0 space-y-1 bg-[var(--color-card)] px-5 py-3.5 sm:px-6">
      <div className="meta text-[var(--color-muted-foreground)]">{label}</div>
      <div
        className={cn(
          "min-w-0 break-words text-sm text-[var(--color-foreground)]",
          mono && "font-mono text-[12.5px]",
        )}
      >
        {value}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 4. Payload — full-width JSON pane.
//
//    The width-blowout fix is `min-w-0` on the OUTER section. Without it,
//    the inner <pre> (whitespace: pre, default) reports its natural width
//    to the parent grid/flex which then exceeds the page, forcing a
//    horizontal scroll on the whole document. With min-w-0 in place the
//    pre's own overflow-x:auto correctly takes over.
// ─────────────────────────────────────────────────────────────────────────

function PayloadPanel({ payload }: { payload: unknown }) {
  const json = useMemo(() => JSON.stringify(payload ?? null, null, 2), [payload]);
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(json);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      /* noop */
    }
  };

  const lineCount = useMemo(() => json.split("\n").length, [json]);

  return (
    <section className="card-shell min-w-0 overflow-hidden" aria-label="Payload">
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--color-border)] px-5 py-3 sm:px-6">
        <span className="flex items-center gap-2 font-mono text-[10.5px] uppercase tracking-[0.22em]">
          <FileText className="h-3 w-3 text-[var(--color-accent-signal)]" aria-hidden />
          {"\\ Payload"}
        </span>
        <div className="flex items-center gap-3">
          <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            application/json · {lineCount} lines
          </span>
          <Button variant="ghost" size="sm" onClick={copy} className="h-7 px-2 text-xs">
            {copied ? (
              <>
                <ClipboardCheck className="mr-1 h-3.5 w-3.5" /> Copied
              </>
            ) : (
              <>
                <Copy className="mr-1 h-3.5 w-3.5" /> Copy
              </>
            )}
          </Button>
        </div>
      </header>

      {/* Gutter + scrollable code. The outer wrapper sets `min-w-0` so the
          page never widens past the viewport regardless of payload shape;
          the inner <pre> then scrolls horizontally on its own. */}
      <div className="min-w-0 bg-[var(--color-surface-2)]">
        <pre
          className={cn(
            "max-h-[70vh] min-w-0 overflow-auto px-5 py-4 font-mono text-[12px] leading-relaxed text-[var(--color-foreground)] sm:px-6",
          )}
        >
          <code>{json}</code>
        </pre>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// helpers
// ─────────────────────────────────────────────────────────────────────────

function formatEventType(raw: unknown): string {
  // Defensive — the API boundary coerces to a string union, but if an
  // unknown shape ever sneaks through we render something instead of
  // crashing on .toUpperCase().
  return typeof raw === "string" && raw.length > 0 ? raw : "Event";
}

function eventTypeVariant(eventType: string): "info" | "warning" | "danger" | "muted" {
  switch (eventType) {
    case "Exception":
      return "danger";
    case "Security":
      return "warning";
    case "EntityChange":
    case "Activity":
      return "info";
    default:
      return "muted";
  }
}

function severityTone(s: string): { variant: "muted" | "info" | "warning" | "danger" | "default" } {
  switch (s) {
    case "Critical":
    case "Error":
      return { variant: "danger" };
    case "Warning":
      return { variant: "warning" };
    case "Information":
      return { variant: "info" };
    case "Trace":
    case "Debug":
      return { variant: "muted" };
    default:
      return { variant: "default" };
  }
}

function formatTimestamp(value: string | undefined | null): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toISOString();
}

const TAG_NAMES: ReadonlyArray<{ bit: number; label: string }> = [
  { bit: 1 << 0, label: "PiiMasked" },
  { bit: 1 << 1, label: "OutOfQuota" },
  { bit: 1 << 2, label: "Sampled" },
  { bit: 1 << 3, label: "RetainedLong" },
  { bit: 1 << 4, label: "HealthCheck" },
  { bit: 1 << 5, label: "Authentication" },
  { bit: 1 << 6, label: "Authorization" },
];

function renderTagsInline(tags: number): React.ReactNode {
  if (!tags || tags === 0) {
    return <span className="text-[var(--color-muted-foreground)]">—</span>;
  }
  const set = TAG_NAMES.filter((t) => (tags & t.bit) === t.bit);
  if (set.length === 0) {
    return <span className="text-[var(--color-muted-foreground)]">—</span>;
  }
  return (
    <span className="flex flex-wrap items-center gap-1.5">
      {set.map((t) => (
        <code key={t.label} className="code-chip text-[11px]">
          {t.label}
        </code>
      ))}
    </span>
  );
}
