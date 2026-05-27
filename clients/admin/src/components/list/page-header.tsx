import type { ReactNode } from "react";
import { SectionRule, type SectionCrumb } from "@/components/section-rule";
import { cn } from "@/lib/cn";

type PageHeaderProps = {
  crumbs: SectionCrumb[];
  trailing?: ReactNode;
  title: string;
  badge?: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  className?: string;
};

/**
 * PageHeader — the editorial top stack used by every Console page that
 * isn't a list-of-things with a built-in search bar. Pairs a SectionRule
 * (with its chartreuse hairline) with a display title and an actions
 * cluster. Lists use ListHeader instead, which adds the search row.
 */
export function PageHeader({
  crumbs,
  trailing,
  title,
  badge,
  description,
  actions,
  className,
}: PageHeaderProps) {
  return (
    <div className={cn("space-y-6 fsh-enter", className)}>
      <SectionRule crumbs={crumbs} trailing={trailing} />
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-3">
            <h1 className="font-display text-4xl font-semibold tracking-tight md:text-5xl">
              {title}
            </h1>
            {badge}
          </div>
          {description && (
            <p className="mt-2 max-w-2xl text-sm text-[var(--color-muted-foreground)]">
              {description}
            </p>
          )}
        </div>
        {actions && (
          <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>
        )}
      </div>
    </div>
  );
}
