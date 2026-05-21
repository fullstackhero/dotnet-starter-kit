import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

// ───────────────────────────────────────────────────────────────────────
//  ToneIconTile — tone-tinted icon square. The single most repeated bit
//  of inline className soup across the dashboard: a rounded tile with a
//  10%-alpha background fill, a 22%-alpha inset ring, and an icon in the
//  tone's full colour.
//
//  Usage:
//    <ToneIconTile icon={Package} tone="primary" size="lg" />
//
//  All seven semantic tones are supported. The default (`primary` / `lg`)
//  matches `EntityPageHeader`'s tile so the page header stays visually
//  identical when refactored to use this primitive directly.
// ───────────────────────────────────────────────────────────────────────

export type ToneIconTileTone =
  | "primary"
  | "saffron"
  | "success"
  | "warning"
  | "destructive"
  | "info"
  | "muted";

export type ToneIconTileSize = "sm" | "md" | "lg";

const TONE_VAR: Record<ToneIconTileTone, string> = {
  primary: "--color-primary",
  saffron: "--color-saffron",
  success: "--color-success",
  warning: "--color-warning",
  destructive: "--color-destructive",
  info: "--color-info",
  muted: "--color-muted-foreground",
};

const SIZE_MAP: Record<
  ToneIconTileSize,
  { tile: string; icon: string; radius: string }
> = {
  sm: { tile: "size-7",  icon: "size-3.5", radius: "rounded-md" },
  md: { tile: "size-9",  icon: "size-4",   radius: "rounded-lg" },
  lg: { tile: "size-10", icon: "size-5",   radius: "rounded-xl" },
};

export function ToneIconTile({
  icon: Icon,
  tone = "primary",
  size = "lg",
  className,
}: {
  icon: LucideIcon;
  tone?: ToneIconTileTone;
  size?: ToneIconTileSize;
  className?: string;
}) {
  const dims = SIZE_MAP[size];
  const v = TONE_VAR[tone];
  return (
    <span
      aria-hidden
      className={cn(
        "grid shrink-0 place-items-center",
        dims.tile,
        dims.radius,
        className,
      )}
      style={{
        backgroundColor: `oklch(from var(${v}) l c h / 0.10)`,
        boxShadow: `inset 0 0 0 1px oklch(from var(${v}) l c h / 0.22)`,
        color: `var(${v})`,
      }}
    >
      <Icon className={dims.icon} />
    </span>
  );
}
