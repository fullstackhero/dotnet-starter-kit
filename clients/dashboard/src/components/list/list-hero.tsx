import {
  Plus,
  RefreshCw,
  Search,
  X,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";
import { pad2 } from "@/lib/list-helpers";

/**
 * Calm hero band shared by every list page in the dashboard (Brands ·
 * Categories · Products · …). A plain warm-paper card with an Outfit
 * title, a tinted count, and a baked-in search bar. Matches the dentalOS
 * calm vocabulary used by `EntityShell` / `EntityDetail`.
 *
 * Composes top-down:
 *   eyebrow row  — section · area · tenant · sub-eyebrow
 *   title row    — display heading + count + subtitle + actions
 *   search row   — search field
 */
export function ListHero({
  eyebrow,
  tenant,
  subEyebrow,
  title,
  totalCount,
  subtitle,
  searchValue,
  onSearch,
  searchPlaceholder = "Find an item by name…",
  isFetching,
  onRefresh,
  ctaLabel = "New",
  onCreate,
}: {
  eyebrow: string;
  tenant: string;
  subEyebrow?: string;
  title: string;
  totalCount: number | null;
  subtitle: string;
  searchValue: string;
  onSearch: (v: string) => void;
  searchPlaceholder?: string;
  isFetching: boolean;
  onRefresh: () => void;
  ctaLabel?: string;
  onCreate: () => void;
}) {
  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-1 overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs",
      )}
    >
      <div className="relative px-6 py-6 sm:px-8 sm:py-7 md:px-8">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
          <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            {eyebrow}
          </span>
          <span aria-hidden className="h-px w-7 bg-[var(--color-border)]" />
          <code className="rounded bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] px-1.5 py-0.5 text-[11px] font-medium text-[var(--color-primary)]">
            {tenant}
          </code>
          {subEyebrow && (
            <>
              <span aria-hidden className="hidden h-px w-7 bg-[var(--color-border)] sm:inline-block" />
              <span className="hidden text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] sm:inline">
                {subEyebrow}
              </span>
            </>
          )}
        </div>

        <div className="mt-4 grid grid-cols-1 gap-6 md:grid-cols-[1fr_auto] md:items-end">
          <div>
            <h1 className="font-display flex items-baseline gap-3 pb-1 text-[28px] font-semibold leading-[1.15] tracking-tight text-[var(--color-foreground)] sm:text-[32px]">
              {title}
              <span
                aria-label={`${totalCount ?? 0} items total`}
                className={cn(
                  "tabular-nums",
                  "text-[20px] font-semibold leading-none tracking-tight",
                  "text-[var(--color-primary)]",
                )}
              >
                {totalCount === null ? "—" : pad2(totalCount)}
              </span>
            </h1>
            <p className="mt-1.5 max-w-xl text-[13.5px] leading-relaxed text-[var(--color-muted-foreground)]">
              {subtitle}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 md:justify-end">
            <Button
              variant="outline"
              size="sm"
              disabled={isFetching}
              onClick={onRefresh}
              className="gap-1.5"
            >
              <RefreshCw
                className={cn("h-3.5 w-3.5", isFetching && "animate-spin")}
              />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            <Button onClick={onCreate} className="gap-1.5">
              <Plus className="h-4 w-4" />
              {ctaLabel}
            </Button>
          </div>
        </div>

        <div className="mt-5">
          <HeroSearch
            value={searchValue}
            onChange={onSearch}
            placeholder={searchPlaceholder}
          />
        </div>
      </div>
    </section>
  );
}

function HeroSearch({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
}) {
  return (
    <div
      className={cn(
        "group/search relative flex h-11 items-center rounded-lg",
        "border border-[var(--color-input)] bg-transparent shadow-xs",
        "transition-shadow duration-[var(--duration-default)]",
        "focus-within:border-[var(--color-ring)] focus-within:ring-[3px] focus-within:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.18)]",
      )}
    >
      <Search className="ml-3.5 h-4 w-4 text-[var(--color-muted-foreground)] transition-colors group-focus-within/search:text-[var(--color-primary)]" />
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        className={cn(
          "h-full flex-1 border-0 bg-transparent pl-2.5 pr-2 text-[14px]",
          "focus-visible:ring-0 focus-visible:ring-offset-0",
          "shadow-none placeholder:text-[var(--color-muted-foreground)]/80",
        )}
      />
      {value && (
        <button
          type="button"
          onClick={() => onChange("")}
          aria-label="Clear search"
          className="mr-1 grid h-7 w-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      )}
    </div>
  );
}
