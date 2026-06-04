import * as React from "react";
import { Link } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailBack — small "← Back to {parent}" link. Sits above the
//  hero with a left-slide hover hint. Matches the dentalOS detail page.
// ───────────────────────────────────────────────────────────────────────

export function EntityDetailBack({
  to,
  label,
}: {
  to: string;
  label: string;
}) {
  return (
    <Link
      to={to}
      className="group mb-4 inline-flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
    >
      <ArrowLeft className="size-3 transition-transform group-hover:-translate-x-0.5" />
      {label}
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailHero — warm-paper card with a rose→saffron gradient strip
//  on top, then an identity row (avatar slot + title + badges + actions),
//  an optional stats row, and an optional contact/meta row.
//  Matches the dentalOS patient-detail-page hero shape.
// ───────────────────────────────────────────────────────────────────────

export function EntityDetailHero({
  avatar,
  title,
  badges,
  subtitle,
  actions,
  stats,
  meta,
  className,
}: {
  /** Large avatar/icon tile — recommend size 44-56px. */
  avatar?: React.ReactNode;
  title: React.ReactNode;
  /** Inline badges next to the title — Active / Hidden / Verified etc. */
  badges?: React.ReactNode;
  /** Secondary metadata line under the title (phone, email, age, etc.). */
  subtitle?: React.ReactNode;
  /** Action buttons on the right of the identity row. */
  actions?: React.ReactNode;
  /** Stat chips row — visits, completed, since, etc. */
  stats?: React.ReactNode;
  /** Contact / meta row — inline icon+text labels below the stats. */
  meta?: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "fsh-enter overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
        "shadow-xs",
        "mb-5",
        className,
      )}
    >
      {/* Branded gradient strip — rose → saffron */}
      <div
        aria-hidden
        className="h-1 w-full"
        style={{
          background:
            "linear-gradient(90deg, var(--color-primary), oklch(from var(--color-primary) l c h / 0.8), var(--color-saffron))",
        }}
      />

      <div className="p-5 sm:px-6">
        {/* Identity row */}
        <div className="flex items-start justify-between gap-3 sm:gap-4">
          <div className="flex min-w-0 items-center gap-3 sm:gap-4">
            {avatar}
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2 sm:gap-2.5">
                <h1 className="truncate font-display text-[17px] font-bold leading-none tracking-tight text-[var(--color-foreground)] sm:text-[20px]">
                  {title}
                </h1>
                {badges}
              </div>
              {subtitle && (
                <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)] sm:mt-1.5 sm:text-[12px]">
                  {subtitle}
                </p>
              )}
            </div>
          </div>
          {actions && (
            <div className="flex shrink-0 items-center gap-1.5">{actions}</div>
          )}
        </div>

        {/* Stats row */}
        {stats && (
          <div className="mt-4 flex flex-wrap items-center gap-2">{stats}</div>
        )}

        {/* Meta row */}
        {meta && (
          <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-[11px]">
            {meta}
          </div>
        )}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailAvatar — large rose-tinted square tile for the hero.
//  Defaults to 56px (large) on desktop / 44px on mobile.
// ───────────────────────────────────────────────────────────────────────

