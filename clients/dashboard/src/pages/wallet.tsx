import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { AlertTriangle, Receipt, Send, Wallet } from "lucide-react";
import { toast } from "sonner";
import {
  createTopupRequest,
  getMyTopupRequests,
  getMyWallet,
  type CreateTopupRequestInput,
  type TopupRequestDto,
  type TopupRequestStatus,
} from "@/api/wallet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
  EntityStatusBadge,
  ErrorBand,
  Field,
  ToneIconTile,
  type EntityStatusTone,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate, formatMoney } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

// Below this balance the wallet card surfaces a low-balance hint nudging the
// tenant to top up. A balance of 0 (or negative, defensively) always warns.
const LOW_BALANCE_THRESHOLD = 10;

const WALLET_QUERY_KEY = ["billing", "wallet", "me"] as const;
const TOPUP_REQUESTS_QUERY_KEY = ["billing", "topup-requests", "me"] as const;

const DESKTOP_GRID = "grid-cols-[1fr_140px_130px_150px]";

// ────────────────────────────────────────────────────────────────────
// Pure helpers — module scope so they're not re-allocated each render.
// ────────────────────────────────────────────────────────────────────

function statusTone(status: TopupRequestStatus): EntityStatusTone {
  switch (status) {
    case "Pending":
      return "warning";
    case "Invoiced":
      return "info";
    case "Completed":
      return "success";
    case "Rejected":
      return "danger";
    case "Cancelled":
    default:
      return "default";
  }
}

// ────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────

