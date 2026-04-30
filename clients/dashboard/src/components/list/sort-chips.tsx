import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import { cn } from "@/lib/cn";

export type SortDir = "asc" | "desc";

export type SortOption<K extends string> = { key: K; label: string };

/**
 * Pill-rail of sort chips used as the primary sort control on list
 * pages. Replaces traditional sortable column headers with a more modern
 * "sort by [chip] [chip] [chip]" presentation.
 */
export function SortChips<K extends string>({
  options,
  sortKey,
  sortDir,
  onSort,
  intro = "sort by",
  prefixLabel,
}: {
  options: SortOption<K>[];
  sortKey: K;
  sortDir: SortDir;
  onSort: (k: K) => void;
  intro?: string;
  /** Optional left-of-chips eyebrow, e.g. "results" / "registry". */
  prefixLabel?: string;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      {prefixLabel && (
        <>
          <span className="font-mono text-[10px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
            {prefixLabel}
          </span>
          <span aria-hidden className="h-3 w-px bg-[var(--color-border-strong)]" />
        </>
      )}
      <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
        {intro}
      </span>
      {options.map((opt) => (
        <SortChip
          key={opt.key}
          label={opt.label}
          active={sortKey === opt.key}
          dir={sortKey === opt.key ? sortDir : null}
          onClick={() => onSort(opt.key)}
        />
      ))}
    </div>
  );
}

function SortChip({
  label,
  active,
  dir,
  onClick,
}: {
  label: string;
  active: boolean;
  dir: SortDir | null;
  onClick: () => void;
}) {
  const Icon = !active ? ArrowUpDown : dir === "asc" ? ArrowUp : ArrowDown;
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        "inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-full px-3",
        "font-mono text-[10.5px] font-medium uppercase tracking-[0.14em]",
        "transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.25)]"
          : "bg-[var(--color-surface-3)] text-[var(--color-muted-foreground)] ring-1 ring-inset ring-[var(--color-border)] hover:bg-[var(--color-surface-4)] hover:text-[var(--color-foreground)]",
      )}
    >
      {label}
      <Icon className={cn("h-3 w-3", !active && "opacity-60")} />
    </button>
  );
}
