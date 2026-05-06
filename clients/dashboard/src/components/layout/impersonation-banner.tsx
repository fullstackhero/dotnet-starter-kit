import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { ArrowRight, LogOut, ShieldAlert, UserCog } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

/**
 * Sticky banner shown across every page while the operator is impersonating
 * another user. Reads the act_* claims from the live access token via
 * AuthContext.impersonation.
 *
 * Two palettes:
 *   - same-tenant impersonation → amber, "Impersonating <name>"
 *   - cross-tenant impersonation → red/destructive, "CROSS-TENANT
 *     IMPERSONATION · {actorTenant} → {tenant}". Cross-tenant means a
 *     SuperAdmin acting *as* a member of a different tenant — much
 *     larger blast radius, deserves a louder warning.
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
  const actorTenantLabel = impersonation.actorTenant ?? "—";
  const isCrossTenant =
    impersonation.actorTenant !== undefined && impersonation.actorTenant !== user?.tenant;

  // Token name flips between two CSS variables so every styled child can use
  // the same expression. Cross-tenant escalates to --color-destructive,
  // same-tenant stays on --color-warning.
  const tone = isCrossTenant ? "var(--color-destructive)" : "var(--color-warning)";

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "relative z-40 flex flex-wrap items-center justify-between gap-3 overflow-hidden",
        "border-y px-4 py-2.5 sm:px-6",
      )}
      style={{
        borderColor: `oklch(from ${tone} l c h / 0.50)`,
        backgroundColor: isCrossTenant
          ? `oklch(from ${tone} l c h / 0.16)`
          : `oklch(from ${tone} l c h / 0.18)`,
      }}
    >
      {/* Diagonal stripe pattern — pulses harder for cross-tenant */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          opacity: isCrossTenant ? 0.16 : 0.10,
          backgroundImage: `repeating-linear-gradient(135deg, transparent 0 8px, oklch(from ${tone} l c h) 8px 9px)`,
        }}
      />

      <div className="flex min-w-0 flex-wrap items-center gap-2">
        <span
          aria-hidden
          className="grid h-7 w-7 shrink-0 place-items-center rounded-md ring-1 ring-inset"
          style={{
            backgroundColor: `oklch(from ${tone} l c h / 0.22)`,
            color: tone,
            // ring color via inline since ring uses border-color hooks
            // and we need to avoid hardcoding token names
            ['--tw-ring-color' as string]: `oklch(from ${tone} l c h / 0.45)`,
          }}
        >
          <ShieldAlert className="h-3.5 w-3.5" />
        </span>

        {isCrossTenant ? (
          <>
            <span
              className="font-mono text-[10.5px] font-bold uppercase tracking-[0.20em]"
              style={{ color: tone }}
            >
              Cross-tenant impersonation
            </span>
            <TenantChip label={actorTenantLabel} tone={tone} mono />
            <ArrowRight className="h-3 w-3 shrink-0" style={{ color: tone }} aria-hidden />
            <TenantChip label={tenantLabel} tone={tone} mono emphasis />
            <span className="hidden truncate text-[12px] text-[var(--color-foreground)] sm:inline">
              acting as <span className="font-semibold">{subjectLabel}</span>
            </span>
            <span className="hidden font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] sm:inline">
              · operator <UserCog className="inline h-3 w-3 -translate-y-px" /> {actorLabel}
            </span>
          </>
        ) : (
          <>
            <span
              className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.18em]"
              style={{ color: tone }}
            >
              Impersonating
            </span>
            <span className="text-display truncate text-[14px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)]">
              {subjectLabel}
            </span>
            <TenantChip label={tenantLabel} tone={tone} />
            <span className="hidden font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)] sm:inline">
              · acting as your operator <UserCog className="inline h-3 w-3 -translate-y-px" /> {actorLabel}
            </span>
          </>
        )}
      </div>

      <Button
        size="sm"
        variant={isCrossTenant ? "destructive" : "outline"}
        onClick={() => stop.mutate()}
        disabled={pending}
        className={cn(
          "shrink-0",
          !isCrossTenant && "border-[oklch(from_var(--color-warning)_l_c_h_/_0.45)] hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
        )}
      >
        <LogOut className="mr-1.5 h-3.5 w-3.5" />
        {pending ? "Ending…" : "End impersonation"}
      </Button>
    </div>
  );
}

function TenantChip({
  label,
  tone,
  mono,
  emphasis,
}: {
  label: string;
  tone: string;
  mono?: boolean;
  emphasis?: boolean;
}) {
  return (
    <code
      className={cn(
        "rounded px-1.5 py-0.5 font-medium",
        mono ? "font-mono text-[11px]" : "font-mono text-[11px]",
        emphasis && "font-bold",
      )}
      style={{
        backgroundColor: `oklch(from ${tone} l c h / 0.18)`,
        color: tone,
      }}
    >
      {label}
    </code>
  );
}
