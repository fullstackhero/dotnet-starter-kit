import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Plus, Users } from "lucide-react";
import { searchUsers, type UserDto } from "@/api/users";
import { listRoles } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { Monogram } from "@/components/monogram";
import { EntityPageHeader, ErrorBand } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { CreateUserDialog } from "@/components/users/create-user-dialog";

const PAGE_SIZE = 12;

type Tri = "any" | "yes" | "no";

function triToBool(v: Tri): boolean | undefined {
  if (v === "yes") return true;
  if (v === "no") return false;
  return undefined;
}

// Desktop grid template — shared by header + rows.
const DESKTOP_COLS =
  "grid-cols-[1fr_140px_24px] lg:grid-cols-[1.6fr_140px_180px_24px]";

export function UsersListPage() {
  const navigate = useNavigate();

  const [pageNumber, setPageNumber] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [activeFilter, setActiveFilter] = useState<Tri>("any");
  const [confirmedFilter, setConfirmedFilter] = useState<Tri>("any");
  const [roleId, setRoleId] = useState<string>("");
  const [createOpen, setCreateOpen] = useState(false);

  // Debounce the search input → searchTerm
  useEffect(() => {
    const t = setTimeout(() => {
      setSearchTerm(searchInput);
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [searchInput]);

  // Reset page when filters change
  useEffect(() => {
    setPageNumber(1);
  }, [activeFilter, confirmedFilter, roleId]);

  const rolesQuery = useQuery({
    queryKey: ["roles"],
    queryFn: listRoles,
    staleTime: 5 * 60_000,
  });

  const usersQuery = useQuery({
    queryKey: [
      "users",
      { pageNumber, pageSize: PAGE_SIZE, searchTerm, activeFilter, confirmedFilter, roleId },
    ],
    queryFn: () =>
      searchUsers({
        pageNumber,
        pageSize: PAGE_SIZE,
        search: searchTerm || undefined,
        isActive: triToBool(activeFilter),
        emailConfirmed: triToBool(confirmedFilter),
        roleId: roleId || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const data = usersQuery.data;
  const items: UserDto[] = data?.items ?? [];
  const baseIndex = ((data?.pageNumber ?? 1) - 1) * (data?.pageSize ?? PAGE_SIZE);

  const pageBadge = useMemo(() => {
    if (!data) return "—";
    const p = String(data.pageNumber).padStart(2, "0");
    const t = String(Math.max(data.totalPages, 1)).padStart(2, "0");
    return `Page ${p} of ${t}`;
  }, [data]);

  const filtersActive =
    activeFilter !== "any" || confirmedFilter !== "any" || roleId !== "";
  const searchActive = searchTerm.length > 0 || filtersActive;

  const clearFilters = () => {
    setSearchInput("");
    setActiveFilter("any");
    setConfirmedFilter("any");
    setRoleId("");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Users}
        title="Directory"
        total={data?.totalCount ?? null}
        unit="account"
        description={data
          ? `${data.totalCount} ${data.totalCount === 1 ? "account" : "accounts"} on this tenant.`
          : "Loading the roster…"}
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" /> New user
        </Button>
      </EntityPageHeader>

      {/* Filter row */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-full max-w-sm">
          <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-muted-foreground)]">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
          </span>
          <input
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search name, username, email…"
            aria-label="Search users"
            className="h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-3 font-mono text-[12.5px] outline-none transition-colors placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)] focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
          />
        </div>

        <Segmented
          label="Status"
          value={activeFilter}
          onChange={setActiveFilter}
          options={[
            { value: "any", label: "Any" },
            { value: "yes", label: "Active" },
            { value: "no", label: "Disabled" },
          ]}
        />
        <Segmented
          label="Email"
          value={confirmedFilter}
          onChange={setConfirmedFilter}
          options={[
            { value: "any", label: "Any" },
            { value: "yes", label: "Confirmed" },
            { value: "no", label: "Pending" },
          ]}
        />

        <Select
          label="Role"
          value={roleId}
          onChange={(v) => setRoleId(v)}
          options={(rolesQuery.data ?? []).map((r) => ({ value: r.id ?? "", label: r.name ?? r.id ?? "" }))}
          placeholder="Any role"
          minWidth="9rem"
        />
      </div>

      {usersQuery.isError && (
        <ErrorBand
          message={
            usersQuery.error instanceof ApiRequestError
              ? usersQuery.error.problem?.detail ?? usersQuery.error.message
              : "Failed to load users."
          }
        />
      )}

      {usersQuery.isLoading && items.length === 0 && (
        <div
          role="status"
          className="py-12 text-center font-mono text-sm uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]"
        >
          Loading…
        </div>
      )}

      {!usersQuery.isLoading && items.length === 0 && !usersQuery.isError && (
        <div className="py-16 text-center">
          <p className="font-display text-2xl text-[var(--color-foreground)]">No matches.</p>
          <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
            {searchActive
              ? "Adjust filters or invite a new user."
              : "Register the first member to seed this tenant."}
          </p>
          {searchActive && (
            <Button
              variant="outline"
              className="mt-4 h-9 rounded-lg px-4 text-[13px]"
              onClick={clearFilters}
            >
              Clear filters
            </Button>
          )}
        </div>
      )}

      {items.length > 0 && (
        <div>
          <p className="mb-3 text-[12px] font-medium text-[var(--color-muted-foreground)]">
            {data?.totalCount ?? 0} user{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
          </p>

          {/* Mobile card list */}
          <div className="space-y-2 md:hidden">
            {items.map((user, i) => (
              <UserMobileCard
                key={user.id ?? i}
                user={user}
                index={baseIndex + i + 1}
                onClick={() => user.id && navigate(`/users/${user.id}`)}
              />
            ))}
          </div>

          {/* Desktop table */}
          <div className="hidden overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs md:block">
            {/* Table header */}
            <div
              className={`grid items-center gap-3 border-b border-[var(--color-border)] bg-[var(--color-muted)]/40 px-4 py-2.5 ${DESKTOP_COLS}`}
            >
              <span className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Name
              </span>
              <span className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Username
              </span>
              <span className="hidden text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] lg:block">
                Status
              </span>
              <span />
            </div>

            <ol className="divide-y divide-[var(--color-border)]">
              {items.map((user, i) => (
                <UserDesktopRow
                  key={user.id ?? i}
                  user={user}
                  isLast={i === items.length - 1}
                  onClick={() => user.id && navigate(`/users/${user.id}`)}
                />
              ))}
            </ol>
          </div>
        </div>
      )}

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-xs">
          <span className="font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            {pageBadge}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPrevious || usersQuery.isFetching}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              className="h-9 rounded-lg px-3 text-[13px]"
            >
              <ChevronLeft className="mr-1 h-3.5 w-3.5" /> Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNext || usersQuery.isFetching}
              onClick={() => setPageNumber((p) => p + 1)}
              className="h-9 rounded-lg px-3 text-[13px]"
            >
              Next <ChevronRight className="ml-1 h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      )}

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}

// ─── Mobile card ────────────────────────────────────────────────────────

function UserMobileCard({
  user,
  index,
  onClick,
}: {
  user: UserDto;
  index: number;
  onClick: () => void;
}) {
  const fullName = [user.firstName, user.lastName].filter(Boolean).join(" ").trim();
  const display = fullName || user.userName || user.email || "Unnamed";

  return (
    <li className="list-none">
      <button
        type="button"
        onClick={onClick}
        aria-label={`Open user ${display}`}
        className={cn(
          "group w-full overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left shadow-xs",
          "transition-colors hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          !user.isActive && "opacity-75",
        )}
      >
        <div className="flex items-center justify-between">
          <div className="flex min-w-0 items-center gap-3">
            <Monogram
              seed={user.id ?? user.userName ?? String(index)}
              firstName={user.firstName}
              lastName={user.lastName}
              fallback={user.userName ?? user.email}
              size="md"
            />
            <div className="min-w-0">
              <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                {display}
              </p>
              <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
                {user.email ?? "no email"}
              </p>
            </div>
          </div>
          <ChevronRight className="size-4 shrink-0 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
        </div>
        <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-1.5">
          <span
            className={cn(
              "inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10.5px] font-medium",
              user.isActive
                ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.12)] text-[var(--color-success)]"
                : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
            )}
          >
            {user.isActive ? "Active" : "Inactive"}
          </span>
          <span
            className={cn(
              "inline-flex items-center gap-1 rounded-full px-1.5 py-0.5 text-[10.5px] font-medium",
              user.emailConfirmed
                ? "bg-[oklch(from_var(--color-info)_l_c_h_/_0.12)] text-[var(--color-info)]"
                : "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.12)] text-[var(--color-warning)]",
            )}
          >
            {user.emailConfirmed ? "Email confirmed" : "Email pending"}
          </span>
        </div>
      </button>
    </li>
  );
}

// ─── Desktop row ────────────────────────────────────────────────────────

function UserDesktopRow({
  user,
  onClick,
}: {
  user: UserDto;
  isLast?: boolean;
  onClick: () => void;
}) {
  const fullName = [user.firstName, user.lastName].filter(Boolean).join(" ").trim();
  const display = fullName || user.userName || user.email || "Unnamed";

  return (
    <li className="list-none">
      <button
        type="button"
        onClick={onClick}
        className={cn(
          `group grid w-full items-center gap-3 px-4 py-3.5 text-left transition-colors hover:bg-[var(--color-accent)] focus-visible:bg-[var(--color-accent)] focus-visible:outline-none ${DESKTOP_COLS}`,
          !user.isActive && "opacity-75",
        )}
      >
        {/* Name + email */}
        <div className="flex min-w-0 items-center gap-3">
          <Monogram
            seed={user.id ?? user.userName ?? "user"}
            firstName={user.firstName}
            lastName={user.lastName}
            fallback={user.userName ?? user.email}
            size="md"
          />
          <div className="min-w-0">
            <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
              {display}
            </span>
            <span
              className={cn(
                "block truncate text-[12px] text-[var(--color-muted-foreground)]",
                !user.email && "italic opacity-60",
              )}
            >
              {user.email ?? "no email on file"}
            </span>
          </div>
        </div>

        {/* Username */}
        <code
          title={user.userName ?? undefined}
          className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
        >
          {user.userName ? `@${user.userName}` : "—"}
        </code>

        {/* Status (lg+) */}
        <div className="hidden items-center gap-1.5 lg:flex">
          <span
            className={cn(
              "inline-flex items-center rounded-full px-1.5 py-0.5 text-[10.5px] font-medium",
              user.isActive
                ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.12)] text-[var(--color-success)]"
                : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
            )}
          >
            {user.isActive ? "Active" : "Inactive"}
          </span>
          <span
            className={cn(
              "inline-flex items-center rounded-full px-1.5 py-0.5 text-[10.5px] font-medium",
              user.emailConfirmed
                ? "bg-[oklch(from_var(--color-info)_l_c_h_/_0.12)] text-[var(--color-info)]"
                : "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.12)] text-[var(--color-warning)]",
            )}
          >
            {user.emailConfirmed ? "Confirmed" : "Pending"}
          </span>
        </div>

        <div className="flex items-center justify-end">
          <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
        </div>
      </button>
    </li>
  );
}

// ─── Segmented filter control ────────────────────────────────────────────

type SegmentedProps<T extends string> = {
  label: string;
  value: T;
  onChange: (next: T) => void;
  options: ReadonlyArray<{ value: T; label: string }>;
};

function Segmented<T extends string>({ label, value, onChange, options }: SegmentedProps<T>) {
  return (
    <div className="flex items-center gap-2 text-[0.6875rem] font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
      <span id={`seg-${label}`}>{label}</span>
      <div role="group" aria-labelledby={`seg-${label}`} className="flex overflow-hidden rounded-md border border-[var(--color-border)]">
        {options.map((o) => {
          const selected = o.value === value;
          return (
            <button
              key={o.value}
              type="button"
              onClick={() => onChange(o.value)}
              aria-pressed={selected}
              className={cn(
                "px-2.5 py-1 text-[0.6875rem] tracking-[0.18em] transition-colors",
                selected
                  ? "bg-[var(--color-foreground)] text-[var(--color-background)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]",
              )}
            >
              {o.label}
            </button>
          );
        })}
      </div>
    </div>
  );
}
