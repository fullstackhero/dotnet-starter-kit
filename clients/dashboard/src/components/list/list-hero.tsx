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
 * Atmospheric hero band shared by every list page in the dashboard
 * (Brands · Categories · Products · …). Wraps the page in a layered
 * radial-gradient + grain backdrop, surfaces the count as a tinted
 * inline number, and bakes the search bar into the same surface.
 *
 * Composes top-down:
 *   eyebrow row  — section · area · tenant · sub-eyebrow
 *   title row    — display heading + count + subtitle + actions
 *   search row   — glassy search field
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
        "fsh-enter fsh-enter-1 card-shell relative overflow-hidden rounded-[20px]",
        "bg-[var(--color-surface-3)]",
      )}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(60% 70% at 0% 0%, oklch(from var(--color-primary) l c h / 0.18), transparent 60%),
            radial-gradient(50% 60% at 100% 0%, oklch(0.700 0.155 195 / 0.10), transparent 65%),
            radial-gradient(80% 80% at 100% 100%, oklch(from var(--color-primary) l c h / 0.05), transparent 70%)
          `,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.06] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <div className="relative px-6 py-7 sm:px-8 sm:py-9 md:px-10">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            {eyebrow}
          </span>
          <span aria-hidden className="h-px w-7 bg-[var(--color-border-strong)]" />
          <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-primary)]">
            {tenant}
          </code>
          {subEyebrow && (
            <>
              <span aria-hidden className="hidden h-px w-7 bg-[var(--color-border-strong)] sm:inline-block" />
              <span className="hidden font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/80 sm:inline">
                {subEyebrow}
              </span>
            </>
          )}
        </div>

        <div className="mt-5 grid grid-cols-1 gap-6 md:grid-cols-[1fr_auto] md:items-end">
          <div>
            {/* leading-[1.1] + pb-1 leave room for descenders. With the
                previous 1.02, titles containing g/y/p/q (e.g. anything
                ending in "Settings" or "Pricing") had their bottom hairs
                clipped by the section's outer `overflow-hidden`. The
                existing catalog titles ("Brands", "Products",
                "Categories") have no descenders so the bug stayed
                dormant. */}
            <h1 className="text-display flex items-baseline gap-3 pb-1 text-[40px] font-semibold leading-[1.1] tracking-[-0.025em] sm:text-[44px]">
              {title}
              <span
                aria-label={`${totalCount ?? 0} items total`}
                className={cn(
                  "tabular-nums",
                  "text-[26px] font-semibold leading-none tracking-[-0.02em]",
                  "text-[var(--color-primary)]",
                )}
              >
                {totalCount === null ? "—" : pad2(totalCount)}
              </span>
            </h1>
            <p className="mt-2 max-w-xl text-[14px] leading-relaxed text-[var(--color-muted-foreground)]">
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
            <Button onClick={onCreate} className="brand-glow gradient-sheen gap-1.5">
              <Plus className="h-4 w-4" />
              {ctaLabel}
            </Button>
          </div>
        </div>

        <div className="mt-6">
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
        "group/search relative flex h-12 items-center rounded-xl",
        "bg-[oklch(from_var(--color-background)_l_c_h_/_0.55)]",
        "ring-1 ring-inset ring-[var(--color-border)]",
        "shadow-[var(--highlight-top)]",
        "backdrop-blur-md",
        "transition-shadow duration-[var(--duration-default)]",
        "focus-within:ring-[oklch(from_var(--color-primary)_l_c_h_/_0.35)]",
      )}
    >
      <Search className="ml-4 h-4 w-4 text-[var(--color-muted-foreground)] transition-colors group-focus-within/search:text-[var(--color-primary)]" />
      <Input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        className={cn(
          "h-full flex-1 border-0 bg-transparent pl-3 pr-2 text-[15px]",
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
