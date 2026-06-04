import { useEffect } from "react";
import { ArrowUpRight, ShieldCheck } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { ADMIN_DEMO_ACCOUNTS, type DemoAccount } from "@/pages/login.demo-accounts";

// ────────────────────────────────────────────────────────────────────────
// DemoAccountsDialog — dev-only demo account picker for the admin app.
//
// Admin surfaces a single root/superadmin account (vs. the dashboard's
// multi-tenant tenant-picker). The layout is a single-pane account list
// rather than the dashboard's two-pane tenant-rail, since there's only
// one operator tenant. Tapping an account signs in instantly and closes
// the dialog. Gating (import.meta.env.DEV) is the caller's responsibility.
// ────────────────────────────────────────────────────────────────────────

interface DemoAccountsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Fired when an account row is tapped — caller signs in with these creds. */
  onPick: (account: DemoAccount) => void;
}

export function DemoAccountsDialog({ open, onOpenChange, onPick }: DemoAccountsDialogProps) {
  // Nothing to reset on re-open (single account list, no tenant rail).
  useEffect(() => {}, [open]);

  const handlePick = (account: DemoAccount) => {
    onOpenChange(false);
    onPick(account);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="overflow-hidden rounded-2xl border-border/70 p-0 sm:max-w-[480px]">
        <DialogTitle className="sr-only">Demo accounts</DialogTitle>
        <DialogDescription className="sr-only">
          Pick a demo account to sign in to the admin console.
        </DialogDescription>

        {/* Atmospheric gradient wash */}
        <div
          className="pointer-events-none absolute -top-32 -right-32 size-[320px] rounded-full bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--color-primary)_14%,transparent),transparent)] blur-3xl"
          aria-hidden
        />
        <div
          className="pointer-events-none absolute -bottom-24 -left-24 size-[240px] rounded-full bg-[radial-gradient(closest-side,color-mix(in_srgb,var(--color-accent-signal,var(--color-primary))_10%,transparent),transparent)] blur-3xl"
          aria-hidden
        />

        {/* Header */}
        <header className="fsh-enter relative px-7 pt-7 pb-5">
          <div className="mb-2.5 flex items-center gap-2">
            <span className="relative flex size-1.5">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-[var(--color-primary)] opacity-75" />
              <span className="relative inline-flex size-1.5 rounded-full bg-[var(--color-primary)]" />
            </span>
            <span className="font-mono text-[10px] font-semibold uppercase tracking-[0.22em] text-[oklch(from_var(--color-primary)_l_c_h_/_0.85)]">
              Dev · demo
            </span>
          </div>
          <h2 className="font-display text-[20px] font-semibold leading-[1.15] tracking-[-0.01em] text-[var(--color-foreground)]">
            Sign in as operator.
          </h2>
          <p className="mt-1.5 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]/80">
            Tap an account below — we'll fill the credentials and sign you in instantly.
          </p>
        </header>

        {/* Account list */}
        <div className="relative border-t border-[var(--color-border)]/60 bg-[var(--color-background)]/30 px-4 py-3">
          <div className="mb-2 flex items-baseline gap-2 px-2">
            <span className="font-mono text-[9.5px] font-semibold uppercase tabular-nums tracking-[0.18em] text-[oklch(from_var(--color-primary)_l_c_h_/_0.55)]">
              Operator accounts
            </span>
            <div className="relative top-[-2px] h-px flex-1 bg-[var(--color-border)]/70" />
            <span className="font-mono text-[9.5px] uppercase tracking-[0.15em] text-[var(--color-muted-foreground)]/50">
              tap to sign in
            </span>
          </div>

          <div className="space-y-0.5">
            {ADMIN_DEMO_ACCOUNTS.map((account, i) => (
              <AccountRow
                key={account.email}
                account={account}
                delay={i * 40}
                onPick={handlePick}
              />
            ))}
          </div>
        </div>

        {/* Footer */}
        <div className="relative flex items-center justify-between border-t border-[var(--color-border)]/60 bg-[var(--color-background)]/40 px-7 py-3">
          <p className="flex items-center gap-1.5 text-[10.5px] text-[var(--color-muted-foreground)]/60">
            <ShieldCheck className="size-3 shrink-0" />
            <span>
              <span className="font-mono text-[var(--color-muted-foreground)]/80">dev only</span>
              <span className="mx-2 text-[var(--color-muted-foreground)]/30">·</span>
              Not visible in production.
            </span>
          </p>
          <kbd className="hidden items-center gap-1 rounded border border-[var(--color-border)]/60 bg-[var(--color-background)]/60 px-1.5 py-0.5 font-mono text-[9.5px] text-[var(--color-muted-foreground)]/60 sm:inline-flex">
            esc
          </kbd>
        </div>
      </DialogContent>
    </Dialog>
  );
}

// ─── AccountRow ──────────────────────────────────────────────────────────────

function AccountRow({
  account,
  delay,
  onPick,
}: {
  account: DemoAccount;
  delay: number;
  onPick: (account: DemoAccount) => void;
}) {
  return (
    <button
      type="button"
      onClick={() => onPick(account)}
      style={{ animationDelay: `${delay}ms` }}
      className="fsh-enter group relative flex w-full cursor-pointer items-center gap-3 overflow-hidden rounded-lg px-2 py-2.5 text-left outline-none transition-all duration-200 hover:translate-x-0.5 focus-visible:bg-[var(--color-primary)]/[0.04] active:scale-[0.99]"
    >
      {/* Hover gradient wash */}
      <span
        className="pointer-events-none absolute inset-0 rounded-lg bg-[linear-gradient(90deg,color-mix(in_srgb,var(--color-primary)_9%,transparent),transparent_70%)] opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        aria-hidden
      />

      {/* Avatar */}
      <span className="relative flex size-9 shrink-0 items-center justify-center rounded-full border border-[var(--color-border)]/70 bg-[var(--color-background)] font-mono text-[11px] font-semibold text-[var(--color-muted-foreground)]/80 transition-all duration-300 group-hover:border-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-[var(--color-primary-foreground)] group-hover:shadow-[0_0_0_3px_color-mix(in_srgb,var(--color-primary)_18%,transparent)]">
        {account.initials}
      </span>

      {/* Content */}
      <div className="relative min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="text-[13px] font-semibold leading-tight tracking-tight text-[var(--color-foreground)]">
            {account.label}
          </span>
          <span className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[9.5px] uppercase tracking-wider text-[var(--color-muted-foreground)]/70 transition-colors duration-200 group-hover:text-[var(--color-primary)]/80">
            {account.tenant}
          </span>
        </div>
        <div className="mt-0.5 truncate font-mono text-[10.5px] leading-tight text-[var(--color-muted-foreground)]/55">
          {account.email}
        </div>
        <div className="mt-0.5 text-[11px] italic leading-tight text-[var(--color-muted-foreground)]/60">
          {account.persona}
        </div>
      </div>

      {/* Arrow indicator */}
      <span
        className="relative flex size-6 shrink-0 -translate-x-1 items-center justify-center rounded-md text-[var(--color-muted-foreground)]/30 opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:bg-[var(--color-primary)]/10 group-hover:text-[var(--color-primary)] group-hover:opacity-100"
        aria-hidden
      >
        <ArrowUpRight className="size-3.5" strokeWidth={2.25} />
      </span>
    </button>
  );
}
