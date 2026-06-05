import { useEffect, useMemo, useState } from "react";
import { ArrowUpRight } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DEMO_ACCOUNT_GROUPS,
  TIER_LABEL,
  type DemoAccount,
} from "@/pages/login.demo-accounts";

// ────────────────────────────────────────────────────────────────────────
// Demo account picker — a two-pane "step into any role" dialog ported from
// the dentalOS live-demo. Left rail = demo tenants, right pane = that
// tenant's seeded users. Tapping a user signs in instantly (each demo
// account carries its own tenant + password). Driven by the static
// DEMO_ACCOUNT_GROUPS — no API call, since the login page is anonymous and
// the API can't safely advertise credentials. Visibility is the caller's
// responsibility (env.demoMode).
// ────────────────────────────────────────────────────────────────────────

interface DemoAccountsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Fired when a user is tapped — the caller signs in with these creds. */
  onPick: (account: DemoAccount) => void;
}

export function DemoAccountsDialog({ open, onOpenChange, onPick }: DemoAccountsDialogProps) {
  const tenants = DEMO_ACCOUNT_GROUPS;
  const [activeIdx, setActiveIdx] = useState(0);

  // Reset to the first tenant each time the dialog re-opens.
  useEffect(() => {
    if (open) setActiveIdx(0);
  }, [open]);

  const activeTenant = tenants[activeIdx];

  const handlePick = (account: DemoAccount) => {
    onOpenChange(false);
    onPick(account);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="overflow-hidden rounded-2xl border-border/70 p-0 sm:max-w-[680px]">
        <DialogTitle className="sr-only">Demo accounts</DialogTitle>
        <DialogDescription className="sr-only">
          Pick a demo tenant and account to sign in with.
        </DialogDescription>

        {/* Atmospheric gradients */}
        <div
          className="pointer-events-none absolute -top-40 -right-40 size-[420px] rounded-full bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--primary)_16%,transparent),transparent)] blur-3xl"
          aria-hidden
        />
        <div
          className="pointer-events-none absolute -bottom-32 -left-32 size-[320px] rounded-full bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--saffron)_12%,transparent),transparent)] blur-3xl"
          aria-hidden
        />

        {/* Header */}
        <header className="fsh-enter relative px-7 pt-7 pb-5">
          <div className="mb-2.5 flex items-center gap-2">
            <span className="relative flex size-1.5">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75" />
              <span className="relative inline-flex size-1.5 rounded-full bg-primary" />
            </span>
            <span className="font-mono text-[10px] font-semibold uppercase tracking-[0.22em] text-primary/85">
              Live demo
            </span>
          </div>
          <h2 className="font-display text-[22px] font-semibold leading-[1.15] tracking-[-0.01em] text-foreground">
            Step into any role.
          </h2>
          <p className="mt-1.5 max-w-[80%] text-[12.5px] leading-relaxed text-muted-foreground/80">
            Explore fullstackhero as any user across the demo tenants — we'll sign you in instantly.
          </p>
        </header>

        {/* Body */}
        <div className="relative grid grid-cols-1 border-t border-border/60 sm:grid-cols-[200px_1fr]">
          <TenantRail tenants={tenants} activeIdx={activeIdx} onSelect={setActiveIdx} />

          <div className="relative bg-background/30 sm:border-l sm:border-border/60">
            {activeTenant && (
              <UserPane key={activeTenant.tenant} tenant={activeTenant} onPick={handlePick} />
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="relative flex items-center justify-between border-t border-border/60 bg-background/40 px-7 py-3">
          <p className="text-[10.5px] tracking-wide text-muted-foreground/60">
            <span className="font-mono text-muted-foreground/80">demo only</span>
            <span className="mx-2 text-muted-foreground/30">·</span>
            Resets with every reseed.
          </p>
          <kbd className="hidden items-center gap-1 rounded border border-border/60 bg-background/60 px-1.5 py-0.5 font-mono text-[9.5px] text-muted-foreground/60 sm:inline-flex">
            esc
          </kbd>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ------------------------------------------------------------------

type DemoTenant = (typeof DEMO_ACCOUNT_GROUPS)[number];

function TenantRail({
  tenants,
  activeIdx,
  onSelect,
}: {
  tenants: readonly DemoTenant[];
  activeIdx: number;
  onSelect: (idx: number) => void;
}) {
  return (
    <nav
      className="relative flex gap-1 overflow-x-auto p-3 sm:flex-col sm:overflow-visible"
      aria-label="Demo tenants"
    >
      {tenants.map((tenant, i) => {
        const isActive = i === activeIdx;
        return (
          <button
            key={tenant.tenant}
            type="button"
            onClick={() => onSelect(i)}
            aria-pressed={isActive}
            className="group relative flex min-w-[180px] cursor-pointer items-center gap-3 rounded-xl px-3 py-2.5 text-left outline-none transition-colors duration-200 focus-visible:ring-2 focus-visible:ring-primary/25 sm:min-w-0"
          >
            {isActive && (
              <span
                className="absolute inset-0 rounded-xl border border-primary/15 bg-primary/[0.06] shadow-[inset_0_0_0_1px_color-mix(in_srgb,var(--primary)_8%,transparent)]"
                aria-hidden
              />
            )}
            <span
              className={[
                "relative flex size-9 shrink-0 items-center justify-center rounded-lg",
                "font-display text-[15px] font-semibold tabular-nums",
                "transition-all duration-300",
                isActive
                  ? "bg-primary text-primary-foreground shadow-[0_4px_14px_color-mix(in_srgb,var(--primary)_35%,transparent)]"
                  : "border border-border/80 bg-background text-muted-foreground/70 group-hover:border-primary/30 group-hover:text-primary",
              ].join(" ")}
            >
              {tenant.tenantLabel.charAt(0).toUpperCase()}
            </span>
            <span className="relative min-w-0 flex-1">
              <span
                className={[
                  "block truncate text-[12.5px] font-semibold leading-tight tracking-tight transition-colors",
                  isActive ? "text-foreground" : "text-muted-foreground group-hover:text-foreground",
                ].join(" ")}
              >
                {tenant.tenantLabel}
              </span>
              <span className="mt-0.5 block font-mono text-[10px] uppercase tracking-wider text-muted-foreground/55">
                {tenant.accounts.length} users
              </span>
            </span>
          </button>
        );
      })}
    </nav>
  );
}

// ------------------------------------------------------------------

function UserPane({
  tenant,
  onPick,
}: {
  tenant: DemoTenant;
  onPick: (account: DemoAccount) => void;
}) {
  return (
    <div className="fsh-enter max-h-[340px] overflow-y-auto p-3">
      <div className="mb-1 flex items-baseline gap-2 px-2 pb-2">
        <span className="font-mono text-[9.5px] font-semibold uppercase tabular-nums tracking-[0.18em] text-primary/55">
          Users
        </span>
        <div className="relative top-[-2px] h-px flex-1 bg-border/70" />
        <span className="font-mono text-[9.5px] uppercase tracking-[0.15em] text-muted-foreground/50">
          tap to sign in
        </span>
      </div>
      <div className="space-y-0.5">
        {tenant.accounts.map((account, i) => (
          <UserRow key={account.email} account={account} delay={i * 40} onPick={onPick} />
        ))}
      </div>
    </div>
  );
}

function UserRow({
  account,
  delay,
  onPick,
}: {
  account: DemoAccount;
  delay: number;
  onPick: (account: DemoAccount) => void;
}) {
  const fullName = `${account.firstName} ${account.lastName}`;
  const roleLabel = TIER_LABEL[account.tier];

  const initials = useMemo(() => {
    const parts = fullName.replace(/^Dr\.?\s+/i, "").split(/\s+/);
    return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase();
  }, [fullName]);

  return (
    <button
      type="button"
      onClick={() => onPick(account)}
      style={{ animationDelay: `${delay}ms` }}
      className="fsh-enter group relative flex w-full cursor-pointer items-center gap-3 overflow-hidden rounded-lg px-2 py-2.5 text-left outline-none transition-all duration-200 hover:translate-x-0.5 focus-visible:bg-primary/[0.04] active:scale-[0.99]"
    >
      {/* Gradient wash — left → transparent on hover */}
      <span
        className="pointer-events-none absolute inset-0 rounded-lg bg-[linear-gradient(90deg,color-mix(in_srgb,var(--primary)_9%,transparent),transparent_70%)] opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        aria-hidden
      />

      {/* Initial monogram */}
      <span className="relative flex size-8 shrink-0 items-center justify-center rounded-full border border-border/70 bg-background font-mono text-[10.5px] font-semibold text-muted-foreground/80 transition-all duration-300 group-hover:border-primary group-hover:bg-primary group-hover:text-primary-foreground group-hover:shadow-[0_0_0_3px_color-mix(in_srgb,var(--primary)_18%,transparent)]">
        {initials || "?"}
      </span>

      {/* Content */}
      <div className="relative min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="text-[13px] font-medium leading-tight tracking-tight text-foreground">
            {fullName}
          </span>
          <span className="text-[9.5px] font-semibold uppercase tracking-[0.14em] text-muted-foreground/60 transition-colors duration-200 group-hover:text-primary/80">
            {roleLabel}
          </span>
        </div>
        <div className="mt-1 truncate font-mono text-[10.5px] leading-tight text-muted-foreground/55">
          {account.email}
        </div>
      </div>

      {/* Arrow */}
      <span
        className="relative flex size-6 shrink-0 -translate-x-1 items-center justify-center rounded-md text-muted-foreground/30 opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:bg-primary/10 group-hover:text-primary group-hover:opacity-100"
        aria-hidden
      >
        <ArrowUpRight className="size-3.5" strokeWidth={2.25} />
      </span>
    </button>
  );
}