export function WalletPage() {
  const [pageNumber, setPageNumber] = useState(1);

  const walletQuery = useQuery({
    queryKey: WALLET_QUERY_KEY,
    queryFn: getMyWallet,
    staleTime: 30_000,
  });

  const requestsQuery = useQuery({
    queryKey: [...TOPUP_REQUESTS_QUERY_KEY, { pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => getMyTopupRequests({ pageNumber, pageSize: PAGE_SIZE }),
    staleTime: 30_000,
    placeholderData: keepPreviousData,
  });

  const wallet = walletQuery.data;
  const requests = requestsQuery.data?.items ?? [];
  const totalPages = requestsQuery.data?.totalPages ?? 1;

  const walletError =
    walletQuery.error != null ? describe(walletQuery.error) : null;
  const requestsError =
    requestsQuery.error != null ? describe(requestsQuery.error) : null;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Wallet}
        title="WhatsApp wallet"
        description="Your prepaid balance for WhatsApp messaging. Request a top-up and we'll raise an invoice to credit your wallet."
      />

      {walletError && <ErrorBand message={walletError} />}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1.2fr_1fr]">
        <BalanceCard
          loading={walletQuery.isLoading}
          balance={wallet?.balance ?? null}
          currency={wallet?.currency ?? "USD"}
        />
        <TopupRequestForm currency={wallet?.currency ?? "USD"} />
      </div>

      <section className="space-y-3">
        <h2 className="font-display text-[15px] font-semibold tracking-tight text-[var(--color-foreground)]">
          My top-up requests
        </h2>

        {requestsError && <ErrorBand message={requestsError} />}

        {requestsQuery.isLoading ? (
          <EntityListLoading desktopColumns={DESKTOP_GRID} rows={4} />
        ) : requests.length === 0 ? (
          <EntityEmpty
            icon={Wallet}
            title="No top-up requests yet"
            body="Once you request a top-up it will appear here with its status — pending, invoiced, or completed."
          />
        ) : (
          <div>
            {/* Mobile: card list */}
            <div className="space-y-2 md:hidden">
              {requests.map((request) => (
                <MobileCard key={request.id} request={request} />
              ))}
            </div>

            {/* Desktop: table */}
            <EntityListCard className="hidden md:block">
              <EntityListHeader className={DESKTOP_GRID}>
                <span>Requested</span>
                <span className="text-right">Amount</span>
                <span>Status</span>
                <span>Invoice</span>
              </EntityListHeader>
              {requests.map((request, i) => (
                <DesktopRow
                  key={request.id}
                  request={request}
                  isLast={i === requests.length - 1}
                />
              ))}
            </EntityListCard>

            <EntityPager
              page={requestsQuery.data?.pageNumber ?? pageNumber}
              totalPages={totalPages}
              hasPrev={pageNumber > 1}
              hasNext={pageNumber < totalPages}
              onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
              onNext={() => setPageNumber((p) => p + 1)}
            />
          </div>
        )}
      </section>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Balance card
// ────────────────────────────────────────────────────────────────────

function BalanceCard({
  loading,
  balance,
  currency,
}: {
  loading: boolean;
  balance: number | null;
  currency: string;
}) {
  const isLow = balance !== null && balance <= LOW_BALANCE_THRESHOLD;

  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 shadow-xs">
      <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        <Wallet className="size-3.5" />
        Current balance
      </div>

      {loading ? (
        <div className="mt-3 h-10 w-40 animate-pulse rounded-lg bg-[oklch(from_var(--color-muted)_l_c_h_/_0.6)]" />
      ) : (
        <div className="mt-2 flex items-baseline gap-2">
          <span className="font-display text-[38px] font-semibold leading-none tracking-tight tabular-nums text-[var(--color-foreground)]">
            {formatMoney(balance ?? 0, currency)}
          </span>
        </div>
      )}

      {!loading && isLow && (
        <div className="mt-4 flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-warning)_l_c_h_/_0.25)] bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] px-3 py-2 text-[12.5px] text-[var(--color-warning)]">
          <AlertTriangle className="mt-0.5 size-3.5 shrink-0" />
          <span>
            {balance !== null && balance <= 0
              ? "Your wallet is empty. Request a top-up to keep sending WhatsApp messages."
              : "Your balance is running low. Consider requesting a top-up soon."}
          </span>
        </div>
      )}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Request top-up form — plain controlled inputs (dashboard convention).
// ────────────────────────────────────────────────────────────────────

function TopupRequestForm({ currency }: { currency: string }) {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");

  const mutation = useMutation({
    mutationFn: (input: CreateTopupRequestInput) => createTopupRequest(input),
    onSuccess: () => {
      toast.success("Top-up requested", {
        description: "We'll raise an invoice to credit your wallet shortly.",
      });
      queryClient.invalidateQueries({ queryKey: WALLET_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: TOPUP_REQUESTS_QUERY_KEY });
      setAmount("");
      setNote("");
    },
    onError: (err) =>
      toast.error("Top-up request failed", { description: describe(err) }),
  });

  const amountNum = Number(amount);
  const valid = amount.trim().length > 0 && !Number.isNaN(amountNum) && amountNum > 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    // Pass per-call data through mutate(arg) — never via state the mutationFn
    // closes over (execute-time race; see project_react_mutation_closure_race).
    mutation.mutate({
      amount: amountNum,
      note: note.trim() ? note.trim() : undefined,
    });
  };

  return (
    <div className="rounded-2xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 shadow-xs">
      <div className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        <Send className="size-3.5" />
        Request a top-up
      </div>

      <form onSubmit={onSubmit} className="mt-4 space-y-4">
        <Field
          id="topup-amount"
          label="Amount"
          required
          hint={`How much to add to your wallet, in ${currency}.`}
        >
          <Input
            id="topup-amount"
            type="number"
            inputMode="decimal"
            step="0.01"
            min="0"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="100.00"
            className="tabular-nums"
            required
          />
        </Field>

        <Field
          id="topup-note"
          label="Note"
          hint="Optional — anything we should know about this top-up."
        >
          <textarea
            id="topup-note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            rows={3}
            maxLength={1000}
            placeholder="e.g. budget for the June campaign"
            className={cn(
              "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
              "placeholder:text-[var(--color-muted-foreground)]",
              "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
            )}
          />
        </Field>

        <Button
          type="submit"
          disabled={mutation.isPending || !valid}
          className="w-full gap-1.5"
        >
          <Send className="size-4" />
          {mutation.isPending ? "Submitting…" : "Request top-up"}
        </Button>
      </form>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Subcomponents — request rows
// ────────────────────────────────────────────────────────────────────

function MobileCard({ request }: { request: TopupRequestDto }) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <ToneIconTile icon={Wallet} tone="primary" size="md" className="rounded-xl" />
          <div className="min-w-0">
            <div className="font-display text-[15px] font-semibold tabular-nums text-[var(--color-foreground)]">
              {formatMoney(request.amount, request.currency)}
            </div>
            <p className="mt-0.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {formatDate(request.createdAtUtc)}
            </p>
          </div>
        </div>
        <div className="flex flex-col items-end gap-1.5">
          <EntityStatusBadge tone={statusTone(request.status)}>
            {request.status}
          </EntityStatusBadge>
          {request.invoiceId && (
            <Link
              to={`/invoices/${request.invoiceId}`}
              className="inline-flex items-center gap-1 text-[11px] font-medium text-[var(--color-primary)] hover:underline"
            >
              <Receipt className="size-3" />
              Invoice
            </Link>
          )}
        </div>
      </div>
      {request.note && (
        <p className="mt-2 line-clamp-2 text-[12px] text-[var(--color-muted-foreground)]">
          {request.note}
        </p>
      )}
    </div>
  );
}

function DesktopRow({
  request,
  isLast,
}: {
  request: TopupRequestDto;
  isLast: boolean;
}) {
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
      {/* Requested date + optional note */}
      <div className="min-w-0">
        <span className="block text-[13px] text-[var(--color-foreground)]">
          {formatDate(request.createdAtUtc)}
        </span>
        {request.note && (
          <span className="mt-0.5 block truncate text-[11px] text-[var(--color-muted-foreground)]">
            {request.note}
          </span>
        )}
      </div>

      {/* Amount */}
      <span className="text-right font-display text-[14px] font-semibold tabular-nums">
        {formatMoney(request.amount, request.currency)}
      </span>

      {/* Status */}
      <span>
        <EntityStatusBadge tone={statusTone(request.status)}>
          {request.status}
        </EntityStatusBadge>
      </span>

      {/* Invoice link */}
      <span className="text-[12px] text-[var(--color-muted-foreground)]">
        {request.invoiceId ? (
          <Link
            to={`/invoices/${request.invoiceId}`}
            className="inline-flex items-center gap-1 font-medium text-[var(--color-primary)] hover:underline"
          >
            <Receipt className="size-3.5" />
            View invoice
          </Link>
        ) : (
          "—"
        )}
      </span>
    </EntityListRow>
  );
}
