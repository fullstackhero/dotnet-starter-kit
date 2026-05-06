import { LiveFeed } from "@/components/sse/live-feed";
import { useAuth } from "@/auth/use-auth";
import { useSse } from "@/sse/sse-context";
import { Badge } from "@/components/ui/badge";

export function ActivityPage() {
  const { user } = useAuth();
  const { status, eventCount } = useSse();

  return (
    <div className="space-y-7">
      <header className="fsh-enter fsh-enter-1 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
              Tenant
            </span>
            <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
              {user?.tenant ?? "—"}
            </code>
          </div>
          <h1 className="text-display mt-2 text-[28px] font-semibold leading-tight">
            Live activity
          </h1>
          <p className="mt-1 text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            Full event log streamed from the API over Server-Sent Events.
          </p>
        </div>

        <div className="flex items-center gap-3">
          {status === "connected" ? (
            <Badge variant="success">streaming</Badge>
          ) : status === "error" ? (
            <Badge variant="danger">offline</Badge>
          ) : (
            <Badge variant="default">{status}</Badge>
          )}
          <div className="flex items-center gap-1.5">
            <span className="font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]">
              total
            </span>
            <span className="font-mono text-sm tabular-nums">
              {new Intl.NumberFormat("en-US").format(eventCount)}
            </span>
          </div>
        </div>
      </header>

      <section className="fsh-enter fsh-enter-2">
        <LiveFeed limit={200} />
      </section>
    </div>
  );
}
