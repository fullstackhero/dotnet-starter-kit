import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowUpRight,
  CalendarClock,
  CreditCard,
  Gauge,
  Receipt,
} from "lucide-react";
import {
  getMyInvoices,
  getMyStatus,
  getMySubscription,
  getUsageSnapshots,
  type InvoiceDto,
  type InvoiceStatus,
  type SubscriptionDto,
  type TenantExpiryState,
  type TenantStatusDto,
  type UsageSnapshotDto,
} from "@/api/billing";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  EntityDetailSection,
  EntityPageHeader,
  EntityStatusBadge,
  ErrorBand,
  type EntityStatusTone,
} from "@/components/list";
import { describe, formatDate, formatMoney } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────
// Pure view helpers — module scope.
// ────────────────────────────────────────────────────────────────────

const numberFmt = new Intl.NumberFormat("en-US");
const formatNumber = (n: number) => numberFmt.format(n);

type UsageRowVm = {
  resource: string;
  used: number;
  limit: number;
  overage: number;
  utilization: number;
};

function toUsageRows(snapshots: UsageSnapshotDto[]): UsageRowVm[] {
  const now = new Date();
  const cy = now.getUTCFullYear();
  const cm = now.getUTCMonth() + 1;
  return snapshots
    .filter((s) => s.periodYear === cy && s.periodMonth === cm)
    .map((s) => ({
      resource: String(s.resource),
      used: s.usedUnits,
      limit: s.limitUnits,
      overage: s.overage,
      utilization: s.limitUnits > 0 ? Math.min(100, (s.usedUnits / s.limitUnits) * 100) : 0,
    }))
    .sort((a, b) => b.utilization - a.utilization);
}

function expiryTone(state: TenantExpiryState | undefined): {
  tone: EntityStatusTone;
  label: string;
} {
  switch (state) {
    case "InGrace":
      return { tone: "warning", label: "In grace" };
    case "Expired":
      return { tone: "danger", label: "Expired" };
    case "Active":
      return { tone: "success", label: "Active" };
    default:
      return { tone: "default", label: "Unknown" };
  }
}

function invoiceStatusTone(status: InvoiceStatus): EntityStatusTone {
  switch (status) {
    case "Paid":
      return "success";
    case "Issued":
      return "info";
    case "Void":
      return "danger";
    default:
      return "default";
  }
}

function formatPeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

// ────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────

