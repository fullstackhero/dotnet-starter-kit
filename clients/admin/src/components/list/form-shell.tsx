import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

type FormShellProps = {
  children: ReactNode;
  className?: string;
  /** Max width of the card. Defaults to a generous reading measure. */
  maxWidth?: "md" | "lg" | "xl" | "full";
};

const MAX_WIDTH_CLASS: Record<NonNullable<FormShellProps["maxWidth"]>, string> = {
  md: "max-w-3xl",
  lg: "max-w-5xl",
  xl: "max-w-6xl",
  full: "max-w-none",
};

/**
 * FormShell — every editor form sits on a single card-shell surface so the
 * fields don't disappear into the canvas-grid background. Pair with
 * FormSection rows + FormActions footer. Apply to create/edit screens that
 * own a single form (not to dashboards that just embed inputs).
 */
export function FormShell({ children, className, maxWidth = "lg" }: FormShellProps) {
  return (
    <div className={cn("card-shell p-6 sm:p-8", MAX_WIDTH_CLASS[maxWidth], className)}>
      <div className="space-y-10">{children}</div>
    </div>
  );
}

type FormSectionProps = {
  /** Mono-caps section heading rendered in the left column (the "aside"). */
  title: string;
  /** Plain prose explaining what this section's fields control. */
  description?: ReactNode;
  /** Optional tone — destructive sections (Danger zone) use the destructive border. */
  tone?: "default" | "danger";
  children: ReactNode;
  className?: string;
};

/**
 * FormSection — one row inside a FormShell: aside (title + description) on
 * the left, fields stack on the right. Stacks vertically on mobile.
 */
export function FormSection({
  title,
  description,
  tone = "default",
  children,
  className,
}: FormSectionProps) {
  const ruleClass =
    tone === "danger"
      ? "border-[var(--color-destructive)]"
      : "border-[var(--color-foreground)]";
  const titleClass =
    tone === "danger"
      ? "text-[var(--color-destructive)]"
      : "text-[var(--color-foreground)]";

  return (
    <section className={cn("grid gap-8 md:grid-cols-[18rem_1fr] md:gap-10", className)}>
      <aside className={cn("space-y-2 border-t pt-3", ruleClass)}>
        <p
          className={cn(
            "font-mono text-[0.6875rem] uppercase tracking-[0.22em]",
            titleClass,
          )}
        >
          {`\\ ${title}`}
        </p>
        {description && (
          <p className="text-sm text-[var(--color-muted-foreground)]">{description}</p>
        )}
      </aside>
      <div className="space-y-5">{children}</div>
    </section>
  );
}

/**
 * FormActions — bottom row of a FormShell with a hairline separator.
 * Right-aligned by default; pass `align="between"` for split layouts
 * (e.g. left-side destructive action + right-side primary).
 */
export function FormActions({
  children,
  align = "start",
  className,
}: {
  children: ReactNode;
  align?: "start" | "between" | "end";
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-2 border-t border-[var(--color-border)] pt-5",
        align === "between" && "justify-between",
        align === "end" && "justify-end",
        className,
      )}
    >
      {children}
    </div>
  );
}
