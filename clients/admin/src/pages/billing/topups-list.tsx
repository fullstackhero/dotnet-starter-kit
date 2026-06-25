import { useMemo, useState } from "react";
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
import { useAuth } from "@/auth/use-auth";
import { BillingPermissions } from "@/lib/permissions";

const PAGE_SIZE = 20;

const STATUSES: TopupRequestStatus[] = ["Pending", "Approved", "Rejected", "Completed"];

// ─── helpers ─────────────────────────────────────────────────────────

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

const dateShort = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "2-digit",
  year: "numeric",
});

function formatDate(iso?: string | null) {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : dateShort.format(d);
}

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
      toast.success("Invoice generated", {
        description: "A draft invoice was created for this top-up.",
        action: invoiceId
          ? {
              label: "View invoice",
              onClick: () => navigate(`/billing/invoices/${invoiceId}`),
            }
          : undefined,
      });
      invalidate();
      closeAction();
    },
    onError: (err) =>
      toast.error("Approve failed", { description: describe(err, "Could not approve the request.") }),
  });

  const rejectMutation = useMutation({
    mutationFn: (vars: { id: string; reason?: string }) =>
      rejectTopupRequest(vars.id, vars.reason),
    onSuccess: () => {
      toast.success("Request rejected");
      invalidate();
      closeAction();
    },
    onError: (err) =>
      toast.error("Reject failed", { description: describe(err, "Could not reject the request.") }),
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
          label="Page requests"
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : data?.items.length ?? 0}
          subtitle={data ? `${data.totalCount.toLocaleString()} total` : "loading…"}
        />
        <KpiTile
          label="Pending"
          value={query.isLoading ? <Skeleton className="h-7 w-12" /> : totals.pendingCount}
          subtitle="awaiting decision (this page)"
        />
        <KpiTile
          label="Requested"
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatMoney(totals.requested, totals.currency)
            )
          }
          subtitle="this page"
        />
      </div>

      {/* Filter panel */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between gap-3">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Filter className="h-4 w-4 text-[var(--color-muted-foreground)]" />
              <span>Filters</span>
            </CardTitle>
            <CardDescription>
              Review wallet top-up requests across every tenant. Approve to generate an invoice.
            </CardDescription>
          </div>
          {filtersDirty && (
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              <X className="mr-1 h-3.5 w-3.5" /> Clear
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="filter-tenant">Tenant</Label>
            <Input
              id="filter-tenant"
              placeholder="tenant identifier"
              value={tenantFilter}
              onChange={(e) => {
                setTenantFilter(e.target.value);
                setPageNumber(1);
              }}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="filter-status">Status</Label>
            <Select
              id="filter-status"
              value={statusFilter}
              onValueChange={(v) => {
                setStatusFilter(v as TopupRequestStatus | "");
                setPageNumber(1);
              }}
              options={STATUSES.map((s) => ({ value: s, label: s }))}
              emptyLabel="All"
            />
          </div>
        </CardContent>
      </Card>

      {/* List */}
      <Card>
        <CardHeader>
          <CardTitle>Top-up requests</CardTitle>
          <CardDescription>
            {data ? (
              <>
                Page <span className="tabular-nums">{data.pageNumber}</span> of{" "}
                <span className="tabular-nums">{Math.max(data.totalPages, 1)}</span> ·{" "}
                <span className="tabular-nums">{data.totalCount.toLocaleString()}</span> total
              </>
            ) : (
              "Loading…"
            )}
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {query.isError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {describe(query.error, "Failed to load top-up requests.")}
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
              No top-up requests match the current filters.
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
                          {formatMoney(req.amount, req.currency)}
                        </span>
                        <Badge variant={statusVariant(req.status)}>{req.status}</Badge>
                        {req.invoiceId && (
                          <button
                            type="button"
                            onClick={() => navigate(`/billing/invoices/${req.invoiceId}`)}
                            className="inline-flex items-center gap-1 rounded font-mono text-[11px] text-[var(--color-primary)] underline-offset-2 hover:underline"
                          >
                            <Receipt className="h-3 w-3" /> invoice
                          </button>
                        )}
                      </div>
                      <div className="mt-1 truncate font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                        tenant <span className="text-[var(--color-foreground)]">{req.tenantId}</span> ·
                        created {formatDate(req.createdAtUtc)}
                        {req.decidedAtUtc && ` · decided ${formatDate(req.decidedAtUtc)}`}
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
                        <Check className="mr-1 h-3.5 w-3.5" /> Approve
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
                        <X className="mr-1 h-3.5 w-3.5" /> Reject
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
          {data ? `Page ${data.pageNumber} / ${Math.max(data.totalPages, 1)}` : ""}
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasPrevious || query.isFetching}
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft className="mr-1 h-4 w-4" /> Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasNext || query.isFetching}
            onClick={() => setPageNumber((p) => p + 1)}
          >
            Next <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Approve / Reject confirmation */}
      <ConfirmDialog
        open={action !== null}
        onOpenChange={(open) => {
          if (!open) closeAction();
        }}
        title={action?.mode === "reject" ? "Reject top-up request" : "Approve top-up request"}
        destructive={action?.mode === "reject"}
        confirmLabel={action?.mode === "reject" ? "Reject" : "Approve & generate invoice"}
        pending={actionPending}
        onConfirm={confirmAction}
        description={
          action ? (
            <div className="space-y-3">
              <p>
                {action.mode === "reject" ? (
                  <>
                    Reject the{" "}
                    <span className="font-semibold">
                      {formatMoney(action.request.amount, action.request.currency)}
                    </span>{" "}
                    top-up for tenant{" "}
                    <span className="font-mono">{action.request.tenantId}</span>? This cannot be undone.
                  </>
                ) : (
                  <>
                    Approve the{" "}
                    <span className="font-semibold">
                      {formatMoney(action.request.amount, action.request.currency)}
                    </span>{" "}
                    top-up for tenant{" "}
                    <span className="font-mono">{action.request.tenantId}</span>? An invoice will be
                    generated for the operator to mark paid.
                  </>
                )}
              </p>
              <div className="space-y-1.5">
                <Label htmlFor="decision-note">
                  {action.mode === "reject" ? "Reason" : "Note"}{" "}
                  <span className="text-[var(--color-muted-foreground)]">(optional)</span>
                </Label>
                <Input
                  id="decision-note"
                  placeholder={action.mode === "reject" ? "duplicate · invalid · …" : "internal note"}
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
