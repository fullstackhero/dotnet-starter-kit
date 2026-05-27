import { useEffect, useMemo, useState } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  Globe,
  LogOut,
  MonitorSmartphone,
  RefreshCw,
  ShieldCheck,
  Smartphone,
  UserCog,
} from "lucide-react";
import { toast } from "sonner";
import {
  adminRevokeAllUserSessions,
  adminRevokeUserSessionById,
  getTenantSessions,
  type UserSessionDto,
} from "@/api/sessions";
import { Button } from "@/components/ui/button";
import {
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { describe, formatRelative } from "@/lib/list-helpers";

const PAGE_SIZE = 50;
const DESKTOP_COLS = "grid-cols-[1.4fr_1.4fr_140px_140px_120px]";

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

  const items = useMemo(() => query.data?.items ?? [], [query.data]);

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

  const data = query.data;
  const searchActive = debouncedSearch.length > 0 || includeInactive;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={UserCog}
        title="Active sessions"
        total={data?.totalCount ?? null}
        unit="session"
        description={`Every browser and device currently signed in to ${user?.tenant ?? "this tenant"}. Refreshes every 30 seconds.`}
      >
        <Button
          variant="outline"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <RefreshCw className={cn("size-4", query.isFetching && "animate-spin")} />
          Refresh
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Find by user, email, or IP…"
      />

      {/* Filter row */}
      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<boolean>
          label="Visibility"
          value={includeInactive}
          onChange={setIncludeInactive}
          options={[
            { value: false, label: "Live only" },
            { value: true, label: "Include inactive" },
          ]}
        />
        {items.length > 0 && (
          <div className="ml-auto hidden items-center gap-3 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] sm:flex">
            <span>{stats.active} active</span>
            <span aria-hidden className="opacity-50">·</span>
            <span>{stats.distinctUsers} users</span>
            <span aria-hidden className="opacity-50">·</span>
            <span>{stats.mobile} mobile</span>
          </div>
        )}
      </div>

      {/* Results */}
      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={ShieldCheck}
          title={searchActive ? "No sessions found" : "No active sessions"}
          body={
            debouncedSearch
              ? `Nothing matches "${debouncedSearch}". Try a different term, or toggle Include inactive to see expired sessions.`
              : "Sessions appear when users sign in. Toggle Include inactive to see expired and revoked sessions."
          }
          action={
            <div className="flex items-center gap-2">
              {debouncedSearch && (
                <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                  Clear search
                </Button>
              )}
              <Button onClick={() => void query.refetch()} className="h-9 rounded-lg px-4 text-[13px]">
                <RefreshCw className="mr-1.5 size-4" />
                Refresh
              </Button>
            </div>
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} session{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          {/* Mobile cards */}
          <div className="space-y-2 md:hidden">
            {items.map((session) => (
              <SessionMobileCard
                key={session.id}
                session={session}
                onRevoke={() => revokeOne.mutate(session)}
                isRevoking={revokeOne.isPending && revokeOne.variables?.id === session.id}
                onRevokeAllForUser={() =>
                  session.userId && revokeAllForUser.mutate(session.userId)
                }
                isRevokingAllForUser={
                  revokeAllForUser.isPending &&
                  revokeAllForUser.variables === session.userId
                }
              />
            ))}
          </div>

          {/* Desktop list */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>User</span>
              <span>Device</span>
              <span>IP</span>
              <span>Last activity</span>
              <span className="text-right">Actions</span>
            </EntityListHeader>
            {items.map((session, i) => (
              <SessionDesktopRow
                key={session.id}
                session={session}
                isLast={i === items.length - 1}
                onRevoke={() => revokeOne.mutate(session)}
                isRevoking={revokeOne.isPending && revokeOne.variables?.id === session.id}
                onRevokeAllForUser={() =>
                  session.userId && revokeAllForUser.mutate(session.userId)
                }
                isRevokingAllForUser={
                  revokeAllForUser.isPending &&
                  revokeAllForUser.variables === session.userId
                }
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? pageNumber}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" />
          <span>{describe(query.error)}</span>
        </div>
      )}

    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function SessionMobileCard({
  session,
  onRevoke,
  isRevoking,
  onRevokeAllForUser,
  isRevokingAllForUser,
}: {
  session: UserSessionDto;
  onRevoke: () => void;
  isRevoking: boolean;
  onRevokeAllForUser: () => void;
  isRevokingAllForUser: boolean;
}) {
  const isMobile = (session.deviceType ?? "").toLowerCase().includes("mobile");
  const DeviceIcon = isMobile ? Smartphone : MonitorSmartphone;
  const displayName = session.userName ?? session.userEmail ?? "Unknown user";
  const browser =
    [session.browser, session.browserVersion].filter(Boolean).join(" ") || "Unknown browser";

  return (
    <div
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        !session.isActive && "opacity-75",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={displayName} size={40} />
          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {displayName}
              </p>
              {session.isCurrentSession && (
                <EntityStatusBadge tone="info">You</EntityStatusBadge>
              )}
              {!session.isActive && (
                <EntityStatusBadge tone="danger">Inactive</EntityStatusBadge>
              )}
            </div>
            {session.userEmail && session.userName && (
              <code className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                {session.userEmail}
              </code>
            )}
          </div>
        </div>
      </div>
      <div className="mt-2 ml-[52px] space-y-1 text-[11px] text-[var(--color-muted-foreground)]">
        <div className="flex items-center gap-1.5">
          <DeviceIcon className="size-3" />
          <span className="truncate">{browser}</span>
        </div>
        {session.ipAddress && (
          <div className="flex items-center gap-1.5">
            <Globe className="size-3" />
            <code className="font-mono">{session.ipAddress}</code>
          </div>
        )}
        <div className="tabular-nums">{formatRelative(session.lastActivityAt)}</div>
      </div>
      {session.isActive && !session.isCurrentSession && (
        <div className="mt-3 ml-[52px] flex items-center gap-1.5">
          {session.userId && (
            <Button
              variant="ghost"
              size="sm"
              disabled={isRevokingAllForUser}
              onClick={onRevokeAllForUser}
              className="gap-1.5"
            >
              <ShieldCheck className="size-3.5" />
              All devices
            </Button>
          )}
          <Button
            variant="outline"
            size="sm"
            disabled={isRevoking}
            onClick={onRevoke}
            className="gap-1.5"
          >
            <LogOut className="size-3.5" />
            {isRevoking ? "Revoking…" : "Revoke"}
          </Button>
        </div>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function SessionDesktopRow({
  session,
  isLast,
  onRevoke,
  isRevoking,
  onRevokeAllForUser,
  isRevokingAllForUser,
}: {
  session: UserSessionDto;
  isLast: boolean;
  onRevoke: () => void;
  isRevoking: boolean;
  onRevokeAllForUser: () => void;
  isRevokingAllForUser: boolean;
}) {
  const isMobile = (session.deviceType ?? "").toLowerCase().includes("mobile");
  const DeviceIcon = isMobile ? Smartphone : MonitorSmartphone;
  const displayName = session.userName ?? session.userEmail ?? "Unknown user";
  const browser =
    [session.browser, session.browserVersion].filter(Boolean).join(" ") || "Unknown browser";
  const os = [session.operatingSystem, session.osVersion].filter(Boolean).join(" ");

  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim={!session.isActive}>
      {/* User */}
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={displayName} size={36} />
        <div className="min-w-0">
          <div className="flex items-center gap-1.5">
            <span className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {displayName}
            </span>
            {session.isCurrentSession && (
              <EntityStatusBadge tone="info">You</EntityStatusBadge>
            )}
            {!session.isActive && (
              <EntityStatusBadge tone="danger">Inactive</EntityStatusBadge>
            )}
          </div>
          {session.userEmail && session.userName && (
            <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {session.userEmail}
            </code>
          )}
        </div>
      </div>

      {/* Device / browser */}
      <div className="flex min-w-0 items-center gap-1.5 text-[12.5px] text-[var(--color-foreground)]">
        <DeviceIcon className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
        <div className="min-w-0">
          <div className="truncate">{browser}</div>
          {os && <div className="truncate text-[11px] text-[var(--color-muted-foreground)]">{os}</div>}
        </div>
      </div>

      {/* IP */}
      <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
        {session.ipAddress ?? "—"}
      </code>

      {/* Last activity */}
      <div className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatRelative(session.lastActivityAt)}
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end gap-1.5">
        {session.isActive && !session.isCurrentSession ? (
          <>
            {session.userId && (
              <Button
                variant="ghost"
                size="sm"
                disabled={isRevokingAllForUser}
                onClick={onRevokeAllForUser}
                className="gap-1.5 text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
                title="Sign this user out of all devices"
              >
                <ShieldCheck className="size-3.5" />
                <span className="hidden lg:inline">All</span>
              </Button>
            )}
            <Button
              variant="outline"
              size="sm"
              disabled={isRevoking}
              onClick={onRevoke}
              className="gap-1.5"
            >
              <LogOut className="size-3.5" />
              {isRevoking ? "Revoking…" : "Revoke"}
            </Button>
          </>
        ) : (
          <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]/70">
            —
          </span>
        )}
      </div>
    </EntityListRow>
  );
}