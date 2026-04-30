import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { LogOut, ShieldAlert, UserCog } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/**
 * Sticky banner shown across every page while the operator is
 * impersonating another user. Reads the act_* claims from the live
 * access token via AuthContext.impersonation. Click "End impersonation"
 * to call the End endpoint and restore the operator session.
 *
 * Visual treatment is deliberately loud — full-width amber bar with
 * grain + a stripe pattern so it's impossible to forget you're not
 * acting as yourself. This is a security-sensitive mode.
 */
export function ImpersonationBanner() {
  const { impersonation, user, stopImpersonation } = useAuth();
  const navigate = useNavigate();
  const [pending, setPending] = useState(false);

  const stop = useMutation({
    mutationFn: async () => {
      setPending(true);
      try {
        await stopImpersonation();
      } finally {
        setPending(false);
      }
    },
    onSuccess: () => {
      toast.success("Returned to your session");
      navigate("/", { replace: true });
    },
    onError: (err) => {
      toast.error("Could not end impersonation cleanly", {
        description: err instanceof Error ? err.message : "Restored local session.",
      });
      navigate("/", { replace: true });
    },
  });

  if (!impersonation) return null;

  const subjectLabel = user?.name ?? user?.email ?? "Unknown";
  const tenantLabel = user?.tenant ?? "—";
  const actorLabel = impersonation.actorName ?? impersonation.actorUserId.slice(0, 8) + "…";

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "relative z-40 flex flex-wrap items-center justify-between gap-3 overflow-hidden",
        "border-b border-[oklch(from_var(--color-warning)_l_c_h_/_0.45)]",
        "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.18)]",
        "px-4 py-2.5 sm:px-6",
      )}
    >
      {/* Diagonal stripe pattern — quiet at low opacity but unmistakable */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.10]"
        style={{
          backgroundImage:
            "repeating-linear-gradient(135deg, transparent 0 8px, oklch(from var(--color-warning) l c h) 8px 9px)",
        }}
      />

      <div className="flex min-w-0 flex-wrap items-center gap-2">
        <span
          aria-hidden
          className={cn(
            "grid h-7 w-7 shrink-0 place-items-center rounded-md",
            "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.20)] text-[var(--color-warning)]",
            "ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.40)]",
          )}
        >
          <ShieldAlert className="h-3.5 w-3.5" />
        </span>
        <span className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.18em] text-[var(--color-warning)]">
          Impersonating
        </span>
        <span className="text-display truncate text-[14px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)]">
          {subjectLabel}
        </span>
        <code className="rounded bg-[oklch(from_var(--color-warning)_l_c_h_/_0.18)] px-1.5 py-0.5 font-mono text-[11px] font-medium text-[var(--color-warning)]">
          {tenantLabel}
        </code>
        <span className="hidden font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] sm:inline">
          · acting as your operator <UserCog className="inline h-3 w-3 -translate-y-px" /> {actorLabel}
        </span>
      </div>

      <Button
        size="sm"
        variant="outline"
        onClick={() => stop.mutate()}
        disabled={pending}
        className={cn(
          "shrink-0 border-[oklch(from_var(--color-warning)_l_c_h_/_0.45)]",
          "hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
        )}
      >
        <LogOut className="mr-1.5 h-3.5 w-3.5" />
        {pending ? "Ending…" : "End impersonation"}
      </Button>
    </div>
  );
}
