/**
 * Inline error band displayed between the toolbar and the list when a
 * query has failed. Picks up the destructive token + the mono-caps
 * "failure ·" eyebrow used elsewhere in the dashboard.
 */
export function ErrorBand({ message }: { message: string }) {
  return (
    <div className="rounded-xl border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-5 py-3 text-sm text-[var(--color-destructive)]">
      <span className="text-[11px] font-semibold uppercase tracking-wider">
        Failure ·{" "}
      </span>
      {message}
    </div>
  );
}
