import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Ban, CheckCircle2, Send } from "lucide-react";
import { toast } from "sonner";
import {
  getInvoice,
  issueInvoice,
  markInvoicePaid,
  voidInvoice,
  type InvoiceDto,
  type InvoiceStatus,
  type InvoiceLineItemDto,
} from "@/api/billing";
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
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

// ─── helpers ─────────────────────────────────────────────────────────

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

const dateLong = new Intl.DateTimeFormat(undefined, {
  month: "long",
  day: "numeric",
  year: "numeric",
});
function formatDate(iso?: string | null) {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : dateLong.format(d);
}

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

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

// ─── component ───────────────────────────────────────────────────────

export function InvoiceDetailPage() {
  const { invoiceId = "" } = useParams<{ invoiceId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["billing", "invoice", invoiceId],
    queryFn: () => getInvoice(invoiceId),
    enabled: !!invoiceId,
  });
  const invoice = query.data;

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["billing", "invoice", invoiceId] });
    queryClient.invalidateQueries({ queryKey: ["billing", "invoices"] });
  };

  // ── state-machine mutations ────────────────────────────────────────

  const [dueAt, setDueAt] = useState("");
  const [voidReason, setVoidReason] = useState("");

  const issueMutation = useMutation({
    mutationFn: () => issueInvoice(invoiceId, dueAt ? new Date(dueAt).toISOString() : null),
    onSuccess: () => {
      toast.success("Invoice issued", { description: "Status moved to Issued." });
      setDueAt("");
      invalidate();
    },
    onError: (err) => toast.error("Issue failed", { description: describe(err, "Could not issue invoice.") }),
  });

  const payMutation = useMutation({
    mutationFn: () => markInvoicePaid(invoiceId),
    onSuccess: () => {
      toast.success("Marked paid");
      invalidate();
    },
    onError: (err) => toast.error("Mark-paid failed", { description: describe(err, "Could not mark paid.") }),
  });

  const voidMutation = useMutation({
    mutationFn: () => voidInvoice(invoiceId, voidReason.trim() ? voidReason.trim() : null),
    onSuccess: () => {
      toast.success("Invoice voided");
      setVoidReason("");
      invalidate();
    },
    onError: (err) => toast.error("Void failed", { description: describe(err, "Could not void invoice.") }),
  });

  // ── render ─────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      <div>
        <Button variant="ghost" size="sm" onClick={() => navigate("/billing/invoices")} className="-ml-2 mb-2">
          <ArrowLeft className="mr-1 h-4 w-4" /> All invoices
        </Button>

        {query.isLoading ? (
          <div className="space-y-2">
            <Skeleton className="h-7 w-72" />
            <Skeleton className="h-4 w-96" />
          </div>
        ) : query.isError ? (
          <div className="text-sm text-[var(--color-destructive)]">
            {describe(query.error, "Failed to load invoice.")}
          </div>
        ) : invoice ? (
          <InvoiceHero invoice={invoice} />
        ) : null}
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        {/* Line items */}
        <Card>
          <CardHeader>
            <CardTitle>Line items</CardTitle>
            <CardDescription>
              {invoice ? `${invoice.lineItems.length} line${invoice.lineItems.length === 1 ? "" : "s"}` : "Loading…"}
            </CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            {query.isLoading ? (
              <ul className="divide-y divide-[var(--color-border)]">
                {Array.from({ length: 2 }).map((_, i) => (
                  <li key={i} className="px-6 py-4">
                    <Skeleton className="h-4 w-1/2" />
                    <Skeleton className="mt-2 h-3 w-1/4" />
                  </li>
                ))}
              </ul>
            ) : invoice && invoice.lineItems.length === 0 ? (
              <div className="px-6 py-8 text-center text-sm text-[var(--color-muted-foreground)]">
                No line items.
              </div>
            ) : invoice ? (
              <ul>
                {invoice.lineItems.map((li, i) => (
                  <LineItemRow key={li.id} item={li} currency={invoice.currency} delayIndex={i} />
                ))}
                <li className="grid grid-cols-[1fr_auto] items-baseline gap-x-6 border-t-2 border-[var(--color-border-strong)] px-6 py-4">
                  <div className="font-mono text-[11px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                    subtotal
                  </div>
                  <div className="text-display text-xl font-semibold tabular-nums">
                    {formatMoney(invoice.subtotalAmount, invoice.currency)}
                  </div>
                </li>
              </ul>
            ) : null}
          </CardContent>
        </Card>

        {/* Actions side panel */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Lifecycle actions</CardTitle>
              <CardDescription>
                Each action enforces the invoice state machine on the server.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {invoice && (
                <>
                  {/* Issue */}
                  <div
                    className={cn(
                      "rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3",
                      invoice.status !== "Draft" && "opacity-60",
                    )}
                  >
                    <div className="mb-2 flex items-center gap-2">
                      <Send className="h-3.5 w-3.5 text-[var(--color-info)]" />
                      <span className="text-sm font-medium">Issue</span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                        from draft
                      </span>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="dueAt" className="text-xs text-[var(--color-muted-foreground)]">
                        Due date (optional)
                      </Label>
                      <Input
                        id="dueAt"
                        type="date"
                        value={dueAt}
                        onChange={(e) => setDueAt(e.target.value)}
                        disabled={invoice.status !== "Draft" || issueMutation.isPending}
                      />
                      <p className="text-[11px] text-[var(--color-muted-foreground)]">
                        Leave blank for the server default of +14 days from issue time.
                      </p>
                      <Button
                        size="sm"
                        disabled={invoice.status !== "Draft" || issueMutation.isPending}
                        onClick={() => issueMutation.mutate()}
                        className="w-full"
                      >
                        {issueMutation.isPending ? "Issuing…" : "Issue invoice"}
                      </Button>
                    </div>
                  </div>

                  {/* Mark paid */}
                  <div
                    className={cn(
                      "rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3",
                      invoice.status !== "Issued" && "opacity-60",
                    )}
                  >
                    <div className="mb-2 flex items-center gap-2">
                      <CheckCircle2 className="h-3.5 w-3.5 text-[var(--color-success)]" />
                      <span className="text-sm font-medium">Mark paid</span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                        from issued
                      </span>
                    </div>
                    <p className="text-[11px] text-[var(--color-muted-foreground)]">
                      Records manual payment receipt. Idempotent.
                    </p>
                    <Button
                      size="sm"
                      disabled={invoice.status !== "Issued" || payMutation.isPending}
                      onClick={() => payMutation.mutate()}
                      className="mt-2 w-full"
                    >
                      {payMutation.isPending ? "Saving…" : "Mark as paid"}
                    </Button>
                  </div>

                  {/* Void */}
                  <div
                    className={cn(
                      "rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] p-3",
                      invoice.status === "Paid" || invoice.status === "Void"
                        ? "opacity-60"
                        : null,
                    )}
                  >
                    <div className="mb-2 flex items-center gap-2">
                      <Ban className="h-3.5 w-3.5 text-[var(--color-destructive)]" />
                      <span className="text-sm font-medium">Void</span>
                      <span className="font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                        from draft / issued
                      </span>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="voidReason" className="text-xs text-[var(--color-muted-foreground)]">
                        Reason (optional, appended to notes)
                      </Label>
                      <Input
                        id="voidReason"
                        placeholder="duplicate · disputed · …"
                        value={voidReason}
                        onChange={(e) => setVoidReason(e.target.value)}
                        disabled={
                          invoice.status === "Paid" ||
                          invoice.status === "Void" ||
                          voidMutation.isPending
                        }
                      />
                      <Button
                        variant="destructive"
                        size="sm"
                        disabled={
                          invoice.status === "Paid" ||
                          invoice.status === "Void" ||
                          voidMutation.isPending
                        }
                        onClick={() => voidMutation.mutate()}
                        className="w-full"
                      >
                        {voidMutation.isPending ? "Voiding…" : "Void invoice"}
                      </Button>
                    </div>
                  </div>

                  {invoice.notes && (
                    <div className="rounded-md border border-[var(--color-border)] p-3">
                      <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                        notes
                      </div>
                      <p className="whitespace-pre-line text-xs text-[var(--color-foreground)]">
                        {invoice.notes}
                      </p>
                    </div>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function InvoiceHero({ invoice }: { invoice: InvoiceDto }) {
  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <code className="rounded bg-[var(--color-surface-2)] px-2 py-0.5 font-mono text-xs font-medium tracking-tight">
          {invoice.invoiceNumber}
        </code>
        <Badge variant={statusVariant(invoice.status)}>{invoice.status}</Badge>
      </div>
      <h2 className="font-display text-3xl font-semibold leading-tight tracking-tight">
        {formatMoney(invoice.subtotalAmount, invoice.currency)}
      </h2>
      <div className="font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
        tenant <span className="text-[var(--color-foreground)]">{invoice.tenantId}</span> ·
        period {formatPeriod(invoice.periodYear, invoice.periodMonth)} ·
        created {formatDate(invoice.createdAtUtc)}
        {invoice.issuedAtUtc && (
          <>
            {" · "}
            issued {formatDate(invoice.issuedAtUtc)}
          </>
        )}
        {invoice.dueAtUtc && invoice.status === "Issued" && (
          <>
            {" · "}
            <span className="text-[var(--color-warning)]">due {formatDate(invoice.dueAtUtc)}</span>
          </>
        )}
        {invoice.paidAtUtc && (
          <>
            {" · "}
            <span className="text-[var(--color-success)]">paid {formatDate(invoice.paidAtUtc)}</span>
          </>
        )}
        {invoice.voidedAtUtc && (
          <>
            {" · "}
            <span className="text-[var(--color-destructive)]">voided {formatDate(invoice.voidedAtUtc)}</span>
          </>
        )}
      </div>
    </div>
  );
}

function LineItemRow({
  item,
  currency,
  delayIndex,
}: {
  item: InvoiceLineItemDto;
  currency: string;
  delayIndex: number;
}) {
  return (
    <li
      className="fsh-enter grid grid-cols-[1fr_auto] items-baseline gap-x-6 border-t border-[var(--color-border)] px-6 py-3 first:border-t-0"
      style={{ animationDelay: `${Math.min(delayIndex, 6) * 30}ms` }}
    >
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-medium">{item.description}</span>
          <Badge variant={item.kind === "BaseFee" ? "default" : item.kind === "Overage" ? "warning" : "muted"}>
            {item.kind}
          </Badge>
          {item.resource && (
            <span className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {item.resource}
            </span>
          )}
        </div>
        <div className="mt-1 font-mono text-[11px] text-[var(--color-muted-foreground)] tabular-nums">
          {item.quantity.toLocaleString()} × {formatMoney(item.unitPrice, currency)}
        </div>
      </div>
      <div className="text-right text-sm font-semibold tabular-nums">
        {formatMoney(item.amount, currency)}
      </div>
    </li>
  );
}
