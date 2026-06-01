import { RefreshCw } from "lucide-react";

/**
 * Inline error band displayed between the toolbar and the list when a
 * query has failed. Picks up the destructive token + the mono-caps
 * "failure ·" eyebrow used elsewhere in the dashboard.
 *
 * When `onRetry` is supplied a small "Retry" button is rendered on the
 * right — wire it to the failed query's `refetch`. Omitting `onRetry`
 * keeps the original message-only band (backward-compatible).
 */
export function ErrorBand({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-xl border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-5 py-3 text-sm text-[var(--color-destructive)]">
      <div className="min-w-0">
        <span className="text-[11px] font-semibold uppercase tracking-wider">
          Failure ·{" "}
        </span>
        {message}
      </div>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="inline-flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[var(--color-card)] px-2.5 text-[12px] font-medium text-[var(--color-destructive)] transition-colors hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)]"
        >
          <RefreshCw className="size-3" />
          Retry
        </button>
      )}
    </div>
  );
}
