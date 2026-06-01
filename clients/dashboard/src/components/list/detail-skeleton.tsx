import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/cn";

/**
 * DetailSkeleton — shared loading placeholder for entity detail pages
 * (user / group / role / product / ticket …). Mirrors the real page shape:
 * a hero card (branded gradient strip + avatar tile + title/subtitle lines
 * + a row of badge/stat chips), then a content area below.
 *
 * The content layout is configurable so one component covers the common
 * shapes used across the app:
 *   - "split"     two columns (sidebar + main) — the identity detail pages
 *   - "stacked"   a single full-width column of cards
 *
 * Extracted from the near-identical DetailSkeleton copies that lived inline
 * in product-detail / ticket-detail; pages should import this instead of
 * re-rolling their own.
 */
export function DetailSkeleton({
  layout = "split",
  className,
}: {
  layout?: "split" | "stacked";
  className?: string;
}) {
  return (
    <div
      className={cn("space-y-5", className)}
      role="status"
      aria-busy="true"
    >
      <span className="sr-only">Loading…</span>

      {/* Hero card */}
      <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs">
        <Skeleton className="h-1 w-full rounded-none" />
        <div className="p-5 sm:px-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3 sm:gap-4">
              <Skeleton className="size-11 rounded-xl sm:size-14 sm:rounded-2xl" />
              <div className="space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-3 w-64" />
              </div>
            </div>
            <Skeleton className="h-8 w-32" />
          </div>
          <div className="mt-4 flex flex-wrap gap-2">
            <Skeleton className="h-7 w-24 rounded-lg" />
            <Skeleton className="h-7 w-24 rounded-lg" />
            <Skeleton className="h-7 w-24 rounded-lg" />
          </div>
        </div>
      </div>

      {/* Content */}
      {layout === "split" ? (
        <div className="grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
          <Skeleton className="h-64 rounded-xl" />
          <Skeleton className="h-64 rounded-xl" />
        </div>
      ) : (
        <div className="space-y-5">
          <Skeleton className="h-32 rounded-xl" />
          <Skeleton className="h-64 rounded-xl" />
        </div>
      )}
    </div>
  );
}
