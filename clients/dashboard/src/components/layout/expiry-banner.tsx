import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle, Clock, X } from "lucide-react";
import { getMyStatus, type TenantStatusDto } from "@/api/billing";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

// Days within which an "Active" subscription nearing its validUpto starts
// surfacing the soft info bar.
const NEARING_EXPIRY_DAYS = 7;

type BannerView =
  | { kind: "none" }
  | {
      kind: "grace";
      daysLeft: number;
      graceEndsLabel: string;
    }
  | { kind: "expired" }
  | {
      kind: "nearing";
      daysLeft: number;
    };

const dateFmt = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "2-digit",
  year: "numeric",
});

/**
 * Whole calendar-ish days from `now` until `iso`, never negative. Uses a
 * ceil so "23 hours left" still reads as "1 day".
 */
function daysUntil(iso: string, now: number = Date.now()): number {
  const target = Date.parse(iso);
  if (Number.isNaN(target)) return 0;
  return Math.max(0, Math.ceil((target - now) / (24 * 60 * 60 * 1000)));
}

function deriveBannerView(
  status: TenantStatusDto | undefined,
  now: number = Date.now(),
): BannerView {
  if (!status) return { kind: "none" };

  if (status.expiryState === "InGrace") {
    return {
      kind: "grace",
      daysLeft: daysUntil(status.graceEndsUtc, now),
      graceEndsLabel: status.graceEndsUtc
        ? dateFmt.format(new Date(status.graceEndsUtc))
        : "soon",
    };
  }

  if (status.expiryState === "Expired") {
    return { kind: "expired" };
  }

  if (status.expiryState === "Active") {
    const daysLeft = daysUntil(status.validUpto, now);
    if (daysLeft <= NEARING_EXPIRY_DAYS) {
      return { kind: "nearing", daysLeft };
    }
  }

  // Fully-healthy state renders nothing in this bar.
  return { kind: "none" };
}

function pluralizeDays(n: number): string {
  return `${n} day${n === 1 ? "" : "s"}`;
}

/**
 * Global subscription health bar. Shows a warning while the tenant is in
 * its post-expiry grace window, and a softer info note when an active
 * subscription is within a week of expiring. Dismissal is per browser
 * session — it reappears on reload while the condition still holds.
 */
export function ExpiryBanner() {
  const { user } = useAuth();
  const [dismissed, setDismissed] = useState(false);

  const statusQuery = useQuery({
    queryKey: ["tenant", "me", "status"],
    queryFn: () => getMyStatus(),
    staleTime: 5 * 60_000,
    // Only meaningful for an authenticated tenant session.
    enabled: !!user,
  });

  const view = deriveBannerView(statusQuery.data);

  if (view.kind === "none") return null;

  // The expired state is the hardest failure — it stays pinned (no dismiss)
  // so the tenant can't lose track that their subscription has lapsed. The
  // softer grace/nearing notices remain dismissible for the session.
  const isExpired = view.kind === "expired";
  if (dismissed && !isExpired) return null;

  const isGrace = view.kind === "grace";
  const tone = isExpired
    ? "var(--color-destructive)"
    : isGrace
      ? "var(--color-warning)"
      : "var(--color-info)";
  const Icon = isExpired || isGrace ? AlertTriangle : Clock;
  const message = isExpired
    ? "Your subscription has expired. Contact your operator to renew and restore full access."
    : isGrace
      ? `Your subscription expired — ${pluralizeDays(view.daysLeft)} of grace left (until ${view.graceEndsLabel}). Contact your operator to renew.`
      : `Your subscription expires in ${pluralizeDays(view.daysLeft)}.`;

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        "relative z-30 flex flex-wrap items-center justify-between gap-3 overflow-hidden",
        "border-b px-4 py-2.5 sm:px-6",
      )}
      style={{
        borderColor: `oklch(from ${tone} l c h / 0.28)`,
        backgroundColor: `oklch(from ${tone} l c h / 0.08)`,
      }}
    >
      <div className="flex min-w-0 items-center gap-2.5">
        <span
          aria-hidden
          className="grid size-7 shrink-0 place-items-center rounded-lg"
          style={{
            backgroundColor: `oklch(from ${tone} l c h / 0.14)`,
            color: tone,
          }}
        >
          <Icon className="h-3.5 w-3.5" />
        </span>
        <p
          className="min-w-0 text-[12.5px] font-medium leading-snug"
          style={{ color: tone }}
        >
          {message}
        </p>
      </div>

      {!isExpired && (
        <button
          type="button"
          onClick={() => setDismissed(true)}
          aria-label="Dismiss subscription notice"
          title="Dismiss"
          className="grid size-7 shrink-0 cursor-pointer place-items-center rounded-md transition-colors hover:bg-[oklch(from_var(--color-foreground)_l_c_h_/_0.06)]"
          style={{ color: tone }}
        >
          <X className="h-3.5 w-3.5" />
        </button>
      )}
    </div>
  );
}
