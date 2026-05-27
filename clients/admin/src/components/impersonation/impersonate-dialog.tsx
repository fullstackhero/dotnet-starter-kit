import { useEffect, useState } from "react";
import { useMutation, useQuery, keepPreviousData } from "@tanstack/react-query";
import { ArrowLeft, ArrowRight, Check, Search, ShieldAlert, UserCog } from "lucide-react";
import { toast } from "sonner";
import { searchUsers, type UserDto } from "@/api/users";
import { startImpersonation, type ImpersonationResponse } from "@/api/impersonation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Monogram } from "@/components/monogram";
import { ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import { cn } from "@/lib/cn";

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tenantId: string;
  tenantName?: string;
  /** Pre-select a user — skips the picker and jumps straight to the form. */
  prefillUser?: UserDto;
};

type DurationOption = { minutes: number; label: string };
const DURATION_OPTIONS: DurationOption[] = [
  { minutes: 10, label: "10 min" },
  { minutes: 15, label: "15 min" },
  { minutes: 30, label: "30 min" },
];

/**
 * ImpersonateDialog — two-step modal flow:
 *   1. Pick a user inside the target tenant (skipped if `prefillUser` is set)
 *   2. Enter reason + pick session duration → start
 *
 * On success, opens the dashboard origin in a NEW TAB with the impersonation
 * token in the URL hash. The dashboard's bootstrap reads the hash, installs
 * the token, and strips it from the URL before any render. Hash params are
 * never sent to the server and don't leak via referrer/HTTP logs.
 */
export function ImpersonateDialog({
  open,
  onOpenChange,
  tenantId,
  tenantName,
  prefillUser,
}: Props) {
  const [step, setStep] = useState<"pick" | "configure">(prefillUser ? "configure" : "pick");
  const [selected, setSelected] = useState<UserDto | null>(prefillUser ?? null);

  // Reset on close + when prefill changes so reopening is idempotent.
  useEffect(() => {
    if (open) {
      setStep(prefillUser ? "configure" : "pick");
      setSelected(prefillUser ?? null);
    }
  }, [open, prefillUser]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent size="lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-7 w-7 place-items-center rounded-md bg-[var(--color-accent-signal)]/15 text-[var(--color-accent-signal)]"
            >
              <UserCog className="h-4 w-4" />
            </span>
            <DialogTitle>Impersonate user</DialogTitle>
          </div>
          <DialogDescription>
            Tenant{" "}
            <code className="code-chip">{tenantName ?? tenantId}</code> ·{" "}
            {step === "pick"
              ? "pick a user to impersonate."
              : "session details. Token will be issued and opened in the dashboard."}
          </DialogDescription>
        </DialogHeader>

        {step === "pick" ? (
          <PickStep
            tenantId={tenantId}
            onPick={(user) => {
              setSelected(user);
              setStep("configure");
            }}
            onCancel={() => onOpenChange(false)}
          />
        ) : (
          selected && (
            <ConfigureStep
              tenantId={tenantId}
              tenantName={tenantName}
              user={selected}
              onBack={
                prefillUser
                  ? undefined
                  : () => {
                      setSelected(null);
                      setStep("pick");
                    }
              }
              onDone={() => onOpenChange(false)}
            />
          )
        )}
      </DialogContent>
    </Dialog>
  );
}

// ─── Step 1: pick user ──────────────────────────────────────────────────

