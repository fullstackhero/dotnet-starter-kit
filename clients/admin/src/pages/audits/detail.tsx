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
  ScrollText,
} from "lucide-react";
import { getAudit, type AuditDetailDto } from "@/api/audits";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { EntityPageHeader, ErrorBand, LoadingRow, SettingsSection } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

/**
 * Audit detail — forensic record view.
 *
 * Layout top to bottom:
 *   1. EntityPageHeader — icon + event-type + occurred-at
 *   2. Identity strip — event-type + severity chips + source
 *   3. Correlation strip — trace / span / correlation / request IDs as
 *      copyable chips.
 *   4. Context grid — fact tiles.
 *   5. Payload viewer — full-width JSON pane with bounded inner scroll.
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
      <div>
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)} className="-ml-2 mb-4">
          <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Back
        </Button>
        <EntityPageHeader
          icon={ScrollText}
          title={event ? `${formatEventType(event.eventType)} event` : "Audit event"}
          description={
            event
              ? "Forensic record · captured by the audit pipeline. Use the correlation strip below to cross-reference logs and traces."
              : "Loading event details…"
          }
        />
      </div>

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
// 1. Identity band — eventType + severity chips, the source line.
// ─────────────────────────────────────────────────────────────────────────

function IdentityBand({ event }: { event: AuditDetailDto }) {
  const sev = severityTone(event.severity);
  const eventLabel = formatEventType(event.eventType);
  return (
    <SettingsSection>
      <div className="flex flex-wrap items-center gap-x-5 gap-y-3" aria-label="Event identity">
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
      </div>
    </SettingsSection>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 2. Correlation band — the most-used widget on the page.
// ─────────────────────────────────────────────────────────────────────────

function CorrelationBand({ event }: { event: AuditDetailDto }) {
  const slots: Array<{ label: string; value: string | null | undefined }> = [
    { label: "Trace id", value: event.traceId },
    { label: "Span id", value: event.spanId },
    { label: "Correlation id", value: event.correlationId },
    { label: "Request id", value: event.requestId },
  ];

  return (
    <SettingsSection
      icon={Fingerprint}
      title="\\ Correlation"
      description="Paste into your observability stack"
    >
      <div className="grid grid-cols-1 divide-y divide-[var(--color-border)] sm:grid-cols-2 sm:divide-x sm:divide-y-0 lg:grid-cols-4 lg:divide-y-0 -mx-5 border-t border-[var(--color-border)]">
        {slots.map((s) => (
          <CorrelationChip key={s.label} label={s.label} value={s.value ?? null} />
        ))}
      </div>
    </SettingsSection>
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
        <div className="font-mono text-[10.5px] uppercase tracking-[0.14em] truncate text-[var(--color-muted-foreground)]">
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
// 3. Context grid — auto-fit fact tiles.
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
    <SettingsSection
      icon={AlertTriangle}
      title="\\ Context"
      description="Who, where, when — the surrounding facts"
    >
      <div
        className="grid gap-px -mx-5 bg-[var(--color-border)] border-t border-[var(--color-border)]"
        style={{ gridTemplateColumns: "repeat(auto-fit, minmax(15rem, 1fr))" }}
      >
        {tiles.map((t) => (
          <FactTile key={t.label} label={t.label} value={t.value} mono={t.mono} />
        ))}
      </div>
    </SettingsSection>
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
      <div className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">{label}</div>
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
    <SettingsSection
      icon={FileText}
      title="\\ Payload"
      description={`application/json · ${lineCount} lines`}
      footer={
        <Button variant="ghost" size="sm" onClick={copy} className="h-7 px-2 text-xs">
          {copied ? (
            <>
              <ClipboardCheck className="mr-1 h-3.5 w-3.5" /> Copied
            </>
          ) : (
            <>
              <Copy className="mr-1 h-3.5 w-3.5" /> Copy JSON
            </>
          )}
        </Button>
      }
    >
      {/* min-w-0 prevents the pre from blowing out the page width */}
      <div className="-mx-5 min-w-0 bg-[var(--color-surface-2)] border-t border-[var(--color-border)]">
        <pre
          className="max-h-[70vh] min-w-0 overflow-auto px-5 py-4 font-mono text-[12px] leading-relaxed text-[var(--color-foreground)] sm:px-6"
        >
          <code>{json}</code>
        </pre>
      </div>
    </SettingsSection>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// helpers
// ─────────────────────────────────────────────────────────────────────────

function formatEventType(raw: unknown): string {
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
