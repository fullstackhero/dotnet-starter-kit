import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { Download, FileText, Receipt } from "lucide-react";
import {
  downloadInvoicePdf,
  getMyInvoice,
  type InvoiceDto,
  type InvoiceLineItemDto,
  type InvoiceStatus,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  EntityDetailBack,
  EntityDetailSection,
  EntityStatusBadge,
  ErrorBand,
  type EntityStatusTone,
} from "@/components/list";
import { describe, formatDate, formatMoney } from "@/lib/list-helpers";

// ────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────

function statusTone(status: InvoiceStatus): EntityStatusTone {
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

export function InvoiceDetailPage() {
  const { t } = useTranslation("subscription");
  const { id = "" } = useParams<{ id: string }>();

  const query = useQuery({
    queryKey: ["billing", "invoices", id],
    queryFn: () => getMyInvoice(id),
    enabled: !!id,
  });

  const invoice = query.data;

  return (
    <div className="pb-12">
      <EntityDetailBack to="/invoices" label={t("invoiceDetail.back")} />

      {query.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      {query.isLoading ? (
        <DetailSkeleton />
      ) : invoice ? (
        <InvoiceBody invoice={invoice} />
      ) : (
        <NotFoundPanel />
      )}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Body
// ────────────────────────────────────────────────────────────────────

function InvoiceBody({ invoice }: { invoice: InvoiceDto }) {
  const { t } = useTranslation("subscription");
  return (
    <div className="space-y-5">
      <InvoiceHeader invoice={invoice} />

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_300px]">
        {/* Left: line items + totals */}
        <EntityDetailSection title={t("invoiceDetail.lineItems")} icon={FileText} padded={false}>
          <LineItemsTable invoice={invoice} />
        </EntityDetailSection>

        {/* Right: meta + dates + notes */}
        <aside className="space-y-5">
          <EntityDetailSection title={t("invoiceDetail.details")} icon={Receipt}>
            <DetailsBody invoice={invoice} />
          </EntityDetailSection>

          {invoice.notes && (
            <EntityDetailSection title={t("invoiceDetail.notes")} icon={FileText}>
              <p className="whitespace-pre-wrap text-[13px] leading-relaxed text-[var(--color-foreground)]/90">
                {invoice.notes}
              </p>
            </EntityDetailSection>
          )}
        </aside>
      </div>
    </div>
  );
}

function InvoiceHeader({ invoice }: { invoice: InvoiceDto }) {
  const { t } = useTranslation("subscription");
  const [downloading, setDownloading] = useState(false);

  const onDownload = async () => {
    if (downloading) return;
    setDownloading(true);
    try {
      await downloadInvoicePdf(invoice.id, invoice.invoiceNumber);
    } catch (err) {
      toast.error(t("invoiceDetail.downloadFailed"), { description: describe(err) });
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div className="fsh-enter overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs">
      <div
        aria-hidden
        className="h-1 w-full"
        style={{
          background:
            "linear-gradient(90deg, var(--color-primary), oklch(from var(--color-primary) l c h / 0.8), var(--color-saffron))",
        }}
      />
      <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-start sm:justify-between sm:px-6">
        <div className="flex min-w-0 items-center gap-4">
          <span
            aria-hidden
            className="grid size-11 shrink-0 place-items-center rounded-xl bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)] text-[var(--color-primary)] sm:size-14"
          >
            <Receipt className="size-5 sm:size-6" />
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="truncate font-display text-[17px] font-bold leading-none tracking-tight text-[var(--color-foreground)] sm:text-[20px]">
                {invoice.invoiceNumber}
              </h1>
              <EntityStatusBadge tone={statusTone(invoice.status)}>
                {t(`invoices.status.${invoice.status}`)}
              </EntityStatusBadge>
              {invoice.purpose && (
                <EntityStatusBadge tone="default">{invoice.purpose}</EntityStatusBadge>
              )}
            </div>
            <p className="mt-1.5 text-[12px] text-[var(--color-muted-foreground)]">
              {t("invoiceDetail.periodLabel", {
                period: formatPeriod(invoice.periodYear, invoice.periodMonth),
              })}{" "}
              ·{" "}
              <span className="font-display font-semibold text-[var(--color-foreground)]">
                {formatMoney(invoice.subtotalAmount, invoice.currency)}
              </span>
            </p>
          </div>
        </div>

        <div className="flex shrink-0 items-center gap-1.5">
          <Button
            variant="outline"
            size="sm"
            onClick={onDownload}
            disabled={downloading}
            className="gap-1.5"
          >
            <Download className="size-3.5" />
            {downloading ? t("invoiceDetail.preparing") : t("invoiceDetail.downloadPdf")}
          </Button>
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Line items table
// ────────────────────────────────────────────────────────────────────

const LINE_GRID = "grid-cols-[1fr_80px_110px_110px] sm:grid-cols-[1fr_100px_130px_130px]";

function LineItemsTable({ invoice }: { invoice: InvoiceDto }) {
  const { t } = useTranslation("subscription");
  const items = invoice.lineItems ?? [];

  if (items.length === 0) {
    return (
      <div className="px-5 py-10 text-center">
        <p className="text-[13px] font-semibold text-[var(--color-foreground)]">
          {t("invoiceDetail.noLineItems")}
        </p>
        <p className="mt-1 text-[11.5px] text-[var(--color-muted-foreground)]">
          {t("invoiceDetail.noLineItemsBody")}
        </p>
      </div>
    );
  }

  return (
    <div>
      <div
        className={`grid ${LINE_GRID} items-center gap-3 border-b border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.4)] px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]`}
      >
        <span>{t("invoiceDetail.col.description")}</span>
        <span className="text-right">{t("invoiceDetail.col.qty")}</span>
        <span className="text-right">{t("invoiceDetail.col.unitPrice")}</span>
        <span className="text-right">{t("invoiceDetail.col.amount")}</span>
      </div>

      {items.map((item, i) => (
        <LineItemRow
          key={item.id}
          item={item}
          currency={invoice.currency}
          isLast={i === items.length - 1}
        />
      ))}

      {/* Total */}
      <div
        className={`grid ${LINE_GRID} items-center gap-3 border-t border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.25)] px-5 py-3.5`}
      >
        <span className="text-[12px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          {t("invoiceDetail.total")}
        </span>
        <span />
        <span />
        <span className="text-right font-display text-[15px] font-bold tabular-nums text-[var(--color-foreground)]">
          {formatMoney(invoice.subtotalAmount, invoice.currency)}
        </span>
      </div>
    </div>
  );
}

function LineItemRow({
  item,
  currency,
  isLast,
}: {
  item: InvoiceLineItemDto;
  currency: string;
  isLast: boolean;
}) {
  return (
    <div
      className={`grid ${LINE_GRID} items-center gap-3 px-5 py-3 ${
        isLast ? "" : "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]"
      }`}
    >
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <span className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
            {item.description}
          </span>
          <EntityStatusBadge tone="default">{item.kind}</EntityStatusBadge>
        </div>
        {item.resource && (
          <span className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {item.resource}
          </span>
        )}
      </div>
      <span className="text-right text-[13px] tabular-nums text-[var(--color-muted-foreground)]">
        {item.quantity}
      </span>
      <span className="text-right text-[13px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatMoney(item.unitPrice, currency)}
      </span>
      <span className="text-right font-display text-[13px] font-semibold tabular-nums text-[var(--color-foreground)]">
        {formatMoney(item.amount, currency)}
      </span>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Details / dates
// ────────────────────────────────────────────────────────────────────

function DetailsBody({ invoice }: { invoice: InvoiceDto }) {
  const { t } = useTranslation("subscription");
  return (
    <dl className="space-y-2.5 text-[12.5px]">
      <Row label={t("invoiceDetail.row.status")}>
        <EntityStatusBadge tone={statusTone(invoice.status)}>
          {t(`invoices.status.${invoice.status}`)}
        </EntityStatusBadge>
      </Row>
      <Row label={t("invoiceDetail.row.currency")}>{invoice.currency}</Row>
      <Row label={t("invoiceDetail.row.period")}>{formatPeriod(invoice.periodYear, invoice.periodMonth)}</Row>
      <Row label={t("invoiceDetail.row.created")}>{formatDate(invoice.createdAtUtc)}</Row>
      {invoice.issuedAtUtc && <Row label={t("invoiceDetail.row.issued")}>{formatDate(invoice.issuedAtUtc)}</Row>}
      {invoice.dueAtUtc && (
        <Row label={t("invoiceDetail.row.due")} tone={invoice.status === "Issued" ? "warning" : undefined}>
          {formatDate(invoice.dueAtUtc)}
        </Row>
      )}
      {invoice.paidAtUtc && (
        <Row label={t("invoiceDetail.row.paid")} tone="success">
          {formatDate(invoice.paidAtUtc)}
        </Row>
      )}
      {invoice.voidedAtUtc && (
        <Row label={t("invoiceDetail.row.voided")} tone="danger">
          {formatDate(invoice.voidedAtUtc)}
        </Row>
      )}
      {invoice.periodStartUtc && (
        <Row label={t("invoiceDetail.row.periodStart")}>{formatDate(invoice.periodStartUtc)}</Row>
      )}
      {invoice.periodEndUtc && (
        <Row label={t("invoiceDetail.row.periodEnd")}>{formatDate(invoice.periodEndUtc)}</Row>
      )}
    </dl>
  );
}

function Row({
  label,
  children,
  tone,
}: {
  label: string;
  children: React.ReactNode;
  tone?: "warning" | "success" | "danger";
}) {
  const toneColor =
    tone === "warning"
      ? "text-[var(--color-warning)]"
      : tone === "success"
        ? "text-[var(--color-success)]"
        : tone === "danger"
          ? "text-[var(--color-destructive)]"
          : "text-[var(--color-foreground)]";
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="text-[var(--color-muted-foreground)]">{label}</dt>
      <dd className={`tabular-nums ${toneColor}`}>{children}</dd>
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Loading + not found
// ────────────────────────────────────────────────────────────────────

function DetailSkeleton() {
  return (
    <div className="space-y-5">
      <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
        <Skeleton className="h-1 w-full rounded-none" />
        <div className="p-5 sm:px-6">
          <div className="flex items-center gap-4">
            <Skeleton className="size-14 rounded-2xl" />
            <div className="space-y-2">
              <Skeleton className="h-5 w-40" />
              <Skeleton className="h-3 w-56" />
            </div>
          </div>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_300px]">
        <Skeleton className="h-64 rounded-xl" />
        <Skeleton className="h-48 rounded-xl" />
      </div>
    </div>
  );
}

function NotFoundPanel() {
  const { t } = useTranslation("subscription");
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-8 py-16 text-center">
      <div className="mb-5 grid size-16 place-items-center rounded-2xl bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]">
        <Receipt className="size-7 text-[var(--color-primary)]" />
      </div>
      <h3 className="mb-1.5 text-[17px] font-semibold text-[var(--color-foreground)]">
        {t("invoiceDetail.notFoundTitle")}
      </h3>
      <p className="mb-6 text-[13px] text-[var(--color-muted-foreground)]">
        {t("invoiceDetail.notFoundBody")}
      </p>
      <Button asChild variant="outline" size="sm">
        <Link to="/invoices">{t("invoiceDetail.notFoundBack")}</Link>
      </Button>
    </div>
  );
}