function PickStep({
  tenantId,
  onPick,
  onCancel,
}: {
  tenantId: string;
  onPick: (user: UserDto) => void;
  onCancel: () => void;
}) {
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const handle = setTimeout(() => setDebounced(search.trim()), 250);
    return () => clearTimeout(handle);
  }, [search]);

  const query = useQuery({
    queryKey: ["impersonation", "users", tenantId, debounced],
    queryFn: () =>
      searchUsers({
        tenantId,
        search: debounced || undefined,
        pageSize: 25,
        // Skip disabled accounts — impersonating a deactivated user is a footgun
        // (the impersonation token would be valid, but the user's normal sign-in
        // is disabled — confusing to debug).
        isActive: true,
      }),
    placeholderData: keepPreviousData,
  });

  const users = query.data?.items ?? [];

  return (
    <>
      <DialogBody className="space-y-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
          <Input
            autoFocus
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name, email, or username…"
            aria-label="Search users to impersonate"
            className="pl-9"
          />
        </div>

        <div className="-mx-2 max-h-[22rem] overflow-y-auto">
          {query.isError && (
            <div className="px-3 py-6 text-sm text-[var(--color-destructive)]">
              {query.error instanceof ApiRequestError
                ? query.error.problem?.detail ?? query.error.message
                : "Failed to load users."}
            </div>
          )}

          {query.isLoading && (
            <div className="px-3 py-10 text-center font-mono text-xs uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Loading
              <span className="caret text-[var(--color-accent-signal)]" aria-hidden />
            </div>
          )}

          {!query.isLoading && users.length === 0 && (
            <div className="px-3 py-10 text-center text-sm text-[var(--color-muted-foreground)]">
              No users match{debounced ? ` “${debounced}”` : ""}.
            </div>
          )}

          <ul className="divide-y divide-[var(--color-border)]">
            {users.map((user) => (
              <li key={user.id}>
                <button
                  type="button"
                  onClick={() => onPick(user)}
                  className="group flex w-full items-center gap-3 px-3 py-2.5 text-left transition-colors hover:bg-[var(--color-muted)]/60 focus:outline-none focus-visible:bg-[var(--color-muted)]/60"
                >
                  <Monogram
                    seed={user.id ?? user.userName ?? "x"}
                    firstName={user.firstName ?? undefined}
                    lastName={user.lastName ?? undefined}
                    fallback={user.userName ?? user.email ?? "?"}
                    size="sm"
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
                      <span className="truncate text-sm font-medium">
                        {[user.firstName, user.lastName].filter(Boolean).join(" ") ||
                          user.userName ||
                          user.email ||
                          "Unnamed"}
                      </span>
                      {user.userName && (
                        <span className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                          @{user.userName}
                        </span>
                      )}
                    </div>
                    <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                      {user.email ?? "—"}
                    </div>
                  </div>
                  <ArrowRight className="h-3.5 w-3.5 text-[var(--color-muted-foreground)] transition-transform group-hover:translate-x-0.5" />
                </button>
              </li>
            ))}
          </ul>
        </div>
      </DialogBody>

      <DialogFooter>
        <Button variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </DialogFooter>
    </>
  );
}

// ─── Step 2: configure ──────────────────────────────────────────────────

function ConfigureStep({
  tenantId,
  tenantName,
  user,
  onBack,
  onDone,
}: {
  tenantId: string;
  tenantName?: string;
  user: UserDto;
  onBack?: () => void;
  onDone: () => void;
}) {
  const [reason, setReason] = useState("");
  const [minutes, setMinutes] = useState<number>(15);

  const trimmedReason = reason.trim();
  const reasonValid = trimmedReason.length >= 4;

  const mutation = useMutation<ImpersonationResponse, Error, void>({
    mutationFn: () =>
      startImpersonation({
        targetUserId: user.id,
        targetTenantId: tenantId,
        reason: trimmedReason,
        durationMinutes: minutes,
      }),
    onSuccess: (response) => {
      handoffToDashboard(response, tenantId);
      toast.success(`Impersonation started · ${minutes} min`, {
        description: `Opened the dashboard as ${labelFor(user)}. End impersonation from inside the dashboard tab.`,
      });
      onDone();
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err.message;
      toast.error("Impersonation failed", { description: detail });
    },
  });

  return (
    <>
      <DialogBody className="space-y-5">
        <SelectedUserCard user={user} tenantId={tenantId} tenantName={tenantName} />

        <fieldset className="space-y-2">
          <legend className="meta text-[var(--color-muted-foreground)]">// Duration</legend>
          <div className="grid grid-cols-3 gap-2">
            {DURATION_OPTIONS.map((opt) => {
              const active = minutes === opt.minutes;
              return (
                <button
                  key={opt.minutes}
                  type="button"
                  onClick={() => setMinutes(opt.minutes)}
                  aria-pressed={active}
                  className={cn(
                    "flex flex-col items-center gap-0.5 rounded-md border px-3 py-2 transition-colors",
                    active
                      ? "border-[var(--color-accent-signal)] bg-[var(--color-accent-signal)]/10 text-[var(--color-foreground)]"
                      : "border-[var(--color-border)] hover:bg-[var(--color-muted)]/60",
                  )}
                >
                  <span className="font-display text-lg font-semibold tabular-nums">
                    {opt.minutes}
                  </span>
                  <span className="meta text-[var(--color-muted-foreground)]">minutes</span>
                </button>
              );
            })}
          </div>
        </fieldset>

        <div className="space-y-1.5">
          <label
            htmlFor="impersonation-reason"
            className="meta flex items-center gap-1.5 text-[var(--color-muted-foreground)]"
          >
            Reason
            <span className="text-[var(--color-destructive)]" aria-hidden>
              ·
            </span>
          </label>
          <textarea
            id="impersonation-reason"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="e.g. Customer ticket #4821 — verifying ledger discrepancy"
            rows={3}
            maxLength={500}
            className={cn(
              "w-full resize-y rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm",
              "transition-[border-color,background-color,box-shadow] duration-[var(--duration-fast)]",
              "hover:border-[var(--color-border-strong)]",
              "focus-visible:outline-none focus-visible:border-[var(--color-accent-signal)] focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.25)] focus-visible:bg-[var(--color-surface-2)]",
            )}
          />
          <p className="flex items-center justify-between text-[11px] text-[var(--color-muted-foreground)]">
            <span>
              Recorded in the security audit trail — be specific enough that a reviewer can
              reconstruct the case later.
            </span>
            <span
              className={cn(
                "font-mono tabular-nums",
                trimmedReason.length > 0 && !reasonValid && "text-[var(--color-warning)]",
              )}
            >
              {trimmedReason.length}/500
            </span>
          </p>
        </div>

        <div className="flex items-start gap-2 rounded-md border border-[var(--color-warning)]/40 bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] px-3 py-2.5 text-xs text-[var(--color-foreground)]">
          <ShieldAlert className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-warning)]" />
          <div>
            <strong className="font-medium">Everything you do is attributed to your account.</strong>
            {" "}The session token carries actor claims; the audit trail will show
            both the user being impersonated and you as the actor. End from the dashboard
            tab when done.
          </div>
        </div>
      </DialogBody>

      <DialogFooter>
        {onBack && (
          <Button variant="outline" onClick={onBack} disabled={mutation.isPending} className="sm:mr-auto">
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Choose another user
          </Button>
        )}
        <Button
          variant="signal"
          disabled={!reasonValid || mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? (
            "Issuing token…"
          ) : (
            <>
              <Check className="mr-1 h-3.5 w-3.5" /> Start {minutes}-min impersonation
            </>
          )}
        </Button>
      </DialogFooter>
    </>
  );
}

