import * as React from "react";
import { ChevronLeft, ChevronRight, Search } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";
import { ToneIconTile, type ToneIconTileTone } from "./tone-icon-tile";

// ───────────────────────────────────────────────────────────────────────
//  EntityPageHeader — tone-tinted icon tile + Outfit title + count chip
//  + description on the left, action buttons on the right.
//  Matches the dentalOS patient-page header rhythm.
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
  /** Icon tile tone. Defaults to `primary` — pick `saffron`/`info`/etc.
   *  for pages where the rose tile fights the page's own accent. */
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

// ───────────────────────────────────────────────────────────────────────
//  EntitySearch — large 46px rounded-xl search with left icon + Clear.
// ───────────────────────────────────────────────────────────────────────

export function EntitySearch({
  value,
  onChange,
  placeholder,
  autoFocus,
}: {
  value: string;
  onChange: (next: string) => void;
  placeholder?: string;
  autoFocus?: boolean;
}) {
  return (
    <div className="relative">
      <Search className="pointer-events-none absolute left-4 top-1/2 size-[18px] -translate-y-1/2 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
      <input
        type="text"
        placeholder={placeholder}
        aria-label={placeholder ?? "Search"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoFocus={autoFocus}
        className={cn(
          "h-[46px] w-full rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
          "pl-12 pr-4 text-[14px] font-normal text-[var(--color-foreground)] outline-none",
          "placeholder:text-[var(--color-muted-foreground)]",
          "shadow-xs",
          "transition-all duration-200",
          "focus:border-[oklch(from_var(--color-ring)_l_c_h_/_0.30)] focus:ring-2 focus:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.10)]",
        )}
      />
      {value && (
        <button
          onClick={() => onChange("")}
          aria-label="Clear search"
          className="absolute right-4 top-1/2 -translate-y-1/2 cursor-pointer text-[11px] font-medium text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
          type="button"
        >
          Clear
        </button>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityFilterPill — segmented filter pill with rose-fill active.
// ───────────────────────────────────────────────────────────────────────

export function EntityFilterPill<T extends string | boolean | null>({
  label,
  value,
  onChange,
  options,
}: {
  label?: string;
  value: T;
  onChange: (next: T) => void;
  options: ReadonlyArray<{ value: T; label: string }>;
}) {
  return (
    <div
      role="group"
      aria-label={label}
      className="inline-flex h-8 items-center rounded-full border border-[var(--color-border)] bg-[var(--color-card)] p-0.5 text-[11px] font-semibold uppercase tracking-wider"
    >
      {options.map((opt, i) => {
        const isActive = value === opt.value;
        return (
          <button
            key={`${String(opt.value)}-${i}`}
            type="button"
            onClick={() => onChange(opt.value)}
            aria-pressed={isActive}
            className={cn(
              "h-7 cursor-pointer rounded-full px-3 transition-colors duration-[var(--duration-fast)]",
              isActive
                ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityPager — small "Page X of Y" + chevron buttons. Matches the
//  dentalOS patient list pagination shape.
// ───────────────────────────────────────────────────────────────────────

export function EntityPager({
  page,
  totalPages,
  hasPrev,
  hasNext,
  onPrev,
  onNext,
}: {
  page: number;
  totalPages: number;
  hasPrev: boolean;
  hasNext: boolean;
  onPrev: () => void;
  onNext: () => void;
}) {
  if (totalPages <= 1) return null;
  return (
    <div className="mt-3 flex items-center justify-between">
      <p className="text-[11px] text-[var(--color-muted-foreground)]">
        Page {page} of {totalPages}
      </p>
      <div className="flex items-center gap-1">
        <button
          type="button"
          disabled={!hasPrev}
          onClick={onPrev}
          aria-label="Previous page"
          className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[oklch(from_var(--color-muted)_l_c_h_/_0.5)] hover:text-[var(--color-foreground)] disabled:cursor-not-allowed disabled:opacity-30"
        >
          <ChevronLeft className="size-4" />
        </button>
        <button
          type="button"
          disabled={!hasNext}
          onClick={onNext}
          aria-label="Next page"
          className="grid size-8 cursor-pointer place-items-center rounded-lg text-[var(--color-muted-foreground)] transition-colors hover:bg-[oklch(from_var(--color-muted)_l_c_h_/_0.5)] hover:text-[var(--color-foreground)] disabled:cursor-not-allowed disabled:opacity-30"
        >
          <ChevronRight className="size-4" />
        </button>
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityEmpty — large icon + Outfit headline + body + action button.
//  Matches the dentalOS empty state shape.
// ───────────────────────────────────────────────────────────────────────

export function EntityEmpty({
  icon: Icon,
  title,
  body,
  action,
}: {
  icon: LucideIcon;
  title: string;
  body?: React.ReactNode;
  action?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center">
      <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
        <Icon className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)]" />
      </div>
      <h2 className="mb-1.5 font-display text-[17px] font-semibold text-[var(--color-foreground)]">
        {title}
      </h2>
      {body && (
        <p className="mb-6 max-w-[320px] text-[13px] text-[var(--color-muted-foreground)]">
          {body}
        </p>
      )}
      {action}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityListCard — desktop list container (border, rounded-xl,
//  shadow-xs). The header row + data rows live inside as children.
// ───────────────────────────────────────────────────────────────────────

export function EntityListCard({
  className,
  children,
  ...rest
}: {
  className?: string;
  children: React.ReactNode;
} & React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]",
        "shadow-xs",
        className,
      )}
      {...rest}
    >
      {children}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityListHeader — uppercase-tracked column titles bar. Grid columns
//  are supplied by the consumer via className.
// ───────────────────────────────────────────────────────────────────────

export function EntityListHeader({
  className,
  children,
}: {
  className: string;
  children: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "grid items-center gap-3 border-b border-[var(--color-border)] bg-[oklch(from_var(--color-muted)_l_c_h_/_0.4)] px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]",
        className,
      )}
    >
      {children}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityListRow — desktop row with hover bg-accent/40, group state,
//  and a hairline bottom divider on all but the last row.
// ───────────────────────────────────────────────────────────────────────

export function EntityListRow({
  className,
  isLast,
  dim,
  children,
  onClick,
}: {
  className: string;
  isLast?: boolean;
  /** Soft dim for inactive/disabled entities. */
  dim?: boolean;
  children: React.ReactNode;
  onClick?: () => void;
}) {
  const interactive = !!onClick;
  return (
    <div
      onClick={onClick}
      onKeyDown={
        interactive
          ? (e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onClick?.();
              }
            }
          : undefined
      }
      role={interactive ? "button" : undefined}
      tabIndex={interactive ? 0 : undefined}
      className={cn(
        "group grid items-center gap-3 px-5 py-3 transition-colors duration-100",
        "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
        !isLast && "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]",
        dim && "opacity-75",
        interactive && "cursor-pointer",
        className,
      )}
    >
      {children}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityMobileCard — full-bleed card on mobile with hover/active
//  states. Use Link-like wrapper so the row navigates on tap.
// ───────────────────────────────────────────────────────────────────────

export const EntityMobileCard = React.forwardRef<
  HTMLAnchorElement,
  React.AnchorHTMLAttributes<HTMLAnchorElement> & { dim?: boolean }
>(({ className, dim, children, ...props }, ref) => (
  <a
    ref={ref}
    className={cn(
      "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
      "shadow-xs",
      "transition-colors hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)] active:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.6)]",
      "outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.4)]",
      dim && "opacity-75",
      className,
    )}
    {...props}
  >
    {children}
  </a>
));
EntityMobileCard.displayName = "EntityMobileCard";

// ───────────────────────────────────────────────────────────────────────
//  EntityInitialsAvatar — square rose-tinted tile with up-to-2 letter
//  initials. Drop-in for table rows + mobile cards.
// ───────────────────────────────────────────────────────────────────────

export function EntityInitialsAvatar({
  name,
  size = 36,
  className,
}: {
  name: string;
  size?: number;
  className?: string;
}) {
  const initials =
    name
      .split(" ")
      .filter((w) => w.length > 0 && !w.endsWith("."))
      .map((w) => w[0])
      .join("")
      .slice(0, 2)
      .toUpperCase() || "·";
  return (
    <span
      aria-hidden
      style={{ width: size, height: size }}
      className={cn(
        "grid shrink-0 place-items-center rounded-xl",
        "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)]",
        "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]",
        className,
      )}
    >
      <span
        className="font-display font-bold text-[var(--color-primary)]"
        style={{ fontSize: size <= 32 ? 10 : size <= 40 ? 12 : 14 }}
      >
        {initials}
      </span>
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityStatusBadge — small pill for status labels (Active, Hidden,
//  etc.). Tone choices: default / success / warning / danger.
// ───────────────────────────────────────────────────────────────────────

export type EntityStatusTone =
  | "default"
  | "success"
  | "warning"
  | "danger"
  | "info";

const STATUS_TONES: Record<EntityStatusTone, string> = {
  default:
    "bg-[var(--color-secondary)] text-[var(--color-secondary-foreground)] border-transparent",
  success:
    "bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)] border-[oklch(from_var(--color-success)_l_c_h_/_0.20)]",
  warning:
    "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.10)] text-[var(--color-warning)] border-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
  danger:
    "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] text-[var(--color-destructive)] border-[oklch(from_var(--color-destructive)_l_c_h_/_0.20)]",
  info: "bg-[oklch(from_var(--color-info)_l_c_h_/_0.10)] text-[var(--color-info)] border-[oklch(from_var(--color-info)_l_c_h_/_0.20)]",
};

export function EntityStatusBadge({
  tone = "default",
  withDot,
  children,
  className,
}: {
  tone?: EntityStatusTone;
  /** Render a small tone-coloured leading dot. Useful for status pills
   *  where the dot reinforces the colour at a glance ("● LIVE"). */
  withDot?: boolean;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex h-5 items-center rounded-full border px-2 py-0 text-[10px] font-semibold uppercase tracking-wider",
        withDot && "gap-1.5",
        STATUS_TONES[tone],
        className,
      )}
    >
      {withDot && (
        <span
          aria-hidden
          className="inline-block size-1.5 shrink-0 rounded-full bg-current"
        />
      )}
      {children}
    </span>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  EntityListLoading — skeleton rows for both mobile + desktop. Pass
//  the same grid template that the real rows use so the columns line
//  up while loading.
// ───────────────────────────────────────────────────────────────────────

export function EntityListLoading({
  rows = 6,
  desktopColumns,
  mobile = true,
}: {
  rows?: number;
  /** Tailwind grid-cols-* class for desktop skeleton rows. */
  desktopColumns: string;
  mobile?: boolean;
}) {
  return (
    <div role="status" aria-busy="true">
      <span className="sr-only">Loading…</span>
      {mobile && (
        <div className="space-y-2 md:hidden">
          {Array.from({ length: rows }).map((_, i) => (
            <div
              key={i}
              className="flex items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4"
            >
              <SkeletonBlock className="size-10 rounded-xl" />
              <div className="flex-1 space-y-1.5">
                <SkeletonBlock className="h-3.5 w-40" />
                <SkeletonBlock className="h-2.5 w-28" />
              </div>
            </div>
          ))}
        </div>
      )}
      <div className="hidden overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] md:block">
        {Array.from({ length: rows }).map((_, i) => (
          <div
            key={i}
            className={cn(
              "grid items-center gap-3 px-5 py-3",
              desktopColumns,
              i < rows - 1 && "border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)]",
            )}
          >
            <div className="flex items-center gap-3">
              <SkeletonBlock className="size-9 rounded-xl" />
              <SkeletonBlock className="h-4 w-48" />
            </div>
            <SkeletonBlock className="h-3 w-24" />
            <SkeletonBlock className="h-5 w-16 rounded-full" />
            <SkeletonBlock className="h-4 w-16" />
            <SkeletonBlock className="ml-auto size-4" />
          </div>
        ))}
      </div>
    </div>
  );
}

function SkeletonBlock({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "animate-pulse bg-[oklch(from_var(--color-muted)_l_c_h_/_0.6)]",
        className,
      )}
    />
  );
}
