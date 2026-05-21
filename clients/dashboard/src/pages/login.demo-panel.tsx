import { useEffect, useMemo, useState } from "react";
import {
  ClipboardCheck,
  Copy,
  FlaskConical,
  Search,
  Sprout,
  X,
} from "lucide-react";
import { Avatar } from "@/components/ui/avatar";
import { ToneIconTile } from "@/components/list";
import { cn } from "@/lib/cn";
import {
  DEMO_ACCOUNT_GROUPS,
  DEMO_PASSWORD,
  TIER_LABEL,
  type DemoAccount,
  type DemoTier,
} from "@/pages/login.demo-accounts";

/**
 * Dev-only credential switcher rendered inside the login Dialog. Matches
 * the dentalOS warm-paper vocabulary used by the rest of the auth flow:
 * rose+saffron accents on the calm card surface, no amber wash, no
 * mesh-and-grain backdrop. A small saffron DEV chip keeps the non-prod
 * signal without dragging in the impersonation palette.
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
    <div
      role="region"
      aria-label="Development demo accounts"
      className="fsh-enter flex flex-col"
    >
      {/* Header — Outfit title + small saffron DEV chip. No long blurb. */}
      <header className="border-b border-[var(--color-border)] px-6 pt-6 pb-4">
        <div className="flex items-center gap-2.5">
          <ToneIconTile icon={FlaskConical} tone="saffron" size="md" />
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <h2 className="font-display text-[16px] font-semibold tracking-tight text-[var(--color-foreground)]">
                Demo accounts
              </h2>
              <span
                className={cn(
                  "rounded-full px-1.5 py-0.5 text-[9.5px] font-semibold uppercase tracking-wider",
                  "bg-[oklch(from_var(--color-saffron)_l_c_h_/_0.16)] text-[var(--color-saffron)]",
                )}
              >
                DEV
              </span>
            </div>
            <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
              Pick a seeded account to prefill the login form.
            </p>
          </div>
        </div>
      </header>

      {/* Search */}
      <div className="px-6 pt-4 pb-2">
        <div
          className={cn(
            "group/search relative flex h-9 items-center rounded-md",
            "bg-[var(--color-secondary)]",
            "ring-1 ring-inset ring-[var(--color-border)]",
            "focus-within:ring-[oklch(from_var(--color-primary)_l_c_h_/_0.40)]",
            "transition-colors duration-[var(--duration-fast)]",
          )}
        >
          <Search className="ml-2.5 size-3.5 text-[var(--color-muted-foreground)]" />
          <input
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            placeholder="Filter by name, email, persona…"
            aria-label="Filter demo accounts"
            className={cn(
              "h-full flex-1 border-0 bg-transparent px-2 text-[12.5px]",
              "outline-none placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.65)]",
            )}
          />
          {filter && (
            <button
              type="button"
              onClick={() => setFilter("")}
              aria-label="Clear filter"
              className={cn(
                "mr-1 grid size-6 cursor-pointer place-items-center rounded",
                "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
              )}
            >
              <X className="size-3" />
            </button>
          )}
        </div>
      </div>

      {/* Tenant groups */}
      <ul className="max-h-[60vh] overflow-y-auto px-3 pt-2 pb-3">
        {filteredGroups.length === 0 ? (
          <li className="px-3 py-8 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
            No demo accounts match "{filter}".
          </li>
        ) : (
          filteredGroups.map((group) => (
            <li key={group.tenant} className="mb-2">
              <div className="mt-1 flex items-baseline gap-2 px-3 pb-1.5">
                <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-primary)]">
                  {group.tenantLabel}
                </span>
                <span
                  aria-hidden
                  className="h-px flex-1 bg-[var(--color-border)]"
                />
                <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
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
            ? "bg-[var(--color-primary-soft)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]"
            : "hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.05)]",
        )}
      >
        <Avatar
          name={fullName}
          size="sm"
          halo={account.tier === "tenant-admin"}
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <span className="truncate text-[13px] font-semibold tracking-tight text-[var(--color-foreground)]">
              {fullName}
            </span>
            <TierBadge tier={account.tier} />
          </div>
          <div className="mt-0.5 flex items-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]">
            <span className="truncate">{account.email}</span>
          </div>
          <p className="mt-0.5 truncate text-[11px] italic text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.8)]">
            {account.persona}
          </p>
        </div>
        <span
          aria-hidden
          className={cn(
            "shrink-0 text-[11px] font-semibold uppercase tracking-wider transition-colors",
            active
              ? "text-[var(--color-primary)]"
              : "text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0)] group-hover/row:text-[var(--color-muted-foreground)]",
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
    "bg-[oklch(from_var(--color-saffron)_l_c_h_/_0.18)] text-[var(--color-saffron)]",
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
        "shrink-0 rounded-full px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider",
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
        "flex items-center justify-between gap-2 border-t border-[var(--color-border)]",
        "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.04)] px-6 py-3",
      )}
    >
      <div className="flex items-center gap-2 text-[11px]">
        <Sprout className="size-3 text-[var(--color-saffron)]" />
        <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          Shared password
        </span>
        <code className="rounded bg-[var(--color-primary-soft)] px-1.5 py-0.5 font-mono text-[11px] font-semibold tracking-tight text-[var(--color-primary)]">
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
          "text-[11px] font-semibold uppercase tracking-wider",
          "transition-colors duration-[var(--duration-fast)]",
          copied
            ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.10)] text-[var(--color-success)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-secondary)] hover:text-[var(--color-foreground)]",
        )}
      >
        {copied ? (
          <>
            <ClipboardCheck className="size-3" /> copied
          </>
        ) : (
          <>
            <Copy className="size-3" /> copy
          </>
        )}
      </button>
    </div>
  );
}
