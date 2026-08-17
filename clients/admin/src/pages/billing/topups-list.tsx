import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
  keepPreviousData,
} from "@tanstack/react-query";
import {
  Check,
  ChevronLeft,
  ChevronRight,
  Filter,
  Receipt,
  Wallet,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  approveTopupRequest,
  listTopupRequests,
  rejectTopupRequest,
  type TopupRequestDto,
  type TopupRequestStatus,
} from "@/api/wallet";
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
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Select } from "@/components/list";
import { KpiTile } from "@/components/kpi-tile";
import { ApiRequestError } from "@/lib/api-client";
import { formatCurrency, formatDate } from "@/lib/format";
import { useAuth } from "@/auth/use-auth";
import { BillingPermissions } from "@/lib/permissions";

const PAGE_SIZE = 20;

const STATUSES: TopupRequestStatus[] = ["Pending", "Approved", "Rejected", "Completed"];

// ─── helpers ─────────────────────────────────────────────────────────

function statusVariant(status: TopupRequestStatus): React.ComponentProps<typeof Badge>["variant"] {
  switch (status) {
    case "Completed":
      return "success";
    case "Approved":
      return "info";
    case "Pending":
      return "warning";
    case "Rejected":
      return "danger";
    default:
      return "default";
  }
}

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

type ActionTarget = { request: TopupRequestDto; mode: "approve" | "reject" };

// ─── component ───────────────────────────────────────────────────────

