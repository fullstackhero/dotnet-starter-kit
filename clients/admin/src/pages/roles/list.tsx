import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus, Shield, ShieldCheck } from "lucide-react";
import { listRoles, type RoleDto } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { EntityPageHeader, ErrorBand, LoadingRow } from "@/components/list";
import { EmptyState } from "@/components/empty-state";
import { ApiRequestError } from "@/lib/api-client";
import { CreateRoleDialog } from "@/components/roles/create-role-dialog";

const ROOT_ROLE_NAMES = new Set(["Admin", "Basic"]);

// Desktop grid template — shared by header + rows.
const DESKTOP_COLS =
  "grid-cols-[1fr_120px_24px] lg:grid-cols-[1.4fr_2fr_120px_24px]";

export function RolesListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search.trim().toLowerCase()), 200);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({ queryKey: ["roles"], queryFn: listRoles });

  const roles = useMemo(() => {
    const items = query.data ?? [];
    return [...items].sort((a, b) => {
      // System roles first, then alphabetical.
      const aSys = ROOT_ROLE_NAMES.has(a.name) ? 0 : 1;
      const bSys = ROOT_ROLE_NAMES.has(b.name) ? 0 : 1;
      if (aSys !== bSys) return aSys - bSys;
      return a.name.localeCompare(b.name);
    });
  }, [query.data]);

  const filtered = useMemo(() => {
    if (!debounced) return roles;
    return roles.filter(
      (r) =>
        r.name.toLowerCase().includes(debounced) ||
        (r.description ?? "").toLowerCase().includes(debounced),
    );
  }, [roles, debounced]);

  const searchActive = debounced.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Shield}
        title="Roles"
        total={query.data ? roles.length : null}
        unit="role"
        description="Define what people can do. Each role bundles a set of permissions; users inherit a role's permissions by being assigned to it."
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New role
        </Button>
      </EntityPageHeader>

      {/* Search */}
      <div className="relative w-full max-w-sm">
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-muted-foreground)]">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
        </span>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by name or description…"
          aria-label="Search roles"
          className="h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-3 text-[13px] outline-none transition-colors placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)] focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
        />
      </div>

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load roles."
          }
        />
      )}

      {query.isLoading && <LoadingRow label="Loading roles" />}

      {!query.isLoading && filtered.length === 0 && !query.isError && (
        searchActive ? (
          <div className="py-16 text-center">
            <p className="font-display text-2xl">No roles found.</p>
            <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
              Nothing matches &ldquo;{debounced}&rdquo;. Try a different term.
            </p>
            <Button
              variant="outline"
              className="mt-4 h-9 rounded-lg px-4 text-[13px]"
              onClick={() => setSearch("")}
            >
              Clear search
            </Button>
          </div>
        ) : (
          <EmptyState
            icon={ShieldCheck}
            kicker="// no roles"
            title="No roles defined yet."
            description="Create your first role to start bundling permissions."
            action={
              <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 h-4 w-4" /> New role
              </Button>
            }
          />
        )
      )}

      {filtered.length > 0 && (
        <div>
          <p className="mb-3 text-[12px] font-medium text-[var(--color-muted-foreground)]">
            {filtered.length} role{filtered.length !== 1 ? "s" : ""} found
          </p>

          {/* Mobile card list */}
          <div className="space-y-2 md:hidden">
            {filtered.map((role) => (
              <RoleMobileCard
                key={role.id}
                role={role}
                onClick={() => navigate(`/roles/${role.id}`)}
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
              <span className="hidden text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] lg:block">
                Description
              </span>
              <span className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Permissions
              </span>
              <span />
            </div>

            <ol className="divide-y divide-[var(--color-border)]">
              {filtered.map((role, i) => (
                <RoleDesktopRow
                  key={role.id}
                  role={role}
                  isLast={i === filtered.length - 1}
                  onClick={() => navigate(`/roles/${role.id}`)}
                />
              ))}
            </ol>
          </div>
        </div>
      )}

      <CreateRoleDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}

// ─── Mobile card ────────────────────────────────────────────────────────

function RoleMobileCard({
  role,
  onClick,
}: {
  role: RoleDto;
  onClick: () => void;
}) {
  const isSystem = ROOT_ROLE_NAMES.has(role.name);
  return (
    <li className="list-none">
      <button
        type="button"
        onClick={onClick}
        aria-label={`Open role ${role.name}`}
        className="group w-full overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left shadow-xs transition-colors hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
      >
        <div className="flex items-center justify-between">
          <div className="flex min-w-0 items-center gap-3">
            <span
              aria-hidden
              className="grid size-10 shrink-0 place-items-center rounded-lg"
              style={{
                backgroundColor: "oklch(from var(--color-primary) l c h / 0.10)",
                boxShadow: "inset 0 0 0 1px oklch(from var(--color-primary) l c h / 0.22)",
                color: "var(--color-primary)",
              }}
            >
              <Shield className="size-4" />
            </span>
            <div className="min-w-0">
              <div className="flex items-center gap-1.5">
                <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
                  {role.name}
                </p>
                {isSystem && (
                  <Badge variant="outline" className="font-mono text-[10px] uppercase tracking-[0.14em]">
                    System
                  </Badge>
                )}
              </div>
              <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
                {role.description ?? <span className="italic opacity-60">No description on file.</span>}
              </p>
            </div>
          </div>
          <ChevronRight className="size-4 shrink-0 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
        </div>
        {role.permissions != null && (
          <div className="mt-2 ml-[52px]">
            <span className="inline-flex items-center rounded-full bg-[oklch(from_var(--color-info)_l_c_h_/_0.12)] px-2 py-0.5 text-[10.5px] font-medium text-[var(--color-info)]">
              {role.permissions.length}{" "}
              {role.permissions.length === 1 ? "permission" : "permissions"}
            </span>
          </div>
        )}
      </button>
    </li>
  );
}

// ─── Desktop row ────────────────────────────────────────────────────────

function RoleDesktopRow({
  role,
  isLast,
  onClick,
}: {
  role: RoleDto;
  isLast: boolean;
  onClick: () => void;
}) {
  const isSystem = ROOT_ROLE_NAMES.has(role.name);
  const permLabel =
    role.permissions === undefined || role.permissions === null
      ? "—"
      : `${role.permissions.length} ${role.permissions.length === 1 ? "permission" : "permissions"}`;

  return (
    <li className="list-none">
      <button
        type="button"
        onClick={onClick}
        className={`group grid w-full items-center gap-3 px-4 py-3.5 text-left transition-colors hover:bg-[var(--color-accent)] focus-visible:outline-none focus-visible:bg-[var(--color-accent)] ${DESKTOP_COLS} ${isLast ? "" : ""}`}
      >
        {/* Name */}
        <div className="flex min-w-0 items-center gap-3">
          <span
            aria-hidden
            className="grid size-9 shrink-0 place-items-center rounded-lg"
            style={{
              backgroundColor: "oklch(from var(--color-primary) l c h / 0.10)",
              boxShadow: "inset 0 0 0 1px oklch(from var(--color-primary) l c h / 0.22)",
              color: "var(--color-primary)",
            }}
          >
            <Shield className="size-4" />
          </span>
          <span className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {role.name}
          </span>
          {isSystem && (
            <Badge variant="outline" className="shrink-0 font-mono text-[10px] uppercase tracking-[0.14em]">
              System
            </Badge>
          )}
        </div>

        {/* Description (lg+) */}
        <div className="hidden lg:block">
          <p className="truncate text-[12.5px] text-[var(--color-muted-foreground)]">
            {role.description ?? <span className="italic opacity-60">No description on file.</span>}
          </p>
        </div>

        {/* Permission count */}
        <span className="font-mono text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
          {permLabel}
        </span>

        <div className="flex items-center justify-end">
          <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
        </div>
      </button>
    </li>
  );
}
