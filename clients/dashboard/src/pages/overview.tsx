import { useQuery } from "@tanstack/react-query";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { getMySubscription, getUsageSnapshots, type UsageSnapshotDto } from "@/api/billing";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { LiveFeed } from "@/components/sse/live-feed";
import { useAuth } from "@/auth/use-auth";

type UsageRow = {
  resource: string;
  used: number;
  limit: number;
  overage: number;
  utilization: number;
};

function toUsageRows(snapshots: UsageSnapshotDto[]): UsageRow[] {
  const now = new Date();
  const currentYear = now.getUTCFullYear();
  const currentMonth = now.getUTCMonth() + 1;
  return snapshots
    .filter((s) => s.periodYear === currentYear && s.periodMonth === currentMonth)
    .map((s) => ({
      resource: String(s.resource),
      used: s.usedUnits,
      limit: s.limitUnits,
      overage: s.overage,
      utilization: s.limitUnits > 0 ? Math.min(100, (s.usedUnits / s.limitUnits) * 100) : 0,
    }));
}

function BarColor(row: UsageRow): string {
  if (row.overage > 0) return "var(--color-destructive)";
  if (row.utilization >= 80) return "var(--color-chart-4)";
  return "var(--color-chart-2)";
}

export function OverviewPage() {
  const { user } = useAuth();

  const usage = useQuery({
    queryKey: ["billing", "usage"],
    queryFn: () => getUsageSnapshots(),
    staleTime: 60_000,
  });

  const subscription = useQuery({
    queryKey: ["billing", "subscription", "me"],
    queryFn: () => getMySubscription(),
    staleTime: 60_000,
  });

  const rows = usage.data ? toUsageRows(usage.data) : [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Overview</h1>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Welcome{user?.name ? `, ${user.name}` : ""}. Live telemetry for tenant{" "}
          <span className="font-medium text-[var(--color-foreground)]">{user?.tenant ?? "—"}</span>.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Usage this period</CardTitle>
            <CardDescription>
              Current-month consumption vs. plan limits per quota resource.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {usage.isLoading ? (
              <div className="flex h-64 items-center justify-center text-sm text-[var(--color-muted-foreground)]">
                Loading…
              </div>
            ) : rows.length === 0 ? (
              <div className="flex h-64 items-center justify-center text-sm text-[var(--color-muted-foreground)]">
                No usage captured yet this period.
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={rows}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                  <XAxis
                    dataKey="resource"
                    tick={{ fill: "var(--color-muted-foreground)", fontSize: 12 }}
                    stroke="var(--color-border)"
                  />
                  <YAxis
                    tick={{ fill: "var(--color-muted-foreground)", fontSize: 12 }}
                    stroke="var(--color-border)"
                  />
                  <Tooltip
                    contentStyle={{
                      background: "var(--color-card)",
                      border: "1px solid var(--color-border)",
                      borderRadius: 8,
                      fontSize: 12,
                    }}
                    cursor={{ fill: "var(--color-accent)", opacity: 0.3 }}
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="limit" name="Limit" fill="var(--color-muted)" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="used" name="Used" radius={[4, 4, 0, 0]}>
                    {rows.map((row) => (
                      <Cell key={row.resource} fill={BarColor(row)} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Subscription</CardTitle>
            <CardDescription>Current plan & status</CardDescription>
          </CardHeader>
          <CardContent>
            {subscription.isLoading ? (
              <div className="text-sm text-[var(--color-muted-foreground)]">Loading…</div>
            ) : !subscription.data ? (
              <div className="text-sm text-[var(--color-muted-foreground)]">
                No active subscription.
              </div>
            ) : (
              <dl className="space-y-3 text-sm">
                <div>
                  <dt className="text-xs uppercase tracking-wider text-[var(--color-muted-foreground)]">Plan</dt>
                  <dd className="font-medium">{subscription.data.planKey}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wider text-[var(--color-muted-foreground)]">Status</dt>
                  <dd className="font-medium">{subscription.data.status}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wider text-[var(--color-muted-foreground)]">Started</dt>
                  <dd>{new Date(subscription.data.startUtc).toLocaleDateString()}</dd>
                </div>
                {subscription.data.endUtc && (
                  <div>
                    <dt className="text-xs uppercase tracking-wider text-[var(--color-muted-foreground)]">Ends</dt>
                    <dd>{new Date(subscription.data.endUtc).toLocaleDateString()}</dd>
                  </div>
                )}
              </dl>
            )}
          </CardContent>
        </Card>
      </div>

      <LiveFeed limit={20} />
    </div>
  );
}
