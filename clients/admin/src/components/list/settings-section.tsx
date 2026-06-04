import * as React from "react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

// ───────────────────────────────────────────────────────────────────────
//  SettingsSection — compact section card with header bar, body, and
//  optional footer bar. Replaces the 18rem-aside FormSection layout with
//  a dashboard-style card: icon + title + description in a header stripe,
//  content in a padded body, optional action row in a footer stripe.
//
//  Usage:
//    <SettingsSection title="Identity" icon={UserCircle2} description="…"
//      footer={<Button>Save</Button>}>
//      {children}
//    </SettingsSection>
// ───────────────────────────────────────────────────────────────────────

export function SettingsSection({
  title,
  icon: Icon,
  description,
  footer,
  className,
  children,
}: {
  title?: string;
  icon?: LucideIcon;
  description?: React.ReactNode;
  footer?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section
      className={cn(
        "overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
        "shadow-xs",
        className,
      )}
    >
      {title && (
        <div className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          <h2 className="flex items-center gap-2 text-[13px] font-semibold text-[var(--color-foreground)]">
            {Icon && (
              <Icon
                aria-hidden
                className="size-3.5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]"
              />
            )}
            {title}
          </h2>
          {description && (
            <p className="mt-1 text-[12px] text-[var(--color-muted-foreground)]">
              {description}
            </p>
          )}
        </div>
      )}
      <div className="px-5 py-5">{children}</div>
      {footer && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          {footer}
        </div>
      )}
    </section>
  );
}
