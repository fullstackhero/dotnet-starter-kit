import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Plus, Search } from "lucide-react";
import { searchUsers, type UserDto } from "@/api/users";
import { listRoles } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Monogram } from "@/components/monogram";
import { SectionRule } from "@/components/section-rule";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 12;

type Tri = "any" | "yes" | "no";

function triToBool(v: Tri): boolean | undefined {
  if (v === "yes") return true;
  if (v === "no") return false;
  return undefined;
}

export function UsersListPage() {
  const navigate = useNavigate();

  const [pageNumber, setPageNumber] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [activeFilter, setActiveFilter] = useState<Tri>("any");
  const [confirmedFilter, setConfirmedFilter] = useState<Tri>("any");
  const [roleId, setRoleId] = useState<string>("");

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

  return (
    <div className="space-y-8">
      <SectionRule
        crumbs={[{ label: "\\ Users" }, { label: "Directory", muted: true }]}
        trailing={pageBadge.toUpperCase()}
      />

      <div className="flex items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-4xl font-semibold tracking-tight md:text-5xl">
            Directory
          </h1>
          <p className="mt-2 text-sm text-[var(--color-muted-foreground)]">
            {data
              ? `${data.totalCount} ${data.totalCount === 1 ? "account" : "accounts"} on this tenant.`
              : "Loading the roster…"}
          </p>
        </div>
        <Button onClick={() => navigate("/users/new")} className="shrink-0">
          <Plus className="mr-1 h-4 w-4" /> New user
        </Button>
      </div>

      {/* Filter row */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-full max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
          <Input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Search name, username, email…"
            className="pl-9 font-mono text-xs placeholder:font-mono placeholder:text-xs"
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

        <label className="flex items-center gap-2 text-[0.6875rem] font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          Role
          <select
            value={roleId}
            onChange={(e) => setRoleId(e.target.value)}
            className="h-8 rounded-sm border border-[var(--color-border)] bg-transparent px-2 text-xs font-mono normal-case tracking-normal text-[var(--color-foreground)] focus:outline-none focus:ring-2 focus:ring-[var(--color-ring)]"
          >
            <option value="">Any role</option>
            {rolesQuery.data?.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {/* Roster */}
      <div className="border-t border-[var(--color-border)]">
        {usersQuery.isError && (
          <div className="border-b border-[var(--color-border)] px-1 py-4 text-sm text-[var(--color-destructive)]">
            {usersQuery.error instanceof ApiRequestError
              ? usersQuery.error.problem?.detail ?? usersQuery.error.message
              : "Failed to load users."}
          </div>
        )}

        {usersQuery.isLoading && items.length === 0 && (
          <div className="px-1 py-12 text-center text-sm text-[var(--color-muted-foreground)] font-mono uppercase tracking-[0.18em]">
            Loading…
          </div>
        )}

        {!usersQuery.isLoading && items.length === 0 && (
          <div className="px-1 py-16 text-center">
            <p className="font-display text-2xl">No matches.</p>
            <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
              Adjust filters or invite a new user.
            </p>
          </div>
        )}

        <ol className="divide-y divide-[var(--color-border)]">
          {items.map((user, i) => (
            <UserRow
              key={user.id ?? i}
              user={user}
              index={baseIndex + i + 1}
              onClick={() => user.id && navigate(`/users/${user.id}`)}
            />
          ))}
        </ol>
      </div>

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
            >
              <ChevronLeft className="mr-1 h-3.5 w-3.5" /> Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNext || usersQuery.isFetching}
              onClick={() => setPageNumber((p) => p + 1)}
            >
              Next <ChevronRight className="ml-1 h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

function UserRow({
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
  const num = String(index).padStart(3, "0");

  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className="group grid w-full grid-cols-[3.5rem_2.5rem_1fr_auto] items-center gap-4 px-1 py-4 text-left transition-colors hover:bg-[var(--color-muted)]/50 focus:outline-none focus-visible:bg-[var(--color-muted)]/50"
      >
        <span className="font-mono text-xs text-[var(--color-muted-foreground)] tabular-nums">
          #{num}
        </span>

        <Monogram
          seed={user.id ?? user.userName ?? num}
          firstName={user.firstName}
          lastName={user.lastName}
          fallback={user.userName ?? user.email}
          size="md"
        />

        <div className="min-w-0">
          <div className="flex items-baseline gap-2">
            <span className="truncate font-display text-lg font-medium tracking-tight">
              {display}
            </span>
            {user.userName && (
              <span className="truncate font-mono text-xs text-[var(--color-muted-foreground)]">
                @{user.userName}
              </span>
            )}
          </div>
          <div className="mt-0.5 flex items-center gap-3 text-xs text-[var(--color-muted-foreground)]">
            <span className="truncate font-mono">{user.email ?? "—"}</span>
            {!user.emailConfirmed && (
              <span className="shrink-0 font-mono uppercase tracking-[0.18em] text-[var(--color-destructive)]/80">
                · pending
              </span>
            )}
          </div>
        </div>

        <StatusDot active={user.isActive} />
      </button>
    </li>
  );
}

function StatusDot({ active }: { active: boolean }) {
  return (
    <span className="flex items-center gap-2 font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
      <span
        aria-hidden
        className={cn(
          "h-1.5 w-1.5 rounded-full",
          active ? "bg-[var(--color-foreground)]" : "border border-[var(--color-foreground)]/40 bg-transparent",
        )}
      />
      {active ? "Active" : "Disabled"}
    </span>
  );
}

type SegmentedProps<T extends string> = {
  label: string;
  value: T;
  onChange: (next: T) => void;
  options: ReadonlyArray<{ value: T; label: string }>;
};

function Segmented<T extends string>({ label, value, onChange, options }: SegmentedProps<T>) {
  return (
    <div className="flex items-center gap-2 text-[0.6875rem] font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
      <span>{label}</span>
      <div className="flex overflow-hidden rounded-sm border border-[var(--color-border)]">
        {options.map((o) => {
          const selected = o.value === value;
          return (
            <button
              key={o.value}
              type="button"
              onClick={() => onChange(o.value)}
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
