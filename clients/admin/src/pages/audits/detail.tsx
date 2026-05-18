import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Check, ClipboardCheck, Copy } from "lucide-react";
import { getAudit, type AuditDetailDto } from "@/api/audits";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  PageHeader,
  ErrorBand,
  LoadingRow,
  FormShell,
  FormSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

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
    <div className="space-y-8">
      <PageHeader
        crumbs={[
          { label: "\\ Audits" },
          { label: id ?? "—", muted: true },
        ]}
        trailing={event ? event.eventType.toUpperCase() : "—"}
        title={event ? `${event.eventType} event` : "Audit event"}
        description={
          event?.source
            ? `Captured by ${event.source} at ${new Date(event.occurredAtUtc).toLocaleString()}.`
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

      {query.isLoading && <LoadingRow label="Loading event" />}

      {event && (
        <>
          <MetadataGrid event={event} />
          <PayloadViewer payload={event.payload} />
        </>
      )}
    </div>
  );
}

function MetadataGrid({ event }: { event: AuditDetailDto }) {
  return (
    <FormShell>
      <FormSection
        title="Metadata"
        description="What happened, where, by whom, and with what trace/correlation lineage."
      >
        <dl className="divide-y divide-[var(--color-border)]">
          <Row label="Event type" value={<Badge variant="info" className="font-mono uppercase tracking-[0.14em]">{event.eventType}</Badge>} />
          <Row label="Severity" value={<Badge variant={severityVariant(event.severity)} className="font-mono uppercase tracking-[0.14em]">{event.severity}</Badge>} />
          <Row label="Occurred at" mono value={new Date(event.occurredAtUtc).toISOString()} />
          <Row label="Received at" mono value={new Date(event.receivedAtUtc).toISOString()} />
          <Row label="Source" value={event.source ?? "—"} />
          <Row label="Tenant" mono value={event.tenantId ?? "—"} />
          <Row label="User" value={event.userName ? `${event.userName} (${event.userId})` : event.userId ?? "—"} />
          <Row label="Trace id" mono copy value={event.traceId ?? "—"} />
          <Row label="Span id" mono copy value={event.spanId ?? "—"} />
          <Row label="Correlation id" mono copy value={event.correlationId ?? "—"} />
          <Row label="Request id" mono copy value={event.requestId ?? "—"} />
          <Row label="Tags" value={renderTags(event.tags as number)} />
        </dl>
      </FormSection>
    </FormShell>
  );
}

function PayloadViewer({ payload }: { payload: unknown }) {
  const json = JSON.stringify(payload ?? null, null, 2);
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

  return (
    <FormShell>
      <FormSection
        title="Payload"
        description="Masked payload as captured by the auditing pipeline. PII fields have already been redacted; what you see is what's stored."
      >
        <div className="overflow-hidden rounded-md border border-[var(--color-border)]">
          <div className="flex items-center justify-between border-b border-[var(--color-border)] bg-[var(--color-surface-2)] px-4 py-2">
            <span className="meta text-[var(--color-muted-foreground)]">application/json</span>
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
          <pre className="max-h-[60vh] overflow-auto bg-[var(--color-surface-2)] px-4 py-3 font-mono text-[12px] leading-relaxed text-[var(--color-foreground)]">
            {json}
          </pre>
        </div>
      </FormSection>
    </FormShell>
  );
}

function Row({
  label,
  value,
  mono,
  copy,
}: {
  label: string;
  value: React.ReactNode;
  mono?: boolean;
  copy?: boolean;
}) {
  const [copied, setCopied] = useState(false);
  const copyValue = typeof value === "string" ? value : null;
  const onCopy = async () => {
    if (!copyValue || copyValue === "—") return;
    try {
      await navigator.clipboard.writeText(copyValue);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1200);
    } catch {
      /* noop */
    }
  };
  return (
    <div className="grid grid-cols-[12rem_1fr_auto] items-center gap-4 px-5 py-3">
      <span className="meta text-[var(--color-muted-foreground)]">{label}</span>
      <span className={cn("min-w-0 truncate text-sm", mono && "font-mono text-xs")}>
        {value}
      </span>
      {copy && copyValue && copyValue !== "—" ? (
        <button
          type="button"
          onClick={onCopy}
          className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
          aria-label="Copy"
        >
          {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
        </button>
      ) : (
        <span aria-hidden />
      )}
    </div>
  );
}

function severityVariant(s: string): "muted" | "info" | "warning" | "danger" | "default" {
  switch (s) {
    case "Critical":
    case "Error":
      return "danger";
    case "Warning":
      return "warning";
    case "Information":
      return "info";
    case "Trace":
    case "Debug":
      return "muted";
    default:
      return "default";
  }
}

const TAG_NAMES = [
  { bit: 1 << 0, label: "PiiMasked" },
  { bit: 1 << 1, label: "OutOfQuota" },
  { bit: 1 << 2, label: "Sampled" },
  { bit: 1 << 3, label: "RetainedLong" },
  { bit: 1 << 4, label: "HealthCheck" },
  { bit: 1 << 5, label: "Authentication" },
  { bit: 1 << 6, label: "Authorization" },
];

function renderTags(tags: number): React.ReactNode {
  if (!tags || tags === 0) return <span className="text-[var(--color-muted-foreground)]">—</span>;
  const set = TAG_NAMES.filter((t) => (tags & t.bit) === t.bit);
  if (set.length === 0) return <span className="text-[var(--color-muted-foreground)]">—</span>;
  return (
    <span className="flex flex-wrap items-center gap-1.5">
      {set.map((t) => (
        <code key={t.label} className="code-chip">
          {t.label}
        </code>
      ))}
    </span>
  );
}
