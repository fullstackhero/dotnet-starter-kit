import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";

/**
 * Pagination footer used at the bottom of every list page.
 * Mono-caps "Showing N of T · folio P / TP" + Prev/Next.
 */
export function Pagination({
  page,
  totalPages,
  totalCount,
  shown,
  fetching,
  hasPrev,
  hasNext,
  onPrev,
  onNext,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  shown: number;
  fetching: boolean;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-4 flex flex-wrap items-center justify-between gap-3 pt-1">
      <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        Showing {shown} of {totalCount} · folio {page} / {totalPages}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={!hasPrev || fetching} onClick={onPrev}>
          <ChevronLeft className="mr-1 h-4 w-4" /> Previous
        </Button>
        <Button variant="outline" size="sm" disabled={!hasNext || fetching} onClick={onNext}>
          Next <ChevronRight className="ml-1 h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
