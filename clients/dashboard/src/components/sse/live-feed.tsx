import { useSse } from "@/sse/sse-context";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

function formatTime(ts: number) {
  return new Date(ts).toLocaleTimeString();
}

function formatData(data: unknown, raw: string): string {
  if (typeof data === "string") return data;
  try {
    return JSON.stringify(data, null, 2);
  } catch {
    return raw;
  }
}

export function LiveFeed({ limit = 25 }: { limit?: number }) {
  const { events, status } = useSse();
  const visible = events.slice(0, limit);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Live activity</CardTitle>
        <CardDescription>
          Real-time events pushed over SSE. {status === "connected" ? "Stream is open." : "Waiting for stream…"}
        </CardDescription>
      </CardHeader>
      <CardContent className="p-0">
        {visible.length === 0 ? (
          <div className="px-6 py-10 text-center text-sm text-[var(--color-muted-foreground)]">
            No events yet. Activity will appear here as the backend publishes it.
          </div>
        ) : (
          <ul className="divide-y divide-[var(--color-border)]">
            {visible.map((ev) => (
              <li key={ev.id} className="flex gap-4 px-6 py-3 text-sm">
                <div className="shrink-0 text-xs tabular-nums text-[var(--color-muted-foreground)]">
                  {formatTime(ev.receivedAt)}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="inline-flex rounded-md bg-[var(--color-accent)] px-2 py-0.5 text-xs font-medium text-[var(--color-accent-foreground)]">
                      {ev.type}
                    </span>
                  </div>
                  <pre className="mt-1 overflow-x-auto whitespace-pre-wrap break-words font-mono text-xs text-[var(--color-muted-foreground)]">
                    {formatData(ev.data, ev.rawData)}
                  </pre>
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
