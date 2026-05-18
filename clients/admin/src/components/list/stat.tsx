import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type StatTone = "default" | "signal" | "success" | "warning" | "danger" | "info";

type StatProps = {
  label: string;
  value: ReactNode;
  hint?: ReactNode;
  tone?: StatTone;
  className?: string;
};

const TONE_VALUE_CLASS: Record<StatTone, string> = {
  default: "text-[var(--color-foreground)]",
  signal: "text-[var(--color-accent-signal)]",
  success: "text-[var(--color-success)]",
  warning: "text-[var(--color-warning)]",
  danger: "text-[var(--color-destructive)]",
  info: "text-[var(--color-info)]",
};

/**
 * Stat — single KPI tile. Mono-caps label → display value → muted hint.
 * Uniform min-height so a row of Stats stays gridlocked even when one
 * tile's hint wraps.
 */
export function Stat({ label, value, hint, tone = "default", className }: StatProps) {
  return (
    <div
      className={cn(
        "card-shell flex h-full min-h-[96px] flex-col justify-between rounded-xl px-5 py-4",
        className,
      )}
    >
      <div className="meta text-[var(--color-muted-foreground)]">{label}</div>
      <div
        className={cn(
          "text-display mt-2 text-[26px] font-semibold leading-none tracking-[-0.02em]",
          TONE_VALUE_CLASS[tone],
        )}
      >
        {value}
      </div>
      {hint && (
        <div className="mt-1 truncate text-[12px] text-[var(--color-muted-foreground)]">
          {hint}
        </div>
      )}
    </div>
  );
}

type StatStripProps = {
  cols?: 2 | 3 | 4;
  children: ReactNode;
  className?: string;
};

/**
 * StatStrip — responsive grid container for Stat tiles with the
 * standard fsh-enter-2 stagger.
 */
export function StatStrip({ cols = 3, children, className }: StatStripProps) {
  return (
    <div
      className={cn(
        "fsh-enter fsh-enter-2 grid grid-cols-1 gap-3",
        cols === 2 && "sm:grid-cols-2",
        cols === 3 && "sm:grid-cols-3",
        cols === 4 && "sm:grid-cols-2 xl:grid-cols-4",
        className,
      )}
    >
      {children}
    </div>
  );
}
