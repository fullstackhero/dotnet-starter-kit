import { useEffect, useMemo, useState } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertCircle,
  Globe,
  LogOut,
  MonitorSmartphone,
  RefreshCw,
  Search,
  ShieldCheck,
  Smartphone,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSessionById,
  getTenantSessions,
  type UserSessionDto,
} from "@/api/sessions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import {
  EmptyState,
  ErrorBand,
  PageHero,
  Pagination,
  Stat,
  StatStrip,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDateMono,
  formatRelative,
  pad2,
} from "@/lib/list-helpers";

const PAGE_SIZE = 50;

// ───────────────────────────────────────────────────────────────────────
//  Page — admin / tenant-wide sessions console
// ───────────────────────────────────────────────────────────────────────

export function SessionsPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [includeInactive, setIncludeInactive] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);

  useEffect(() => {
    const id = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(id);
  }, [search]);

  useEffect(() => setPageNumber(1), [debouncedSearch, includeInactive]);

  const query = useQuery({
    queryKey: [
      "identity",
      "sessions",
      "tenant",
      { search: debouncedSearch, includeInactive, pageNumber },
    ],
    queryFn: () =>
      getTenantSessions({
        search: debouncedSearch || undefined,
        includeInactive,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000, // light auto-refresh — sessions move fast
  });

  const items = query.data?.items ?? [];

  const stats = useMemo(() => {
    const active = items.filter((s) => s.isActive).length;
    const mobile = items.filter((s) =>
      (s.deviceType ?? "").toLowerCase().includes("mobile"),
    ).length;
    const distinctUsers = new Set(
      items.filter((s) => s.userId).map((s) => s.userId!),
    ).size;
    return { active, mobile, distinctUsers };
  }, [items]);

  const revokeOne = useMutation({
    mutationFn: (s: UserSessionDto) =>
      adminRevokeUserSessionById(s.userId ?? "", s.id),
    onSuccess: () => {
      toast.success("Session revoked");
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions"] });
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.message
          : "Could not revoke session.",
      );
    },
  });

  const revokeAllForUser = useMutation({
    mutationFn: (userId: string) => adminRevokeAllUserSessions(userId),
    onSuccess: (data) => {
      toast.success(
        `Revoked ${data.revokedCount} ${data.revokedCount === 1 ? "session" : "sessions"}`,
      );
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions"] });
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.message
          : "Could not revoke sessions.",
      );
    },
  });

  return (
    <div className="space-y-6 pb-12">
      <PageHero
        eyebrow="System · Sessions"
        tenant={user?.tenant ?? "—"}
        title="Active sessions"
        subtitle="Every browser and device currently signed in to this tenant. Refreshes every 30 seconds. Revoke individual sessions, or sign a user out of all their devices at once."
        actions={
          <Button
            variant="outline"
            size="sm"
            disabled={query.isFetching}
            onClick={() => void query.refetch()}
            className="gap-1.5"
          >
            <RefreshCw className={cn("h-3.5 w-3.5", query.isFetching && "animate-spin")} />
            Refresh
          </Button>
        }
      />

      <StatStrip cols={4}>
        <Stat
          label="Total signed in"
          value={query.isLoading ? "—" : (query.data?.totalCount ?? 0).toString()}
          hint={includeInactive ? "Including expired/revoked" : "Live sessions only"}
          accent
        />
        <Stat
          label="Active on page"
          value={query.isLoading ? "—" : stats.active.toString()}
          hint="Not revoked, not expired"
        />
        <Stat
          label="Distinct users"
          value={query.isLoading ? "—" : stats.distinctUsers.toString()}
          hint="On this folio"
        />
        <Stat
          label="Mobile sessions"
          value={query.isLoading ? "—" : stats.mobile.toString()}
          hint="Phones / tablets on this folio"
        />
      </StatStrip>

      {/* Filter bar — search + include-inactive switch */}
      <div className="fsh-enter fsh-enter-2 flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[260px] max-w-md">
          <Search
            aria-hidden
            className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
          />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Find by user, email, or IP…"
            className="pl-9"
          />
          {search && (
            <button
              type="button"
              aria-label="Clear search"
              onClick={() => setSearch("")}
              className={cn(
                "absolute right-2 top-1/2 grid h-6 w-6 -translate-y-1/2 cursor-pointer place-items-center rounded",
                "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]",
              )}
            >
              <X className="h-3.5 w-3.5" />
            </button>
          )}
        </div>

        <label className="inline-flex cursor-pointer items-center gap-2.5 rounded-full bg-[var(--color-surface-3)] px-3 py-1.5 ring-1 ring-inset ring-[var(--color-border)]">
          <Switch
            checked={includeInactive}
            onCheckedChange={setIncludeInactive}
            aria-label="Include inactive"
          />
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
            Show inactive
          </span>
        </label>
      </div>

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section
        aria-label="Sessions"
        className={cn(
          "fsh-enter fsh-enter-3 card-shell overflow-hidden rounded-2xl",
          "bg-[var(--color-surface-3)]",
        )}
      >
        {query.isLoading && items.length === 0 ? (
          <ul aria-busy>
            {Array.from({ length: 5 }).map((_, i) => (
              <SessionSkeletonRow key={i} delayMs={i * 40} />
            ))}
          </ul>
        ) : items.length === 0 ? (
          <EmptyState
            eyebrow={debouncedSearch ? "No matches" : "Nothing live"}
            headline={
              debouncedSearch
                ? `Nothing matches "${debouncedSearch}".`
                : "No active sessions for this tenant right now."
            }
            body="Sessions appear when users sign in. Toggle Show inactive to see expired and revoked sessions."
            icon={<ShieldCheck className="h-6 w-6 text-[var(--color-primary)]" />}
            primaryAction={{
              label: "Refresh",
              onClick: () => void query.refetch(),
              icon: <RefreshCw className="h-3.5 w-3.5" />,
            }}
            secondaryAction={
              debouncedSearch
                ? {
                    label: "Clear search",
                    onClick: () => setSearch(""),
                    icon: <X className="h-3.5 w-3.5" />,
                  }
                : undefined
            }
          />
        ) : (
          <ul role="list">
            {items.map((session, i) => (
              <SessionRow
                key={session.id}
                session={session}
                delayMs={Math.min(i, 8) * 25}
                onRevoke={() => revokeOne.mutate(session)}
                isRevoking={
                  revokeOne.isPending && revokeOne.variables?.id === session.id
                }
                onRevokeAllForUser={() =>
                  session.userId && revokeAllForUser.mutate(session.userId)
                }
                isRevokingAllForUser={
                  revokeAllForUser.isPending &&
                  revokeAllForUser.variables === session.userId
                }
              />
            ))}
          </ul>
        )}
      </section>

      {query.data && query.data.totalCount > 0 && (
        <Pagination
          page={query.data.pageNumber}
          totalPages={Math.max(query.data.totalPages, 1)}
          totalCount={query.data.totalCount}
          shown={items.length}
          fetching={query.isFetching}
          hasPrev={query.data.hasPrevious}
          hasNext={query.data.hasNext}
          onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
          onNext={() => setPageNumber((p) => p + 1)}
        />
      )}

      {/* Footer-line note for context */}
      <div className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]/80">
        page {pad2(pageNumber)}
        {query.data && (
          <>
            {" "}· {query.data.totalCount} total
            {query.data.totalCount > items.length && ` · showing ${items.length}`}
          </>
        )}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row