export function SubscriptionPage() {
  const status = useQuery({
    queryKey: ["tenant", "me", "status"],
    queryFn: () => getMyStatus(),
    staleTime: 60_000,
  });

  const subscription = useQuery({
    queryKey: ["billing", "subscriptions", "me"],
    queryFn: () => getMySubscription(),
    staleTime: 60_000,
  });

  const usage = useQuery({
    queryKey: ["billing", "usage"],
    queryFn: () => getUsageSnapshots(),
    staleTime: 60_000,
  });

  const invoices = useQuery({
    queryKey: ["billing", "invoices", "me", { pageNumber: 1, pageSize: 5 }],
    queryFn: () => getMyInvoices({ pageNumber: 1, pageSize: 5 }),
    staleTime: 60_000,
  });

  const usageRows = useMemo(
    () => (usage.data ? toUsageRows(usage.data) : []),
    [usage.data],
  );

  const recentInvoices = useMemo(() => {
    const items = invoices.data?.items ?? [];
    return [...items].sort(
      (a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
    );
  }, [invoices.data]);

  const errorMessage = status.error
    ? describe(status.error)
    : subscription.error
      ? describe(subscription.error)
      : null;

  const planName =
    status.data?.plan ?? subscription.data?.planKey ?? null;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={CreditCard}
        title="Subscription"
        description="Your tenant's plan, validity, usage, and recent invoices."
      />

      {errorMessage && <ErrorBand message={errorMessage} />}

      <div className="flex flex-col gap-4 lg:flex-row">
        {/* Left rail — plan + validity */}
        <aside className="w-full space-y-4 lg:w-[360px] lg:shrink-0">
          <EntityDetailSection title="Plan" icon={CreditCard}>
            <PlanBody
              planName={planName}
              subscription={subscription.data}
              loading={status.isLoading || subscription.isLoading}
            />
          </EntityDetailSection>

          <EntityDetailSection title="Validity" icon={CalendarClock}>
            <ValidityBody status={status.data} loading={status.isLoading} />
          </EntityDetailSection>
        </aside>

        {/* Right column — usage + invoices */}
        <div className="w-full min-w-0 flex-1 space-y-4">
          <EntityDetailSection
            title="Usage by resource"
            icon={Gauge}
            description="Current-month consumption against your plan limits."
          >
            <UsageBody
              rows={usageRows}
              loading={usage.isLoading}
              isError={usage.isError}
            />
          </EntityDetailSection>

          <EntityDetailSection
            title="Recent invoices"
            icon={Receipt}
            description="Your five most recent invoices."
            action={
              <Link
                to="/invoices"
                className="inline-flex items-center gap-1 text-[11px] font-medium text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
              >
                See all <ArrowUpRight className="size-3" />
              </Link>
            }
          >
            <RecentInvoicesBody
              invoices={recentInvoices}
              loading={invoices.isLoading}
              isError={invoices.isError}
            />
          </EntityDetailSection>
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Plan body
// ────────────────────────────────────────────────────────────────────

function PlanBody({
  planName,
  subscription,
  loading,
}: {
  planName: string | null;
  subscription: SubscriptionDto | null | undefined;
  loading: boolean;
}) {
  if (loading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-7 w-3/5" />
        <Skeleton className="h-3 w-2/5" />
        <Skeleton className="h-3 w-4/5" />
      </div>
    );
  }

  if (!planName && !subscription) {
    return (
      <div className="space-y-1">
        <div className="text-[13px] font-semibold tracking-tight text-[var(--color-foreground)]">
          No active subscription
        </div>
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          Your tenant has no plan assigned. Contact your operator to enable
          billing, quotas, and overage tracking.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-display text-[22px] font-bold tracking-tight text-[var(--color-foreground)]">
          {planName ?? "—"}
        </span>
        {subscription && (
          // The dashboard's /subscriptions/me only ever surfaces the ACTIVE
          // subscription (or null), so the badge is always the active tone.
          <Badge variant="success">{subscription.status}</Badge>
        )}
      </div>

      {subscription && (
        <dl className="space-y-1.5 text-[12px]">
          <div className="flex items-center justify-between gap-3">
            <dt className="text-[var(--color-muted-foreground)]">Started</dt>
            <dd className="tabular-nums text-[var(--color-foreground)]">
              {formatDate(subscription.startUtc)}
            </dd>
          </div>
          <div className="flex items-center justify-between gap-3">
            <dt className="text-[var(--color-muted-foreground)]">Ends</dt>
            <dd className="tabular-nums text-[var(--color-foreground)]">
              {subscription.endUtc ? formatDate(subscription.endUtc) : "open-ended"}
            </dd>
          </div>
        </dl>
      )}

      <p className="text-[11px] leading-relaxed text-[var(--color-muted-foreground)]">
        Plan changes are operator-driven. Contact your operator to upgrade,
        renew, or cancel.
      </p>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Validity body
// ────────────────────────────────────────────────────────────────────

function ValidityBody({
  status,
  loading,
}: {
  status: TenantStatusDto | undefined;
  loading: boolean;
}) {
  if (loading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-5 w-24" />
        <Skeleton className="h-3 w-3/5" />
      </div>
    );
  }

  if (!status) {
    return (
      <p className="text-[12px] text-[var(--color-muted-foreground)]">
        Tenant status is unavailable right now.
      </p>
    );
  }

  const { tone, label } = expiryTone(status.expiryState);

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <EntityStatusBadge tone={tone}>{label}</EntityStatusBadge>
        {!status.isActive && (
          <EntityStatusBadge tone="danger">Inactive</EntityStatusBadge>
        )}
      </div>

      <dl className="space-y-1.5 text-[12px]">
        <div className="flex items-center justify-between gap-3">
          <dt className="text-[var(--color-muted-foreground)]">Valid until</dt>
          <dd className="tabular-nums text-[var(--color-foreground)]">
            {formatDate(status.validUpto)}
          </dd>
        </div>
        {status.expiryState === "InGrace" && (
          <div className="flex items-center justify-between gap-3">
            <dt className="text-[var(--color-muted-foreground)]">Grace ends</dt>
            <dd className="tabular-nums text-[var(--color-warning)]">
              {formatDate(status.graceEndsUtc)}
            </dd>
          </div>
        )}
      </dl>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Usage body
// ────────────────────────────────────────────────────────────────────

function UsageBody({
  rows,
  loading,
  isError,
}: {
  rows: UsageRowVm[];
  loading: boolean;
  isError: boolean;
}) {
  if (loading) {
    return (
      <ul className="space-y-3">
        {[0, 1, 2].map((i) => (
          <li key={i} className="space-y-2 py-1.5">
            <div className="flex items-center justify-between">
              <Skeleton className="h-3.5 w-32" />
              <Skeleton className="h-3.5 w-24" />
            </div>
            <Skeleton className="h-1 w-full" />
          </li>
        ))}
      </ul>
    );
  }

  if (isError) {
    return (
      <p className="py-6 text-center text-[12px] text-[var(--color-muted-foreground)]">
        Couldn't load usage. Try refreshing.
      </p>
    );
  }

  if (rows.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-8 text-center">
        <span
          aria-hidden
          className="grid size-8 place-items-center rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)]"
        >
          <Gauge className="size-3.5 text-[var(--color-primary)]" />
        </span>
        <div className="text-[13px] font-semibold tracking-tight text-[var(--color-foreground)]">
          No usage captured yet
        </div>
        <p className="max-w-sm text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          Consumption will appear here as snapshots are recorded for this period.
        </p>
      </div>
    );
  }

  return (
    <ul>
      {rows.map((row) => (
        <UsageRow key={row.resource} row={row} />
      ))}
    </ul>
  );
}

function UsageRow({ row }: { row: UsageRowVm }) {
  const overUtilized = row.utilization >= 80;
  const overage = row.overage > 0;
  return (
    <li className="grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1.5 border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] py-2.5 first:border-t-0 first:pt-0">
      <div className="flex min-w-0 items-center gap-2">
        <span className="truncate text-[12.5px] font-medium tracking-tight text-[var(--color-foreground)]">
          {row.resource}
        </span>
        {overage && <Badge variant="danger">+{formatNumber(row.overage)}</Badge>}
      </div>

      <div className="text-right tabular-nums">
        <span className="text-[12.5px] font-semibold tracking-tight text-[var(--color-foreground)]">
          {formatNumber(row.used)}
        </span>
        <span className="ml-1 text-[11.5px] font-normal text-[var(--color-muted-foreground)]">
          / {formatNumber(row.limit)}
        </span>
        <span className="ml-2 text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {row.utilization.toFixed(0)}%
        </span>
      </div>

      <div className="col-span-2">
        <div className="relative h-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
          <div
            className={cn(
              "h-full rounded-full transition-[width] duration-[700ms] ease-[var(--ease-out-cubic)]",
              overage
                ? "bg-[var(--color-destructive)]"
                : overUtilized
                  ? "bg-[var(--color-warning)]"
                  : "bg-[var(--color-primary)]",
            )}
            style={{ width: `${row.utilization}%` }}
          />
        </div>
      </div>
    </li>
  );
}

// ────────────────────────────────────────────────────────────────────
// Recent invoices body
// ────────────────────────────────────────────────────────────────────

function RecentInvoicesBody({
  invoices,
  loading,
  isError,
}: {
  invoices: InvoiceDto[];
  loading: boolean;
  isError: boolean;
}) {
  if (loading) {
    return (
      <ul className="space-y-2.5">
        {[0, 1, 2].map((i) => (
          <li key={i} className="flex items-center gap-3">
            <Skeleton className="size-8 rounded-lg" />
            <Skeleton className="h-3.5 w-40" />
            <Skeleton className="ml-auto h-3.5 w-16" />
          </li>
        ))}
      </ul>
    );
  }

  if (isError) {
    return (
      <p className="py-6 text-center text-[12px] text-[var(--color-muted-foreground)]">
        Couldn't load invoices. Try refreshing.
      </p>
    );
  }

  if (invoices.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 py-6 text-center">
        <Receipt className="size-4 text-[var(--color-muted-foreground)]" />
        <div className="text-[13px] font-semibold tracking-tight text-[var(--color-foreground)]">
          No invoices yet
        </div>
        <p className="max-w-sm text-[11.5px] text-[var(--color-muted-foreground)]">
          Once your tenant has been billed for a period, invoices will appear here.
        </p>
      </div>
    );
  }

  return (
    <ul className="-my-1 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
      {invoices.map((invoice) => (
        <li key={invoice.id}>
          <Link
            to={`/invoices/${invoice.id}`}
            className="group/row -mx-1 flex items-center gap-3 rounded-md px-1 py-2.5 transition-colors hover:bg-[var(--color-accent)]"
          >
            <span
              aria-hidden
              className="grid size-8 shrink-0 place-items-center rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
            >
              <Receipt className="size-3.5" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <code className="truncate font-mono text-[12.5px] font-medium tracking-tight text-[var(--color-foreground)]">
                  {invoice.invoiceNumber}
                </code>
                <EntityStatusBadge tone={invoiceStatusTone(invoice.status)}>
                  {invoice.status}
                </EntityStatusBadge>
              </div>
              <div className="mt-0.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                period {formatPeriod(invoice.periodYear, invoice.periodMonth)}
              </div>
            </div>
            <span className="shrink-0 font-display text-[13px] font-semibold tabular-nums text-[var(--color-foreground)]">
              {formatMoney(invoice.subtotalAmount, invoice.currency)}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  );
}
