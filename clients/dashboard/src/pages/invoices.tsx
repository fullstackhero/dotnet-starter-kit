import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileText, RefreshCw } from "lucide-react";
import {
  getMyInvoices,
  type InvoiceDto,
  type InvoiceStatus,
} from "@/api/billing";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ApiRequestError } from "@/lib/api-client";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

// ────────────────────────────────────────────────────────────────────
// Pure helpers — module scope so they're not re-allocated each render.
// ────────────────────────────────────────────────────────────────────

const numberFmt = new Intl.NumberFormat("en-US");

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

const dateShort = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "2-digit",
  year: "numeric",
});

function formatDate(iso: string) {
  return dateShort.format(new Date(iso));
}

function formatPeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

function statusTone(status: InvoiceStatus): React.ComponentProps<typeof Badge>["variant"] {
  switch (status) {
    case "Paid":
      return "success";
    case "Issued":
      return "info";
    case "Draft":
      return "default";
    case "Void":
      return "danger";
    default:
      return "default";
  }
}

type Totals = {
  count: number;
  totalBilled: number;
  outstanding: number;
  paid: number;
  paidCount: number;
  currency: string;
};

function summarize(invoices: InvoiceDto[]): Totals {
  if (invoices.length === 0) {
    return {
      count: 0,
      totalBilled: 0,
      outstanding: 0,
      paid: 0,
      paidCount: 0,
      currency: "USD",
    };
  }
  // Use the first invoice's currency as the display currency. Real
  // billing systems are typically single-currency per tenant.
  const currency = invoices[0].currency;
  let totalBilled = 0;
  let outstanding = 0;
  let paid = 0;
  let paidCount = 0;
  for (const inv of invoices) {
    totalBilled += inv.subtotalAmount;
    if (inv.status === "Paid") {
      paid += inv.subtotalAmount;
      paidCount++;
    } else if (inv.status === "Issued") {
      outstanding += inv.subtotalAmount;
    }
  }
  return { count: invoices.length, totalBilled, outstanding, paid, paidCount, currency };
}

// ────────────────────────────────────────────────────────────────────
// Subcomponents
// ────────────────────────────────────────────────────────────────────

function KpiTile({
  label,
  value,
  subtitle,
}: {
  label: string;
  value: React.ReactNode;
  subtitle: React.ReactNode;
}) {
  return (
    <Card interactive>
      <CardContent className="px-5 pb-5 pt-5">
        <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <div className="text-display mt-3 text-2xl font-semibold leading-none tabular-nums">
          {value}
        </div>
        <div className="mt-2 text-xs leading-relaxed text-[var(--color-muted-foreground)]">
          {subtitle}
        </div>
      </CardContent>
    </Card>
  );
}

