import { useEffect, useMemo, useState } from "react";
import {
  ChevronDown,
  ClipboardCheck,
  Copy,
  FlaskConical,
  Search,
  Sprout,
  X,
} from "lucide-react";
import { Avatar } from "@/components/ui/avatar";
import { cn } from "@/lib/cn";
import {
  DEMO_ACCOUNT_GROUPS,
  DEMO_PASSWORD,
  TIER_LABEL,
  type DemoAccount,
  type DemoTier,
} from "@/pages/login.demo-accounts";

/**
 * Dev-only credential switcher panel for the login page.
 *
 * Design intent — this is a *signal* that you are in development mode. It
 * leans into an amber palette (mirroring the impersonation banner) so it's
 * visually impossible to confuse with a production sign-in surface.
 *
 * Layout: side card on ≥md, collapsed accordion above on mobile. Each row
 * prefills the form via `onSelect` (no auto-submit — operators still click
 * Sign in). A copy-password affordance saves a trip to the backend seed code.
 *
 * Visibility: caller is responsible for gating on `import.meta.env.DEV`.
 * Bundled-out in production builds via Vite static replacement.
 */
export function LoginDemoPanel({
  current,
  onSelect,
}: {
  /** The currently-prefilled email — used to highlight the active row. */
  current: { email: string; tenant: string };
  onSelect: (account: DemoAccount) => void;
}) {
  const [open, setOpen] = useState(true);
  const [filter, setFilter] = useState("");

  const filteredGroups = useMemo(() => {
    const q = filter.trim().toLowerCase();
    if (!q) return DEMO_ACCOUNT_GROUPS;
    return DEMO_ACCOUNT_GROUPS.map((g) => ({
      ...g,
      accounts: g.accounts.filter(
        (a) =>
          a.email.toLowerCase().includes(q) ||
          `${a.firstName} ${a.lastName}`.toLowerCase().includes(q) ||
          a.persona.toLowerCase().includes(q),
      ),
    })).filter((g) => g.accounts.length > 0);
  }, [filter]);

  return (
    <aside
      role="region"
      aria-label="Development demo accounts"
      className={cn(
        "fsh-enter fsh-enter-2 relative w-full overflow-hidden rounded-[20px]",
        "card-shell",
        // Amber-tinted surface — flags this as a non-prod affordance at a glance.
        "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.05)]",
        "ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.30)]",
      )}
    >
      {/* Atmospheric backdrop — radial mesh + grain, amber-tinted */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(60% 70% at 0% 0%, oklch(from var(--color-warning) l c h / 0.18), transparent 60%),
            radial-gradient(50% 60% at 100% 100%, oklch(from var(--color-warning) l c h / 0.06), transparent 70%)
          `,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <header
        className={cn(
          "flex items-center justify-between gap-3 px-5 py-4",
          open && "border-b border-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
        )}
      >
        <div className="flex min-w-0 items-center gap-2.5">
          <span
            aria-hidden
            className={cn(
              "grid h-7 w-7 shrink-0 place-items-center rounded-md",
              "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.20)] text-[var(--color-warning)]",
              "ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.40)]",
            )}
          >
            <FlaskConical className="h-3.5 w-3.5" />
          </span>
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <span className="font-mono text-[10.5px] font-semibold uppercase tracking-[0.18em] text-[var(--color-warning)]">
                DEV · Demo accounts
              </span>
            </div>
            <p className="mt-0.5 truncate text-[11.5px] text-[var(--color-muted-foreground)]">
              Visible only when running with the dev seed.
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={() => setOpen((o) => !o)}
          aria-expanded={open}
          aria-label={open ? "Collapse demo accounts" : "Expand demo accounts"}
          className={cn(
            "grid h-7 w-7 shrink-0 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.16)] hover:text-[var(--color-warning)]",
            "transition-colors duration-[var(--duration-fast)]",
          )}
        >
          <ChevronDown
            className={cn(
              "h-4 w-4 transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
              !open && "-rotate-90",
            )}
          />
        </button>
      </header>

      {open && (
        <div>
          {/* Search */}
          <div className="px-5 pt-3">
            <div
              className={cn(
                "group/search relative flex h-9 items-center rounded-md",
                "bg-[oklch(from_var(--color-background)_l_c_h_/_0.70)]",
                "ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
                "focus-within:ring-[oklch(from_var(--color-warning)_l_c_h_/_0.45)]",
              )}
            >
              <Search className="ml-2.5 h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
              <input
                value={filter}
                onChange={(e) => setFilter(e.target.value)}
                placeholder="Filter by name, email, persona…"
                aria-label="Filter demo accounts"
                className={cn(
                  "h-full flex-1 border-0 bg-transparent px-2 text-[12.5px]",
                  "outline-none placeholder:text-[var(--color-muted-foreground)]/80",
                )}
              />
              {filter && (
                <button
                  type="button"
                  onClick={() => setFilter("")}
                  aria-label="Clear filter"
                  className="mr-1 grid h-6 w-6 place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
                >
                  <X className="h-3 w-3" />
                </button>
              )}
            </div>
          </div>

          {/* Tenant groups */}
          <ul className="mt-3 max-h-[60vh] overflow-y-auto px-1.5 pb-2">
            {filteredGroups.length === 0 ? (
              <li className="px-4 py-6 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                No demo accounts match "{filter}".
              </li>
            ) : (
              filteredGroups.map((group) => (
                <li key={group.tenant} className="mb-2">
                  <div className="mt-1 flex items-baseline gap-2 px-3 pb-1.5">
                    <span className="font-mono text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-warning)]">
                      {group.tenantLabel}
                    </span>
                    <span aria-hidden className="h-px flex-1 bg-[oklch(from_var(--color-warning)_l_c_h_/_0.25)]" />
                    <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                      {group.blurb}
                    </span>
                  </div>
                  <ul>
                    {group.accounts.map((account) => (
                      <DemoRow
                        key={account.email}
                        account={account}
                        active={account.email === current.email && account.tenant === current.tenant}
                        onSelect={onSelect}
                      />
                    ))}
                  </ul>
                </li>
              ))
            )}
          </ul>

          {/* Shared password footer */}
          <PasswordFooter />
        </div>
      )}
    </aside>
  );
}

function DemoRow({
  account,
  active,
  onSelect,
}: {
  account: DemoAccount;
  active: boolean;
  onSelect: (account: DemoAccount) => void;
}) {
  const fullName = `${account.firstName} ${account.lastName}`;
  return (
    <li>
      <button
        type="button"
        onClick={() => onSelect(account)}
        aria-pressed={active}
        className={cn(
          "group/row relative flex w-full cursor-pointer items-center gap-3 rounded-lg px-3 py-2 text-left",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          active
            ? "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.15)] ring-1 ring-inset ring-[oklch(from_var(--color-warning)_l_c_h_/_0.35)]"
            : "hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)]",
        )}
      >
        <Avatar
          name={fullName}
          size="sm"
          halo={account.tier === "tenant-admin"}
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <span className="truncate text-[13px] font-semibold tracking-tight">{fullName}</span>
            <TierBadge tier={account.tier} />
          </div>
          <div className="mt-0.5 flex items-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
            <span className="truncate font-mono">{account.email}</span>
          </div>
          <p className="mt-0.5 truncate text-[11px] italic text-[var(--color-muted-foreground)]/80">
            {account.persona}
          </p>
        </div>
        <span
          aria-hidden
          className={cn(
            "shrink-0 font-mono text-[10px] uppercase tracking-[0.14em]",
            active ? "text-[var(--color-warning)]" : "text-[var(--color-muted-foreground)]/0 group-hover/row:text-[var(--color-muted-foreground)]",
            "transition-colors",
          )}
        >
          {active ? "loaded" : "use →"}
        </span>
      </button>
    </li>
  );
}

const TIER_VARIANT: Record<DemoTier, string> = {
  "tenant-admin":
    "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.18)] text-[var(--color-warning)]",
  manager:
    "bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
  support:
    "bg-[oklch(from_var(--color-info)_l_c_h_/_0.16)] text-[var(--color-info)]",
  basic:
    "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
};

function TierBadge({ tier }: { tier: DemoTier }) {
  return (
    <span
      className={cn(
        "shrink-0 rounded-full px-1.5 py-0.5 font-mono text-[9px] font-medium uppercase tracking-[0.12em]",
        TIER_VARIANT[tier],
      )}
    >
      {TIER_LABEL[tier]}
    </span>
  );
}

function PasswordFooter() {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!copied) return;
    const t = setTimeout(() => setCopied(false), 1400);
    return () => clearTimeout(t);
  }, [copied]);

  const onCopy = async () => {
    try {
      await navigator.clipboard.writeText(DEMO_PASSWORD);
      setCopied(true);
    } catch {
      // ignore — clipboard unavailable in some test contexts
    }
  };

  return (
    <div
      className={cn(
        "flex items-center justify-between gap-2 border-t border-[oklch(from_var(--color-warning)_l_c_h_/_0.20)]",
        "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.06)] px-5 py-3",
      )}
    >
      <div className="flex items-center gap-2 text-[11px]">
        <Sprout className="h-3 w-3 text-[var(--color-warning)]" />
        <span className="font-mono uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          Shared password
        </span>
        <code className="rounded bg-[oklch(from_var(--color-warning)_l_c_h_/_0.12)] px-1.5 py-0.5 font-mono text-[11px] font-semibold tracking-tight text-[var(--color-warning)]">
          {DEMO_PASSWORD}
        </code>
      </div>
      <button
        type="button"
        onClick={onCopy}
        title={copied ? "Copied!" : "Copy password"}
        aria-label="Copy password"
        className={cn(
          "inline-flex h-7 cursor-pointer items-center gap-1 rounded-md px-2",
          "font-mono text-[10.5px] uppercase tracking-[0.14em]",
          "transition-colors duration-[var(--duration-fast)]",
          copied
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.18)] hover:text-[var(--color-warning)]",
        )}
      >
        {copied ? (
          <>
            <ClipboardCheck className="h-3 w-3" /> copied
          </>
        ) : (
          <>
            <Copy className="h-3 w-3" /> copy
          </>
        )}
      </button>
    </div>
  );
}