export function TopupsListPage() {
  const { t } = useTranslation("billing");
  const statusLabel = (status: TopupRequestStatus): string =>
    t(`status.${status.charAt(0).toLowerCase()}${status.slice(1)}`);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: currentUser } = useAuth();
  // Approve / reject mutate the request and generate invoices — gated by Billing.Manage.
  const canManageBilling = (currentUser?.permissions ?? []).includes(BillingPermissions.Manage);

  const [pageNumber, setPageNumber] = useState(1);
  const [tenantFilter, setTenantFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState<TopupRequestStatus | "">("Pending");

  const filters = useMemo(
    () => ({
      tenantId: tenantFilter.trim() || undefined,
      status: statusFilter || undefined,
    }),
    [tenantFilter, statusFilter],
  );

  const query = useQuery({
    queryKey: ["billing", "topup-requests", { pageNumber, ...filters }],
    queryFn: () => listTopupRequests({ pageNumber, pageSize: PAGE_SIZE, ...filters }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = useMemo<TopupRequestDto[]>(() => data?.items ?? [], [data]);

  const totals = useMemo(() => {
    let pendingCount = 0;
    let requested = 0;
    const firstCurrency = items[0]?.currency ?? "USD";
    for (const req of items) {
      requested += req.amount;
      if (req.status === "Pending") pendingCount += 1;
    }
    return { pendingCount, requested, currency: firstCurrency };
  }, [items]);

  const filtersDirty = !!tenantFilter || statusFilter !== "Pending";

  const clearFilters = () => {
    setTenantFilter("");
    setStatusFilter("Pending");
    setPageNumber(1);
  };

  // ── approve / reject mutations ─────────────────────────────────────

  const [action, setAction] = useState<ActionTarget | null>(null);
  const [decisionNote, setDecisionNote] = useState("");

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["billing", "topup-requests"] });

  const closeAction = () => {
    setAction(null);
    setDecisionNote("");
  };

  // Pass id + note/reason through mutate(arg); never close over `action`/`decisionNote`
  // state, which could be stale between render and execute-time.
  const approveMutation = useMutation({
    mutationFn: (vars: { id: string; note?: string }) =>
      approveTopupRequest(vars.id, vars.note),
    onSuccess: (invoiceId) => {
      toast.success(t("topups.toast.invoiceGenerated"), {
        description: t("topups.toast.invoiceGeneratedDesc"),
        action: invoiceId
          ? {
              label: t("topups.toast.viewInvoice"),
              onClick: () => navigate(`/billing/invoices/${invoiceId}`),
            }
          : undefined,
      });
      invalidate();
      closeAction();
    },
    onError: (err) =>
      toast.error(t("topups.toast.approveFailed"), { description: describe(err, t("topups.toast.approveFailedDesc")) }),
  });

  const rejectMutation = useMutation({
    mutationFn: (vars: { id: string; reason?: string }) =>
      rejectTopupRequest(vars.id, vars.reason),
    onSuccess: () => {
      toast.success(t("topups.toast.rejected"));
      invalidate();
      closeAction();
    },
    onError: (err) =>
      toast.error(t("topups.toast.rejectFailed"), { description: describe(err, t("topups.toast.rejectFailedDesc")) }),
  });

  const actionPending = approveMutation.isPending || rejectMutation.isPending;

  const confirmAction = () => {
    if (!action) return;
    if (action.mode === "approve") {
      approveMutation.mutate({ id: action.request.id, note: decisionNote });
    } else {
      rejectMutation.mutate({ id: action.request.id, reason: decisionNote });
    }
  };

  return (
    <div className="space-y-6">
      {/* KPI strip — page-scope (current page, not all-time) */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <KpiTile
          label={t("topups.kpi.pageRequests")}
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : data?.items.length ?? 0}
          subtitle={
            data
              ? t("topups.kpi.totalCount", { count: data.totalCount })
              : t("topups.kpi.loading")
          }
        />
        <KpiTile
          label={t("topups.kpi.pending")}
          value={query.isLoading ? <Skeleton className="h-7 w-12" /> : totals.pendingCount}
          subtitle={t("topups.kpi.pendingHint")}
        />
        <KpiTile
          label={t("topups.kpi.requested")}
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatCurrency(totals.requested, totals.currency)
            )
          }
          subtitle={t("topups.kpi.thisPage")}
        />
      </div>

      {/* Filter panel */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between gap-3">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Filter className="h-4 w-4 text-[var(--color-muted-foreground)]" />
              <span>{t("topups.filters.title")}</span>
            </CardTitle>
            <CardDescription>{t("topups.filters.description")}</CardDescription>
          </div>
          {filtersDirty && (
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              <X className="mr-1 h-3.5 w-3.5" /> {t("invoices.filters.clear")}
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="filter-tenant">{t("topups.filters.tenant")}</Label>
            <Input
              id="filter-tenant"
              placeholder={t("topups.filters.tenantPlaceholder")}
              value={tenantFilter}
              onChange={(e) => {
                setTenantFilter(e.target.value);
                setPageNumber(1);
              }}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="filter-status">{t("topups.filters.status")}</Label>
            <Select
              id="filter-status"
              value={statusFilter}
              onValueChange={(v) => {
                setStatusFilter(v as TopupRequestStatus | "");
                setPageNumber(1);
              }}
              options={STATUSES.map((s) => ({ value: s, label: statusLabel(s) }))}
              emptyLabel={t("status.all")}
            />
          </div>
        </CardContent>
      </Card>

      {/* List */}
      <Card>
        <CardHeader>
          <CardTitle>{t("topups.list.title")}</CardTitle>
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
              {describe(query.error, t("topups.loadError"))}
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
              {t("topups.list.empty")}
            </div>
          ) : (
            <ul>
              {items.map((req, i) => (
                <li
                  key={req.id}
                  className="fsh-enter grid grid-cols-[1fr_auto] items-center gap-x-6 gap-y-1 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
                  style={{ animationDelay: `${Math.min(i, 8) * 25}ms` }}
                >
                  {/* Identity column */}
                  <div className="flex min-w-0 items-center gap-3">
                    <span
                      aria-hidden
                      className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)] ring-1 ring-inset ring-[var(--color-border)]"
                    >
                      <Wallet className="h-4 w-4" />
                    </span>
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-display text-base font-semibold tabular-nums">
                          {formatCurrency(req.amount, req.currency)}
                        </span>
                        <Badge variant={statusVariant(req.status)}>{statusLabel(req.status)}</Badge>
                        {req.invoiceId && (
                          <button
                            type="button"
                            onClick={() => navigate(`/billing/invoices/${req.invoiceId}`)}
                            className="inline-flex items-center gap-1 rounded font-mono text-[11px] text-[var(--color-primary)] underline-offset-2 hover:underline"
                          >
                            <Receipt className="h-3 w-3" /> {t("topups.invoiceLink")}
                          </button>
                        )}
                      </div>
                      <div className="mt-1 truncate font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                        {t("label.tenant")}{" "}
                        <span className="text-[var(--color-foreground)]">{req.tenantId}</span> ·{" "}
                        {t("label.created")} {formatDate(req.createdAtUtc)}
                        {req.decidedAtUtc && ` · ${t("label.decided")} ${formatDate(req.decidedAtUtc)}`}
                        {req.note && ` · “${req.note}”`}
                      </div>
                    </div>
                  </div>

                  {/* Actions column */}
                  {canManageBilling && req.status === "Pending" && (
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        disabled={actionPending}
                        onClick={() => {
                          setDecisionNote("");
                          setAction({ request: req, mode: "approve" });
                        }}
                      >
                        <Check className="mr-1 h-3.5 w-3.5" /> {t("topups.approve")}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={actionPending}
                        onClick={() => {
                          setDecisionNote("");
                          setAction({ request: req, mode: "reject" });
                        }}
                      >
                        <X className="mr-1 h-3.5 w-3.5" /> {t("topups.reject")}
                      </Button>
                    </div>
                  )}
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

      {/* Approve / Reject confirmation */}
      <ConfirmDialog
        open={action !== null}
        onOpenChange={(open) => {
          if (!open) closeAction();
        }}
        title={action?.mode === "reject" ? t("topups.confirm.rejectTitle") : t("topups.confirm.approveTitle")}
        destructive={action?.mode === "reject"}
        confirmLabel={action?.mode === "reject" ? t("topups.confirm.rejectConfirm") : t("topups.confirm.approveConfirm")}
        pending={actionPending}
        onConfirm={confirmAction}
        description={
          action ? (
            <div className="space-y-3">
              <p>
                {action.mode === "reject"
                  ? t("topups.confirm.rejectBody", {
                      amount: formatCurrency(action.request.amount, action.request.currency),
                      tenant: action.request.tenantId,
                    })
                  : t("topups.confirm.approveBody", {
                      amount: formatCurrency(action.request.amount, action.request.currency),
                      tenant: action.request.tenantId,
                    })}
              </p>
              <div className="space-y-1.5">
                <Label htmlFor="decision-note">
                  {action.mode === "reject" ? t("topups.confirm.reason") : t("topups.confirm.note")}{" "}
                  <span className="text-[var(--color-muted-foreground)]">{t("topups.confirm.optional")}</span>
                </Label>
                <Input
                  id="decision-note"
                  placeholder={action.mode === "reject" ? t("topups.confirm.rejectPlaceholder") : t("topups.confirm.notePlaceholder")}
                  value={decisionNote}
                  onChange={(e) => setDecisionNote(e.target.value)}
                  disabled={actionPending}
                  autoComplete="off"
                />
              </div>
            </div>
          ) : null
        }
      />
    </div>
  );
}
