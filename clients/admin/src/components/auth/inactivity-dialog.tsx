import * as DialogPrimitive from "@radix-ui/react-dialog";
import { ShieldAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/**
 * InactivityDialog — the inactivity warning. A forced-choice modal (no Esc,
 * no overlay-dismiss, no close X) built on the raw Radix primitives so it can
 * opt out of the shared DialogContent's close affordance. The centerpiece is
 * an SVG countdown ring that drains as the seconds tick and shifts tone
 * primary → amber → red, pulsing in the final stretch. Respects
 * prefers-reduced-motion (the ring steps per second instead of animating).
 */

const RING_RADIUS = 52;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

function ringTone(fraction: number): string {
  if (fraction > 0.5) return "var(--color-primary)";
  if (fraction > 0.2) return "var(--color-warning)";
  return "var(--color-destructive)";
}

export function InactivityDialog({
  open,
  secondsLeft,
  totalSeconds,
  onStay,
  onSignOut,
}: {
  open: boolean;
  secondsLeft: number;
  totalSeconds: number;
  onStay: () => void;
  onSignOut: () => void;
}) {
  const fraction =
    totalSeconds > 0 ? Math.max(0, Math.min(1, secondsLeft / totalSeconds)) : 0;
  const dashOffset = RING_CIRCUMFERENCE * (1 - fraction);
  const tone = ringTone(fraction);
  const urgent = secondsLeft <= 10;

  return (
    <DialogPrimitive.Root open={open}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay
          className={cn(
            "fixed inset-0 z-50 bg-[oklch(0_0_0_/_0.45)] backdrop-blur-[6px]",
            "data-[state=open]:animate-fsh-overlay-in data-[state=closed]:animate-fsh-overlay-out",
          )}
        />
        <DialogPrimitive.Content
          onEscapeKeyDown={(e) => e.preventDefault()}
          onPointerDownOutside={(e) => e.preventDefault()}
          onInteractOutside={(e) => e.preventDefault()}
          aria-describedby="inactivity-desc"
          className={cn(
            "fixed left-1/2 top-1/2 z-50 w-full max-w-[400px] -translate-x-1/2 -translate-y-1/2",
            "rounded-2xl border border-[var(--color-border)] bg-[var(--color-card)] p-7 text-center shadow-xl outline-none",
            "data-[state=open]:animate-fsh-dialog-in data-[state=closed]:animate-fsh-dialog-out",
          )}
        >
          {/* Countdown ring */}
          <div className="relative mx-auto mb-5 size-[132px]">
            <svg viewBox="0 0 120 120" className="size-full -rotate-90" aria-hidden>
              <circle
                cx="60"
                cy="60"
                r={RING_RADIUS}
                fill="none"
                strokeWidth="7"
                className="stroke-[var(--color-muted)]"
              />
              <circle
                cx="60"
                cy="60"
                r={RING_RADIUS}
                fill="none"
                strokeWidth="7"
                strokeLinecap="round"
                stroke={tone}
                strokeDasharray={RING_CIRCUMFERENCE}
                strokeDashoffset={dashOffset}
                className={cn(
                  "motion-safe:transition-[stroke-dashoffset,stroke] motion-safe:duration-1000 motion-safe:ease-linear",
                  urgent && "motion-safe:animate-pulse",
                )}
              />
            </svg>
            <div className="absolute inset-0 grid place-items-center">
              <div className="flex flex-col items-center">
                <span
                  className="font-display text-[34px] font-bold leading-none tabular-nums tracking-tight"
                  style={{ color: tone }}
                >
                  {Math.max(0, secondsLeft)}
                </span>
                <span className="mt-0.5 text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  seconds
                </span>
              </div>
            </div>
          </div>

          <div className="mb-1.5 flex items-center justify-center gap-2">
            <ShieldAlert className="size-4 text-[var(--color-primary)]" aria-hidden />
            <DialogPrimitive.Title className="font-display text-[18px] font-semibold tracking-tight text-[var(--color-foreground)]">
              Still there?
            </DialogPrimitive.Title>
          </div>
          <DialogPrimitive.Description
            id="inactivity-desc"
            className="mx-auto max-w-[300px] text-[13px] leading-relaxed text-[var(--color-muted-foreground)]"
          >
            You&apos;ve been inactive for a while. We&apos;ll sign you out shortly to keep your
            account secure.
          </DialogPrimitive.Description>

          <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-center">
            <Button variant="outline" onClick={onSignOut} className="sm:min-w-[120px]">
              Sign out now
            </Button>
            <Button onClick={onStay} className="sm:min-w-[120px]" autoFocus>
              I&apos;m here
            </Button>
          </div>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
