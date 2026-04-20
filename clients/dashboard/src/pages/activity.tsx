import { LiveFeed } from "@/components/sse/live-feed";

export function ActivityPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Live activity</h1>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Full event log streamed from the API over Server-Sent Events.
        </p>
      </div>
      <LiveFeed limit={200} />
    </div>
  );
}
