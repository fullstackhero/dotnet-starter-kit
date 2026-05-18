import * as React from "react";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/cn";

type KpiTileProps = {
  label: string;
  value: React.ReactNode;
  subtitle?: React.ReactNode;
  className?: string;
};

/**
 * KpiTile — small stat card used at the top of list pages. Mono uppercase
 * label, display-weight numeric value with tabular figures, optional
 * subtitle in muted text. Lives inside a Card so the surface tier and
 * border treatment stay consistent with the rest of the admin shell.
 */
export function KpiTile({ label, value, subtitle, className }: KpiTileProps) {
  return (
    <Card className={cn("card-shell-interactive", className)}>
      <CardContent className="px-5 pb-5 pt-5">
        <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          {label}
        </div>
        <div className="text-display mt-3 text-3xl font-semibold leading-none tabular-nums">
          {value}
        </div>
        {subtitle !== undefined && (
          <div className="mt-2 text-xs leading-relaxed text-[var(--color-muted-foreground)]">
            {subtitle}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
