import { cn } from "@/lib/cn";

const TONES = ["mono-tone-0", "mono-tone-1", "mono-tone-2", "mono-tone-3"] as const;

function toneFor(seed: string): string {
  let h = 2166136261;
  for (let i = 0; i < seed.length; i++) {
    h ^= seed.charCodeAt(i);
    h = (h * 16777619) >>> 0;
  }
  return TONES[h % TONES.length];
}

function initialsFor(first?: string | null, last?: string | null, fallback?: string | null): string {
  const f = (first ?? "").trim();
  const l = (last ?? "").trim();
  if (f && l) return (f[0] + l[0]).toUpperCase();
  if (f) return f.slice(0, 2).toUpperCase();
  if (l) return l.slice(0, 2).toUpperCase();
  const fb = (fallback ?? "??").trim();
  return (fb.slice(0, 2) || "??").toUpperCase();
}

type Size = "sm" | "md" | "lg";

const SIZE_CLASS: Record<Size, string> = {
  sm: "h-8 w-8 text-[0.6875rem]",
  md: "h-10 w-10 text-xs",
  lg: "h-20 w-20 text-2xl",
};

export type MonogramProps = {
  seed: string;
  firstName?: string | null;
  lastName?: string | null;
  fallback?: string | null;
  size?: Size;
  className?: string;
};

export function Monogram({ seed, firstName, lastName, fallback, size = "md", className }: MonogramProps) {
  const tone = toneFor(seed || fallback || "x");
  const initials = initialsFor(firstName, lastName, fallback);
  return (
    <div
      aria-hidden
      className={cn(
        "mono-grid grid place-items-center rounded-sm font-mono font-medium tracking-wider select-none",
        SIZE_CLASS[size],
        tone,
        className,
      )}
    >
      {initials}
    </div>
  );
}