function SelectedUserCard({
  user,
  tenantId,
  tenantName,
}: {
  user: UserDto;
  tenantId: string;
  tenantName?: string;
}) {
  return (
    <div className="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-3">
      <Monogram
        seed={user.id ?? user.userName ?? "x"}
        firstName={user.firstName ?? undefined}
        lastName={user.lastName ?? undefined}
        fallback={user.userName ?? user.email ?? "?"}
        size="md"
      />
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="truncate text-sm font-medium">{labelFor(user)}</span>
          {user.userName && (
            <span className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              @{user.userName}
            </span>
          )}
        </div>
        <div className="mt-0.5 flex flex-wrap items-baseline gap-2 font-mono text-[11px] text-[var(--color-muted-foreground)]">
          <span className="truncate">{user.email ?? "—"}</span>
          <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
            {tenantName ?? tenantId}
          </Badge>
        </div>
      </div>
    </div>
  );
}

// ─── Handoff helper ─────────────────────────────────────────────────────

/**
 * Open the dashboard in a new tab with the impersonation token in the URL
 * hash. Hash params don't get sent to servers and aren't logged in HTTP
 * referrers, so this is safer than a query-string handoff. The dashboard's
 * bootstrap reads the hash, calls tokenStore.beginImpersonation, then
 * history.replaceState() to strip the hash before any render.
 *
 * `expiresAt` is included so the dashboard can show a countdown.
 */
function handoffToDashboard(response: ImpersonationResponse, tenantId: string) {
  const params = new URLSearchParams();
  params.set("token", response.accessToken);
  params.set("tenant", tenantId);
  params.set("expiresAt", response.accessTokenExpiresAt);
  const url = `${env.dashboardUrl}/#impersonate?${params.toString()}`;
  // noopener+noreferrer so the opened tab can't navigate this one and the
  // referrer header is suppressed entirely (defense in depth — the hash
  // wouldn't be in the referrer anyway, but the rest of this URL would be).
  window.open(url, "_blank", "noopener,noreferrer");
}

function labelFor(user: UserDto): string {
  return (
    [user.firstName, user.lastName].filter(Boolean).join(" ").trim() ||
    user.userName ||
    user.email ||
    "Unnamed user"
  );
}
