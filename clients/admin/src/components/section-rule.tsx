import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type SectionCrumb = {
  label: string;
  muted?: boolean;
};

export type SectionRuleProps = {
  crumbs: SectionCrumb[];
  trailing?: ReactNode;
  className?: string;
};

/**
 * Editorial top-rule used on every page within a module section.
 * Renders a hairline above breadcrumb-style mono-caps labels separated by "\\".
 */
export function SectionRule({ crumbs, trailing, className }: SectionRuleProps) {
  return (
    <div className={cn("section-rule justify-between", className)}>
      <div className="flex items-baseline gap-2">
        {crumbs.map((c, i) => (
          <span key={`${c.label}-${i}`} className="flex items-baseline gap-2">
            {i > 0 && <span className="section-rule__crumb section-rule__crumb--muted">\\</span>}
            <span className={cn("section-rule__crumb", c.muted && "section-rule__crumb--muted")}>
              {c.label}
            </span>
          </span>
        ))}
      </div>
      {trailing && <div className="text-[0.6875rem] font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">{trailing}</div>}
    </div>
  );
}
