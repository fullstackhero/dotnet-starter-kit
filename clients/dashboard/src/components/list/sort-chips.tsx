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
          <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            {prefixLabel}
          </span>
          <span aria-hidden className="h-3 w-px bg-[var(--color-border)]" />
        </>
      )}
      <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]/80">
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
        "text-[11px] font-semibold uppercase tracking-wider",
        "border transition-colors duration-[var(--duration-fast)]",
        active
          ? "border-[oklch(from_var(--color-primary)_l_c_h_/_0.25)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
      )}
    >
      {label}
      <Icon className={cn("h-3 w-3", !active && "opacity-60")} />
    </button>
  );
}
