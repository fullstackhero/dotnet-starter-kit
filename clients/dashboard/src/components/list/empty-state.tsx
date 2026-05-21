import type { ReactNode } from "react";
import { Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";

type Action = {
  label: string;
  onClick: () => void;
  icon?: ReactNode;
};

/**
 * EmptyState — calm zero-result panel matching the dos `EntityEmpty`
 * shape: warm muted icon tile, plain semibold uppercase eyebrow,
 * Outfit display headline, body, and one or two action buttons.
 */
export function EmptyState({
  eyebrow,
  headline,
  body,
  icon,
  primaryAction,
  secondaryAction,
}: {
  eyebrow: string;
  headline: ReactNode;
  body: ReactNode;
  icon: ReactNode;
  primaryAction: Action;
  secondaryAction?: Action;
}) {
  return (
    <div className="flex flex-col items-center gap-4 px-6 py-16 text-center sm:py-20">
      {/* Calm muted icon tile — replaces the museum-plinth chrome. */}
      <div className="grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
        {icon}
      </div>

      <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {eyebrow}
      </span>

      <h3 className="font-display text-balance max-w-md text-[18px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)] sm:text-[20px]">
        {headline}
      </h3>

      <p className="max-w-md text-balance text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
        {body}
      </p>

      <div className="mt-2 flex flex-wrap items-center justify-center gap-2">
        {secondaryAction && (
          <Button
            variant="outline"
            size="sm"
            onClick={secondaryAction.onClick}
            className="gap-1.5"
          >
            {secondaryAction.icon}
            {secondaryAction.label}
          </Button>
        )}
        <Button onClick={primaryAction.onClick} className="gap-1.5">
          {primaryAction.icon ?? <Sparkles className="size-3.5" />}
          {primaryAction.label}
        </Button>
      </div>
    </div>
  );
}
