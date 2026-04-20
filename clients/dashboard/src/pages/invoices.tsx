import { useQuery } from "@tanstack/react-query";
import { getMyInvoices, type InvoiceDto } from "@/api/billing";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

function StatusPill({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        status === "Paid" && "bg-emerald-500/15 text-emerald-500",
        status === "Issued" && "bg-sky-500/15 text-sky-500",
        status === "Draft" && "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        status === "Void" && "bg-[var(--color-destructive)]/15 text-[var(--color-destructive)]",
      )}
    >
      {status}
    </span>
  );
}

export function InvoicesPage() {
  const query = useQuery({
    queryKey: ["billing", "invoices", "me"],
    queryFn: getMyInvoices,
  });

  const invoices: InvoiceDto[] = query.data ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Invoices</h1>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Your tenant's billing history.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All invoices</CardTitle>
          <CardDescription>
            {query.data ? `${invoices.length} total` : "Loading…"}
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {query.isError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {query.error instanceof ApiRequestError
                ? query.error.problem?.detail ?? query.error.message
                : "Failed to load invoices."}
            </div>
          )}
          {query.isLoading ? (
            <div className="px-6 py-8 text-center text-sm text-[var(--color-muted-foreground)]">
              Loading…
            </div>
          ) : invoices.length === 0 ? (
            <div className="px-6 py-8 text-center text-sm text-[var(--color-muted-foreground)]">
              No invoices yet.
            </div>
          ) : (
            <ul className="divide-y divide-[var(--color-border)]">
              {invoices.map((invoice) => (
                <li key={invoice.id} className="flex items-center justify-between px-6 py-4">
                  <div>
                    <div className="font-medium">{invoice.invoiceNumber}</div>
                    <div className="text-xs text-[var(--color-muted-foreground)]">
                      {invoice.periodYear}-{String(invoice.periodMonth).padStart(2, "0")} · created{" "}
                      {new Date(invoice.createdAtUtc).toLocaleDateString()}
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <StatusPill status={invoice.status} />
                    <div className="text-right">
                      <div className="font-medium tabular-nums">
                        {formatMoney(invoice.subtotalAmount, invoice.currency)}
                      </div>
                      {invoice.paidAtUtc && (
                        <div className="text-xs text-[var(--color-muted-foreground)]">
                          paid {new Date(invoice.paidAtUtc).toLocaleDateString()}
                        </div>
                      )}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
