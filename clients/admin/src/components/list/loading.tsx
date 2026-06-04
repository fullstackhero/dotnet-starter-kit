import { cn } from "@/lib/cn";

type LoadingRowProps = {
  className?: string;
  label?: string;
};

/**
 * LoadingRow — small inline mono-caps placeholder used at the top of
 * a list while the first page resolves. Subtle, no spinner — the
 * caret-style ellipsis is enough.
 */
export function LoadingRow({ className, label = "Loading" }: LoadingRowProps) {
  return (
    <div
      role="status"
      className={cn(
        "px-1 py-12 text-center text-sm font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]",
        className,
      )}
    >
      {label}
      <span className="caret text-[var(--color-accent-signal)]" aria-hidden />
    </div>
  );
}
