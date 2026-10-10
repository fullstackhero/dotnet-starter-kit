import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import {
  ChevronLeft,
  ChevronRight,
  FileText,
  Filter,
  X,
} from "lucide-react";
import { listInvoices, type InvoiceDto, type InvoiceStatus } from "@/api/billing";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/list";
import { KpiTile } from "@/components/kpi-tile";
import { ApiRequestError } from "@/lib/api-client";
import { formatCurrency, formatDate } from "@/lib/format";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 20;

const STATUSES: InvoiceStatus[] = ["Draft", "Issued", "Paid", "Void"];

// ─── helpers ─────────────────────────────────────────────────────────

function formatPeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

function statusVariant(status: InvoiceStatus): React.ComponentProps<typeof Badge>["variant"] {
  switch (status) {
    case "Paid":
      return "success";
    case "Issued":
      return "info";
    case "Draft":
      return "warning";
    case "Void":
      return "danger";
    default:
      return "default";
  }
}

// ─── component ───────────────────────────────────────────────────────

export function InvoicesListPage() {
  const { t } = useTranslation("billing");
  const navigate = useNavigate();
  const describe = (err: unknown): string => {
    if (err instanceof ApiRequestError)
      return err.problem?.detail ?? err.problem?.title ?? err.message;
    if (err instanceof Error) return err.message;
    return t("invoices.loadError");
  };
  const statusLabel = (status: InvoiceStatus): string =>
    t(`status.${status.charAt(0).toLowerCase()}${status.slice(1)}`);
  const [pageNumber, setPageNumber] = useState(1);

  const [tenantFilter, setTenantFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState<InvoiceStatus | "">("");
  const [periodYear, setPeriodYear] = useState("");
  const [periodMonth, setPeriodMonth] = useState("");

  const filters = useMemo(
    () => ({
      tenantId: tenantFilter.trim() || undefined,
      status: statusFilter || undefined,
      periodYear: periodYear ? Number(periodYear) : undefined,
      periodMonth: periodMonth ? Number(periodMonth) : undefined,
    }),
    [tenantFilter, statusFilter, periodYear, periodMonth],
  );

  const query = useQuery({
    queryKey: ["billing", "invoices", { pageNumber, ...filters }],
    queryFn: () => listInvoices({ pageNumber, pageSize: PAGE_SIZE, ...filters }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  // useMemo dependencies need a stable reference; wrap the optional list once
  // so both the page render and the totals memo derive from the same value.
  const items = useMemo<InvoiceDto[]>(() => data?.items ?? [], [data]);

  const totals = useMemo(() => {
    let totalBilled = 0;
    let outstanding = 0;
    let paid = 0;
    let paidCount = 0;
    const firstCurrency = items[0]?.currency ?? "USD";
    for (const inv of items) {
      totalBilled += inv.subtotalAmount;
      if (inv.status === "Paid") {
        paid += inv.subtotalAmount;
        paidCount += 1;
      } else if (inv.status === "Issued") {
        outstanding += inv.subtotalAmount;
      }
    }
    return { totalBilled, outstanding, paid, paidCount, currency: firstCurrency };
  }, [items]);

  const filtersDirty =
    !!tenantFilter || !!statusFilter || !!periodYear || !!periodMonth;

  const clearFilters = () => {
    setTenantFilter("");
    setStatusFilter("");
    setPeriodYear("");
    setPeriodMonth("");
    setPageNumber(1);
  };

  return (
    <div className="space-y-6">
      {/* KPI strip — page-scope (current page, not all-time) */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiTile
          label={t("invoices.kpi.pageInvoices")}
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : data?.items.length ?? 0}
          subtitle={
            data
              ? t("invoices.kpi.totalCount", { count: data.totalCount })
              : t("invoices.kpi.loading")
          }
        />
        <KpiTile
          label={t("invoices.kpi.billed")}
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatCurrency(totals.totalBilled, totals.currency)
            )
          }
          subtitle={t("invoices.kpi.thisPage")}
        />
        <KpiTile
          label={t("invoices.kpi.outstanding")}
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatCurrency(totals.outstanding, totals.currency)
            )
          }
          subtitle={t("invoices.kpi.outstandingHint")}
        />
        <KpiTile
          label={t("invoices.kpi.paid")}
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatCurrency(totals.paid, totals.currency)
            )
          }
          subtitle={t("invoices.kpi.paidCount", { count: totals.paidCount })}
        />
      </div>

      {/* Filter panel */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between gap-3">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Filter className="h-4 w-4 text-[var(--color-muted-foreground)]" />
              <span>{t("invoices.filters.title")}</span>
            </CardTitle>
            <CardDescription>{t("invoices.filters.description")}</CardDescription>
          </div>
          {filtersDirty && (
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              <X className="mr-1 h-3.5 w-3.5" /> {t("invoices.filters.clear")}
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-1.5">
            <Label htmlFor="filter-tenant">{t("invoices.filters.tenant")}</Label>
            <Input
              id="filter-tenant"
              placeholder={t("invoices.filters.tenantPlaceholder")}
              value={tenantFilter}
              onChange={(e) => {
                setTenantFilter(e.target.value);
                setPageNumber(1);
              }}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="filter-status">{t("invoices.filters.status")}</Label>
            <Select
              id="filter-status"
              value={statusFilter}
              onValueChange={(v) => {
                setStatusFilter(v as InvoiceStatus | "");
                setPageNumber(1);
              }}
              options={STATUSES.map((s) => ({ value: s, label: statusLabel(s) }))}
              emptyLabel={t("status.all")}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="filter-year">{t("invoices.filters.year")}</Label>
            <Input
              id="filter-year"
              inputMode="numeric"
              placeholder="2026"
              value={periodYear}
              onChange={(e) => {
                setPeriodYear(e.target.value.replace(/[^0-9]/g, "").slice(0, 4));
                setPageNumber(1);
              }}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="filter-month">{t("invoices.filters.month")}</Label>
            <Input
              id="filter-month"
              inputMode="numeric"
              placeholder="1–12"
              value={periodMonth}
              onChange={(e) => {
                setPeriodMonth(e.target.value.replace(/[^0-9]/g, "").slice(0, 2));
                setPageNumber(1);
              }}
            />
          </div>
        </CardContent>
      </Card>

      {/* List */}
      <Card>
        <CardHeader>
          <CardTitle>{t("invoices.list.title")}</CardTitle>
          <CardDescription>
            {data
              ? t("invoices.list.summary", {
                  page: data.pageNumber,
                  total: Math.max(data.totalPages, 1),
                  count: data.totalCount,
                })
              : t("invoices.list.loading")}
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {query.isError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {describe(query.error)}
            </div>
          )}

          {query.isLoading && items.length === 0 ? (
            <ul className="divide-y divide-[var(--color-border)]">
              {Array.from({ length: 5 }).map((_, i) => (
                <li key={i} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="space-y-2">
                      <Skeleton className="h-4 w-40" />
                      <Skeleton className="h-3 w-64" />
                    </div>
                    <Skeleton className="h-5 w-20" />
                  </div>
                </li>
              ))}
            </ul>
          ) : items.length === 0 ? (
            <div className="px-6 py-10 text-center text-sm text-[var(--color-muted-foreground)]">
              {t("invoices.list.empty")}
            </div>
          ) : (
            <ul>
              {items.map((inv, i) => (
                <li key={inv.id} className="border-t border-[var(--color-border)] first:border-t-0">
                  <button
                    type="button"
                    onClick={() => navigate(`/billing/invoices/${inv.id}`)}
                    className={cn(
                      "fsh-enter grid w-full grid-cols-[1fr_auto] items-center gap-x-6 gap-y-1 px-6 py-4 text-left transition-colors hover:bg-[var(--color-muted)] cursor-pointer",
                    )}
                    style={{ animationDelay: `${Math.min(i, 8) * 25}ms` }}
                  >
                  {/* Identity column */}
                  <div className="flex min-w-0 items-center gap-3">
                    <span
                      aria-hidden
                      className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] ring-1 ring-inset ring-[var(--color-border)]"
                    >
                      <FileText className="h-4 w-4" />
                    </span>
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <code className="rounded bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight">
                          {inv.invoiceNumber}
                        </code>
                        <Badge variant={statusVariant(inv.status)}>{statusLabel(inv.status)}</Badge>
                        {inv.purpose && (
                          <Badge variant="outline">
                            {inv.purpose === "Subscription"
                              ? t("purpose.subscription")
                              : t("purpose.usage")}
                          </Badge>
                        )}
                      </div>
                      <div className="mt-1 truncate font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                        {t("label.tenant")}{" "}
                        <span className="text-[var(--color-foreground)]">{inv.tenantId}</span> ·{" "}
                        {t("label.period")} {formatPeriod(inv.periodYear, inv.periodMonth)} ·{" "}
                        {t("label.created")} {formatDate(inv.createdAtUtc)}
                        {inv.paidAtUtc && (
                          <>
                            {" · "}
                            <span className="text-[var(--color-success)]">
                              {t("label.paid")} {formatDate(inv.paidAtUtc)}
                            </span>
                          </>
                        )}
                        {inv.voidedAtUtc && (
                          <>
                            {" · "}
                            <span className="text-[var(--color-destructive)]">
                              {t("label.voided")} {formatDate(inv.voidedAtUtc)}
                            </span>
                          </>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Amount column */}
                  <div className="text-right">
                    <div className="text-display text-base font-semibold tabular-nums">
                      {formatCurrency(inv.subtotalAmount, inv.currency)}
                    </div>
                    {inv.dueAtUtc && inv.status === "Issued" && (
                      <div className="font-mono text-[11px] text-[var(--color-warning)]">
                        {t("label.due")} {formatDate(inv.dueAtUtc)}
                      </div>
                    )}
                  </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      <div className="flex items-center justify-between text-sm">
        <div className="font-mono text-[11px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          {data
            ? t("pagination.pageOf", {
                page: data.pageNumber,
                total: Math.max(data.totalPages, 1),
              })
            : ""}
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasPrevious || query.isFetching}
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft className="mr-1 h-4 w-4" /> {t("pagination.previous")}
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasNext || query.isFetching}
            onClick={() => setPageNumber((p) => p + 1)}
          >
            {t("pagination.next")} <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