export function EntityDetailAvatar({
  name,
  src,
  icon: Icon,
  className,
}: {
  name?: string;
  src?: string | null;
  icon?: LucideIcon;
  className?: string;
}) {
  const initials = name
    ? name
        .split(" ")
        .filter((w) => w.length > 0 && !w.endsWith("."))
        .map((w) => w[0])
        .join("")
        .slice(0, 2)
        .toUpperCase() || "·"
    : "·";

  // Fall back to the icon/initials when the image is missing OR fails to load
  // (e.g. a seeded default-avatar URL that 404s) — never show a broken image.
  const [imgFailed, setImgFailed] = React.useState(false);
  React.useEffect(() => setImgFailed(false), [src]);
  const showImage = Boolean(src) && !imgFailed;

  return (
    <div
      className={cn(
        "grid size-11 shrink-0 place-items-center overflow-hidden rounded-xl",
        "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]",
        "sm:size-14 sm:rounded-2xl",
        className,
      )}
    >
      {showImage ? (
        <img
          src={src ?? undefined}
          alt=""
          onError={() => setImgFailed(true)}
          className="size-full object-cover"
        />
      ) : Icon ? (
        <Icon className="size-5 text-[var(--color-primary)] sm:size-6" />
      ) : (
        <span className="font-display text-[16px] font-bold text-[var(--color-primary)] sm:text-[20px]">
          {initials}
        </span>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailStat — small stat pill: icon + bold value + label.
//  Tone variants control the background/icon color.
// ───────────────────────────────────────────────────────────────────────

export type EntityDetailStatTone = "default" | "success" | "primary" | "warning" | "danger";

const STAT_TONES: Record<EntityDetailStatTone, { bg: string; icon: string; value: string }> = {
  default: {
    bg: "bg-[oklch(from_var(--color-muted)_l_c_h_/_0.5)]",
    icon: "text-[var(--color-muted-foreground)]",
    value: "text-[var(--color-foreground)]",
  },
  primary: {
    bg: "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]",
    icon: "text-[var(--color-primary)]",
    value: "text-[var(--color-primary)]",
  },
  success: {
    bg: "bg-[oklch(from_var(--color-success)_l_c_h_/_0.08)]",
    icon: "text-[var(--color-success)]",
    value: "text-[var(--color-success)]",
  },
  warning: {
    bg: "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)]",
    icon: "text-[var(--color-warning)]",
    value: "text-[var(--color-warning)]",
  },
  danger: {
    bg: "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)]",
    icon: "text-[var(--color-destructive)]",
    value: "text-[var(--color-destructive)]",
  },
};

export function EntityDetailStat({
  icon: Icon,
  value,
  label,
  tone = "default",
}: {
  icon?: LucideIcon;
  value: React.ReactNode;
  label: string;
  tone?: EntityDetailStatTone;
}) {
  const t = STAT_TONES[tone];
  return (
    <div
      className={cn(
        "inline-flex h-7 items-center gap-1.5 rounded-lg px-3 text-[11px]",
        t.bg,
      )}
    >
      {Icon && <Icon className={cn("size-3", t.icon)} />}
      <span className={cn("font-display font-bold", t.value)}>{value}</span>
      <span className="text-[var(--color-muted-foreground)]">{label}</span>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailMeta — small icon+text inline meta item for the hero's
//  meta row. Use with size="size-3" icons.
// ───────────────────────────────────────────────────────────────────────

export function EntityDetailMeta({
  icon: Icon,
  children,
  hideOnMobile,
  hideOnTablet,
}: {
  icon?: LucideIcon;
  children: React.ReactNode;
  hideOnMobile?: boolean;
  hideOnTablet?: boolean;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 text-[var(--color-muted-foreground)]",
        hideOnMobile && "hidden sm:inline-flex",
        hideOnTablet && "hidden lg:inline-flex",
      )}
    >
      {Icon && <Icon className="size-3" />}
      {children}
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityDetailSection — warm-paper card with optional header bar.
//  Used as the building block for content cards on detail pages.
//  Mirrors the dentalOS detail-page content card.
// ───────────────────────────────────────────────────────────────────────

export function EntityDetailSection({
  title,
  icon: Icon,
  description,
  action,
  footer,
  padded = true,
  className,
  children,
}: {
  title?: React.ReactNode;
  icon?: LucideIcon;
  description?: React.ReactNode;
  /** Trailing action button(s) shown on the right of the header bar. */
  action?: React.ReactNode;
  footer?: React.ReactNode;
  /** When false, body has no padding (useful for list-style content). */
  padded?: boolean;
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
        <div className="flex items-center justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          <div className="min-w-0">
            <h2 className="flex items-center gap-2 text-[13px] font-semibold text-[var(--color-foreground)]">
              {Icon && (
                <Icon className="size-3.5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
              )}
              {title}
            </h2>
            {description && (
              <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
                {description}
              </p>
            )}
          </div>
          {action && <div className="shrink-0">{action}</div>}
        </div>
      )}
      <div className={cn(padded && "px-5 py-5")}>{children}</div>
      {footer && (
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-5 py-3">
          {footer}
        </div>
      )}
    </section>
  );
}