// ───────────────────────────────────────────────────────────────────────

function SessionRow({
  session,
  delayMs,
  onRevoke,
  isRevoking,
  onRevokeAllForUser,
  isRevokingAllForUser,
}: {
  session: UserSessionDto;
  delayMs: number;
  onRevoke: () => void;
  isRevoking: boolean;
  onRevokeAllForUser: () => void;
  isRevokingAllForUser: boolean;
}) {
  const isMobile = (session.deviceType ?? "").toLowerCase().includes("mobile");
  const Icon = isMobile ? Smartphone : MonitorSmartphone;
  const tone = !session.isActive
    ? "muted"
    : session.isCurrentSession
      ? "current"
      : "active";

  const browser =
    [session.browser, session.browserVersion].filter(Boolean).join(" ") ||
    "Unknown browser";
  const os = [session.operatingSystem, session.osVersion]
    .filter(Boolean)
    .join(" ");

  return (
    <li
      className={cn(
        "fsh-enter group/row flex items-center gap-4 border-t border-[var(--color-border)]",
        "first:border-t-0 px-6 py-4",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-surface-4)]",
        !session.isActive && "opacity-70",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      {/* Device icon plate */}
      <span
        aria-hidden
        className={cn(
          "grid h-10 w-10 shrink-0 place-items-center rounded-full ring-1 ring-inset",
          tone === "current" &&
            "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]",
          tone === "active" &&
            "bg-[var(--color-muted)] text-[var(--color-foreground)] ring-[var(--color-border)]",
          tone === "muted" &&
            "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-[var(--color-border)]",
        )}
      >
        <Icon className="h-4 w-4" />
      </span>

      {/* Identity column */}
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
          <span className="text-display truncate text-[14.5px] font-medium leading-tight text-[var(--color-foreground)]">
            {session.userName ?? session.userEmail ?? "Unknown user"}
          </span>
          {session.userEmail && session.userName && (
            <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]">
              {session.userEmail}
            </code>
          )}
          {session.isCurrentSession && <Badge variant="brand">this device</Badge>}
          {!session.isActive && <Badge variant="outline">revoked / expired</Badge>}
        </div>
        <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
          <span className="inline-flex items-center gap-1">
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
              client
            </span>
            <span className="truncate">
              {browser}
              {os && <span className="opacity-70"> · {os}</span>}
            </span>
          </span>
          {session.ipAddress && (
            <span className="inline-flex items-center gap-1">
              <Globe className="h-3 w-3" />
              <code className="font-mono">{session.ipAddress}</code>
            </span>
          )}
          <span className="inline-flex items-center gap-1">
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
              last
            </span>
            <span className="tabular-nums">
              {formatRelative(session.lastActivityAt)}
            </span>
          </span>
          <span className="hidden items-center gap-1 sm:inline-flex">
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
              expires
            </span>
            <span className="tabular-nums">
              {formatDateMono(session.expiresAt)}
            </span>
          </span>
        </div>
      </div>

      {/* Action cluster — only on active sessions, never on the
          calling-admin's current session (we'd self-revoke). */}
      {session.isActive && !session.isCurrentSession ? (
        <div className="flex shrink-0 items-center gap-1.5">
          {session.userId && (
            <Button
              variant="ghost"
              size="sm"
              disabled={isRevokingAllForUser}
              onClick={onRevokeAllForUser}
              className="gap-1.5 text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
              title="Sign this user out of all devices"
            >
              <ShieldCheck className="h-3.5 w-3.5" />
              <span className="hidden md:inline">All devices</span>
            </Button>
          )}
          <Button
            variant="outline"
            size="sm"
            disabled={isRevoking}
            onClick={onRevoke}
            className="gap-1.5"
          >
            <LogOut className="h-3.5 w-3.5" />
            {isRevoking ? "Revoking…" : "Revoke"}
          </Button>
        </div>
      ) : session.isCurrentSession ? (
        <span
          title="Use Profile · Security to manage your own current session"
          className={cn(
            "shrink-0 inline-flex items-center gap-1.5 rounded-full px-2.5 py-1",
            "font-mono text-[10px] font-medium uppercase tracking-[0.14em]",
            "bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
          )}
        >
          <ShieldCheck className="h-3 w-3" />
          You
        </span>
      ) : (
        <span className="shrink-0 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
          —
        </span>
      )}
    </li>
  );
}

function SessionSkeletonRow({ delayMs }: { delayMs: number }) {
  return (
    <li
      className="fsh-enter flex items-center gap-4 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <Skeleton className="h-10 w-10 rounded-full" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-1/2" />
        <Skeleton className="h-3 w-2/3" />
      </div>
      <Skeleton className="h-7 w-20 rounded-md" />
    </li>
  );
}

// AlertCircle is reserved for a future "stale session" callout — keep import
// alive while that lands.
void AlertCircle;
