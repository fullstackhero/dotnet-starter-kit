import * as React from "react";
import { cn } from "@/lib/cn";

type AvatarSize = "xs" | "sm" | "md" | "lg";

type AvatarProps = React.HTMLAttributes<HTMLSpanElement> & {
  /** Display name; up to two leading characters become the fallback initials. */
  name?: string | null;
  /** Optional image URL. Falls back to the initials when missing or fails to load. */
  src?: string | null;
  size?: AvatarSize;
  /** When true, emits a small pulsing status dot at the bottom-right. */
  status?: "online" | "offline" | "warning";
  /** Adds a soft brand halo around the avatar (used in the dropdown hero). */
  halo?: boolean;
};

const sizeClass: Record<AvatarSize, string> = {
  xs: "h-5 w-5 text-[10px]",
  sm: "h-7 w-7 text-[11px]",
  md: "h-9 w-9 text-[13px]",
  lg: "h-12 w-12 text-[16px]",
};

// Pixel dims mirror `sizeClass`. Setting width + height on the <img>
// prevents avatar swap-in from triggering CLS while the image loads.
const sizePx: Record<AvatarSize, number> = {
  xs: 20,
  sm: 28,
  md: 36,
  lg: 48,
};

const dotSize: Record<AvatarSize, string> = {
  xs: "h-1 w-1",
  sm: "h-1.5 w-1.5",
  md: "h-2 w-2",
  lg: "h-2.5 w-2.5",
};

function getInitials(name?: string | null): string {
  if (!name) return "?";
  const trimmed = name.trim();
  if (trimmed.length === 0) return "?";
  const parts = trimmed.split(/\s+/).slice(0, 2);
  // For "Mukesh Murugan" → "MM"; for single name "admin@root.com" → "A".
  return parts.map((p) => p.charAt(0).toUpperCase()).join("");
}

function statusColor(status: AvatarProps["status"]): string | undefined {
  switch (status) {
    case "online":
      return "var(--color-success)";
    case "warning":
      return "var(--color-warning)";
    case "offline":
      return "var(--color-muted-foreground)";
    default:
      return undefined;
  }
}

/**
 * Avatar — circular surface that carries the brand-mark vocabulary.
 * The base is the rotating conic gradient under a top-edge highlight;
 * the user's initial sits on top in primary-foreground. When `src` is
 * provided, the image covers the gradient. An optional status dot
 * anchors at the bottom-right and pulses for `online`.
 */
export const Avatar = React.forwardRef<HTMLSpanElement, AvatarProps>(
  ({ name, src, size = "md", status, halo, className, ...props }, ref) => {
    const initials = getInitials(name);
    const dotColor = statusColor(status);
    const [imgFailed, setImgFailed] = React.useState(false);
    const showImage = Boolean(src) && !imgFailed;

    return (
      <span
        ref={ref}
        aria-label={name ?? undefined}
        className={cn(
          "relative inline-flex shrink-0",
          halo && [
            // Soft brand halo: outer 4px ring at primary-soft + a deep
            // diffuse glow. Sits behind the avatar via the wrapper's
            // own box-shadow (no stacking-context issues).
            "rounded-full",
            "shadow-[0_0_0_4px_var(--color-primary-soft),0_8px_28px_-8px_oklch(from_var(--color-primary)_l_c_h_/_0.35)]",
          ],
          className,
        )}
        {...props}
      >
        <span
          className={cn(
            "brand-mark grid place-items-center overflow-hidden rounded-full",
            "font-semibold tracking-tight text-[var(--color-primary-foreground)]",
            "shadow-[0_1px_0_oklch(1_0_0_/_0.22)_inset,0_4px_14px_-4px_oklch(from_var(--color-primary)_l_c_h_/_0.45)]",
            sizeClass[size],
          )}
        >
          {showImage ? (
            <img
              src={src ?? undefined}
              alt={name ?? ""}
              referrerPolicy="no-referrer"
              loading="lazy"
              decoding="async"
              width={sizePx[size]}
              height={sizePx[size]}
              onError={() => setImgFailed(true)}
              className="h-full w-full object-cover"
            />
          ) : (
            <span className="relative z-[2] leading-none">{initials}</span>
          )}
        </span>
        {dotColor && (
          <span
            aria-hidden
            className={cn(
              "absolute bottom-0 right-0 rounded-full ring-2 ring-[var(--color-surface-1)]",
              status === "online" && "pulse-dot",
              dotSize[size],
            )}
            style={{ backgroundColor: dotColor, color: dotColor }}
          />
        )}
      </span>
    );
  },
);
Avatar.displayName = "Avatar";
