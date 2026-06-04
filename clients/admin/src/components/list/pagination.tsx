import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";

type PaginationProps = {
  page: number;
  totalPages: number;
  totalCount: number;
  shown: number;
  fetching?: boolean;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
  /** Optional left label override, e.g. "audit events". Default: "items". */
  noun?: string;
};

/**
 * Pagination — mono-caps folio counter + prev/next. Matches Console
 * vocabulary: "Showing N of T · folio PP / TT" instead of generic
 * "Page 1 of 5". Two-digit zero-padded folios for visual rhythm.
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
  noun = "items",
}: PaginationProps) {
  const p = String(page).padStart(2, "0");
  const tp = String(Math.max(totalPages, 1)).padStart(2, "0");
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
      <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Showing {shown} of {totalCount} {noun} · folio {p} / {tp}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" disabled={!hasPrev || fetching} onClick={onPrev}>
          <ChevronLeft className="mr-1 h-3.5 w-3.5" /> Previous
        </Button>
        <Button variant="outline" size="sm" disabled={!hasNext || fetching} onClick={onNext}>
          Next <ChevronRight className="ml-1 h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}