function InvoiceRow({
  invoice,
  delayMs,
}: {
  invoice: InvoiceDto;
  delayMs: number;
}) {
  return (
    <li
      className={cn(
        "fsh-enter group/row grid grid-cols-[1fr_auto] items-center gap-x-6 gap-y-1",
        "border-t border-[var(--color-border)] px-6 py-3.5 first:border-t-0",
        "transition-colors hover:bg-[var(--color-muted)]",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      {/* Identity column */}
      <div className="flex items-center gap-3">
        <span
          aria-hidden
          className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] ring-1 ring-inset ring-[var(--color-border)]"
        >
          <FileText className="h-4 w-4" />
        </span>
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight">
              {invoice.invoiceNumber}
            </code>
            <Badge variant={statusTone(invoice.status)}>{invoice.status}</Badge>
          </div>
          <div className="mt-1 font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
            period {formatPeriod(invoice.periodYear, invoice.periodMonth)} ·
            created {formatDate(invoice.createdAtUtc)}
            {invoice.paidAtUtc && (
              <>
                {" · "}
                <span className="text-[var(--color-success)]">
                  paid {formatDate(invoice.paidAtUtc)}
                </span>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Amount column */}
      <div className="text-right">
        <div className="text-display text-base font-semibold tabular-nums">
          {formatMoney(invoice.subtotalAmount, invoice.currency)}
        </div>
        {invoice.dueAtUtc && invoice.status === "Issued" && (
          <div className="font-mono text-[11px] text-[var(--color-warning)]">
            due {formatDate(invoice.dueAtUtc)}
          </div>
        )}
      </div>
    </li>
  );
}

function ListSkeleton() {
  return (
    <ul>
      {[0, 1, 2].map((i) => (
        <li
          key={i}
          className="grid grid-cols-[1fr_auto] gap-x-6 border-t border-[var(--color-border)] px-6 py-3.5 first:border-t-0"
        >
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded-md" />
            <div className="space-y-1.5">
              <Skeleton className="h-4 w-40" />
              <Skeleton className="h-3 w-56" />
            </div>
          </div>
          <Skeleton className="h-5 w-20" />
        </li>
      ))}
    </ul>
  );
}

function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-14 text-center">
      <span
        aria-hidden
        className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
      >
        <FileText className="h-4 w-4" />
      </span>
      <div className="text-sm font-medium tracking-tight">{title}</div>
      <p className="max-w-sm text-xs leading-relaxed text-[var(--color-muted-foreground)]">
        {description}
      </p>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────

export function InvoicesPage() {
  const { user } = useAuth();
  const query = useQuery({
    queryKey: ["billing", "invoices", "me"],
    queryFn: getMyInvoices,
    staleTime: 30_000,
  });

  const invoices = query.data ?? [];
  const sorted = useMemo(
    () =>
      [...invoices].sort(
        (a, b) =>
          new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
      ),
    [invoices],
  );
  const totals = useMemo(() => summarize(invoices), [invoices]);

  const errorMessage =
    query.error instanceof ApiRequestError
      ? query.error.problem?.detail ?? query.error.message
      : query.error
        ? "Failed to load invoices."
        : null;

  return (
    <div className="space-y-7">
      {/* ── Header ───────────────────────────────────────────────── */}
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
            Invoices
          </h1>
          <p className="mt-1 text-sm leading-relaxed text-[var(--color-muted-foreground)]">
            Your tenant's billing history.
          </p>
        </div>

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
      </header>

      {/* ── KPI strip ────────────────────────────────────────────── */}
      <section
        aria-label="Billing summary"
        className="grid grid-cols-1 gap-4 sm:grid-cols-3"
      >
        <div className="fsh-enter fsh-enter-2">
          <KpiTile
            label="Total billed"
            value={
              query.isLoading ? (
                <Skeleton className="h-7 w-28" />
              ) : (
                formatMoney(totals.totalBilled, totals.currency)
              )
            }
            subtitle={
              <>
                across <span className="font-mono tabular-nums">{numberFmt.format(totals.count)}</span> invoices
              </>
            }
          />
        </div>
        <div className="fsh-enter fsh-enter-2">
          <KpiTile
            label="Outstanding"
            value={
              query.isLoading ? (
                <Skeleton className="h-7 w-24" />
              ) : totals.outstanding > 0 ? (
                <span className="text-[var(--color-warning)]">
                  {formatMoney(totals.outstanding, totals.currency)}
                </span>
              ) : (
                formatMoney(0, totals.currency)
              )
            }
            subtitle={
              totals.outstanding > 0
                ? "issued, awaiting payment"
                : "no unpaid invoices"
            }
          />
        </div>
        <div className="fsh-enter fsh-enter-3">
          <KpiTile
            label="Paid"
            value={
              query.isLoading ? (
                <Skeleton className="h-7 w-24" />
              ) : (
                <span className="text-[var(--color-success)]">
                  {formatMoney(totals.paid, totals.currency)}
                </span>
              )
            }
            subtitle={
              <>
                <span className="font-mono tabular-nums">{numberFmt.format(totals.paidCount)}</span>{" "}
                {totals.paidCount === 1 ? "invoice" : "invoices"} cleared
              </>
            }
          />
        </div>
      </section>

      {/* ── List ─────────────────────────────────────────────────── */}
      <Card className="fsh-enter fsh-enter-4">
        <CardHeader>
          <div className="flex items-center justify-between gap-4">
            <div>
              <CardTitle className="flex items-center gap-2">
                All invoices
                {!query.isLoading && (
                  <Badge variant="default">{numberFmt.format(totals.count)}</Badge>
                )}
              </CardTitle>
              <CardDescription>
                Sorted by creation date, newest first.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {errorMessage && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {errorMessage}
            </div>
          )}
          {query.isLoading ? (
            <ListSkeleton />
          ) : sorted.length === 0 ? (
            <EmptyState
              title="No invoices yet"
              description="Once your tenant has been billed for a period, invoices will appear here."
            />
          ) : (
            <ul>
              {sorted.map((invoice, idx) => (
                <InvoiceRow key={invoice.id} invoice={invoice} delayMs={50 * idx} />
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
