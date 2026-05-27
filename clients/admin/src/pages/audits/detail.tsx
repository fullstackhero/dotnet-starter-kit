import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  Check,
  ClipboardCheck,
  Copy,
  FileText,
  Fingerprint,
  ScrollText,
  X,
} from "lucide-react";
import { getAudit, type AuditDetailDto } from "@/api/audits";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, LoadingRow } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import {
  Sheet,
  SheetContent,
} from "@/components/ui/dialog";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────────
// AuditDetailSheet — side sheet shown when an audit row is clicked on the
// list page. Fetches the full record by id and renders the same 4 sections
// the old full-page route showed.
// ─────────────────────────────────────────────────────────────────────────

export interface AuditDetailSheetProps {
  /** The audit id to load, or null / undefined when the sheet is closed. */
  auditId: string | null | undefined;
  onClose: () => void;
}

export function AuditDetailSheet({ auditId, onClose }: AuditDetailSheetProps) {
  const open = Boolean(auditId);

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent
        side="right"
        showClose={false}
        className="w-[min(640px,95vw)] max-w-none overflow-hidden p-0"
      >
        <AuditDetailSheetBody auditId={auditId ?? null} onClose={onClose} />
      </SheetContent>
    </Sheet>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// Inner body — data-fetch + layout. Exported so it can be composed
// independently if needed.
// ─────────────────────────────────────────────────────────────────────────

export function AuditDetailSheetBody({
  auditId,
  onClose,
}: {
  auditId: string | null;
  onClose: () => void;
}) {
  const query = useQuery({
    queryKey: ["audits", auditId],
    queryFn: () => getAudit(auditId!),
    enabled: Boolean(auditId),
    staleTime: 60_000,
  });

  const event = query.data;

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-start justify-between gap-3 border-b border-[var(--color-border)] px-6 py-5">
        <div className="flex items-center gap-2.5">
          <span className="grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[var(--color-muted)]">
            <ScrollText className="h-4 w-4 text-[var(--color-muted-foreground)]" />
          </span>
          <div>
            <div className="text-[13px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)]">
              {event ? `${formatEventType(event.eventType)} event` : "Audit event"}
            </div>
            <div className="mt-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
              {event ? formatTimestamp(event.occurredAtUtc) : "Loading…"}
            </div>
          </div>
        </div>
        <button
          type="button"
          aria-label="Close"
          onClick={onClose}
          className={cn(
            "grid h-7 w-7 shrink-0 place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] transition-colors",
            "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          )}
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Scrollable body */}
      <div className="min-h-0 flex-1 overflow-y-auto">
        {query.isLoading && !event && (
          <div className="p-6">
            <LoadingRow label="Loading event" />
          </div>
        )}

        {query.isError && (
          <div className="p-6">
            <ErrorBand
              message={
                query.error instanceof ApiRequestError
                  ? query.error.problem?.detail ?? query.error.message
                  : "Failed to load event."
              }
            />
          </div>
        )}

        {event && (
          <div className="space-y-0 divide-y divide-[var(--color-border)]">
            <IdentityBand event={event} />
            <CorrelationBand event={event} />
            <ContextGrid event={event} />
            <PayloadPanel payload={event.payload} />
          </div>
        )}
      </div>
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
    <div className="px-6 py-4" aria-label="Event identity">
      <div className="mb-2 font-mono text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
        Identity
      </div>
      <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
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
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 2. Correlation band — copyable ID chips.
// ─────────────────────────────────────────────────────────────────────────

function CorrelationBand({ event }: { event: AuditDetailDto }) {
  const slots: Array<{ label: string; value: string | null | undefined }> = [
    { label: "Trace id", value: event.traceId },
    { label: "Span id", value: event.spanId },
    { label: "Correlation id", value: event.correlationId },
    { label: "Request id", value: event.requestId },
  ];

  return (
    <div className="px-6 py-4">
      <div className="mb-2 flex items-center gap-1.5">
        <Fingerprint className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
        <span className="font-mono text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          Correlation
        </span>
        <span className="ml-1 text-[10.5px] text-[var(--color-muted-foreground)]">
          — paste into your observability stack
        </span>
      </div>
      <div className="grid grid-cols-1 gap-1 sm:grid-cols-2">
        {slots.map((s) => (
          <CorrelationChip key={s.label} label={s.label} value={s.value ?? null} />
        ))}
      </div>
    </div>
  );
}

function CorrelationChip({ label, value }: { label: string; value: string | null }) {
  const [copied, setCopied] = useState(false);
  const hasValue = Boolean(value && value !== "—");

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
        "group/chip flex min-w-0 items-start gap-3 rounded-lg border border-[var(--color-border)] px-3 py-2.5 text-left transition-colors",
        hasValue
          ? "hover:bg-[var(--color-muted)]/50"
          : "opacity-60",
      )}
      aria-label={hasValue ? `Copy ${label}` : `${label} not available`}
    >
      <div className="min-w-0 flex-1 space-y-0.5">
        <div className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <div className="truncate font-mono text-[11.5px] text-[var(--color-foreground)]">
          {hasValue ? value : "—"}
        </div>
      </div>
      {hasValue && (
        <span
          aria-hidden
          className={cn(
            "grid h-5 w-5 shrink-0 place-items-center rounded text-[var(--color-muted-foreground)] transition-all",
            "opacity-0 group-hover/chip:opacity-100 group-focus-visible/chip:opacity-100",
            copied && "opacity-100 text-[var(--color-success)]",
          )}
        >
          {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
        </span>
      )}
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 3. Context grid — who/where/when fact tiles.
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
    <div className="px-6 py-4">
      <div className="mb-2 flex items-center gap-1.5">
        <AlertTriangle className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
        <span className="font-mono text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          Context
        </span>
      </div>
      <div
        className="grid gap-2"
        style={{ gridTemplateColumns: "repeat(auto-fit, minmax(13rem, 1fr))" }}
      >
        {tiles.map((t) => (
          <FactTile key={t.label} label={t.label} value={t.value} mono={t.mono} />
        ))}
      </div>
    </div>
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
    <div className="min-w-0 space-y-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-1)] px-3 py-2.5">
      <div className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
        {label}
      </div>
      <div
        className={cn(
          "min-w-0 break-words text-sm text-[var(--color-foreground)]",
          mono && "font-mono text-[12px]",
        )}
      >
        {value}
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// 4. Payload — JSON pane with copy button.
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
    <div className="px-6 py-4">
      <div className="mb-2 flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <FileText className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
          <span className="font-mono text-[10px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            Payload
          </span>
          <span className="text-[10.5px] text-[var(--color-muted-foreground)]">
            · {lineCount} lines
          </span>
        </div>
        <Button variant="ghost" size="sm" onClick={copy} className="h-6 px-2 text-[11px]">
          {copied ? (
            <>
              <ClipboardCheck className="mr-1 h-3 w-3" /> Copied
            </>
          ) : (
            <>
              <Copy className="mr-1 h-3 w-3" /> Copy
            </>
          )}
        </Button>
      </div>
      <pre className="max-h-[50vh] overflow-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-2)] px-4 py-3 font-mono text-[11.5px] leading-relaxed text-[var(--color-foreground)]">
        <code>{json}</code>
      </pre>
    </div>
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
