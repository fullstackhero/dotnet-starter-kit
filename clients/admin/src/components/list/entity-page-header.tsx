import * as React from "react";
import type { LucideIcon } from "lucide-react";
import { ToneIconTile, type ToneIconTileTone } from "./tone-icon-tile";

// ───────────────────────────────────────────────────────────────────────
//  EntityPageHeader — tone-tinted icon tile + Outfit title + count chip
//  + description on the left, action buttons on the right.
//  Replaces the dashboard-divergent FormShell page-title area with the
//  unified header rhythm used across the dashboard app.
// ───────────────────────────────────────────────────────────────────────

export function EntityPageHeader({
  icon,
  title,
  tone = "primary",
  total,
  unit = "item",
  description,
  children,
}: {
  icon: LucideIcon;
  title: React.ReactNode;
  /** Icon tile tone. Defaults to `primary`.
   *  Pick `saffron` / `info` / etc. for pages where the rose tile
   *  fights the page's own accent. */
  tone?: ToneIconTileTone;
  total?: number | null;
  unit?: string;
  description?: React.ReactNode;
  /** Action buttons rendered on the right (stack full-width on mobile). */
  children?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div className="flex items-start gap-3.5">
        <ToneIconTile icon={icon} tone={tone} size="lg" />
        <div className="min-w-0">
          <div className="flex items-baseline gap-2">
            <h1 className="font-display text-display-page font-semibold tracking-tight text-[var(--color-foreground)]">
              {title}
            </h1>
            {total !== undefined && total !== null && (
              <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {total} {total === 1 ? unit : `${unit}s`}
              </span>
            )}
          </div>
          {description && (
            <p className="mt-0.5 text-[13px] text-[var(--color-muted-foreground)]">
              {description}
            </p>
          )}
        </div>
      </div>

      {children && (
        <div className="flex w-full gap-2 sm:w-auto">{children}</div>
      )}
    </div>
  );
}
