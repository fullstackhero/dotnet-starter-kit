/**
 * Inline error band displayed between the toolbar and the list when a
 * query has failed. Picks up the destructive token + the mono-caps
 * "failure ·" eyebrow used elsewhere in the dashboard.
 */
export function ErrorBand({ message }: { message: string }) {
  return (
    <div className="surface-edge rounded-xl bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-5 py-3 text-sm text-[var(--color-destructive)]">
      <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.16em]">
        failure ·{" "}
      </span>
      {message}
    </div>
  );
}
