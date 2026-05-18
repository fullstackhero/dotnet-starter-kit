import { AlertTriangle } from "lucide-react";

type ErrorBandProps = {
  message: string;
  /** Optional eyebrow override, e.g. "TIMEOUT". Default: "FAILURE". */
  kind?: string;
};

/**
 * ErrorBand — inline failure surface used between toolbar and content.
 * Mono-caps eyebrow + destructive tint, matched to the rest of Console.
 */
export function ErrorBand({ message, kind = "failure" }: ErrorBandProps) {
  return (
    <div
      role="alert"
      className="flex items-start gap-2.5 rounded-md border border-[var(--color-destructive)]/40 bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3.5 py-2.5 text-sm text-[var(--color-destructive)]"
    >
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      <div className="min-w-0">
        <span className="meta mr-2 text-[var(--color-destructive)]">{kind} ·</span>
        <span className="text-[var(--color-destructive)]">{message}</span>
      </div>
    </div>
  );
}
