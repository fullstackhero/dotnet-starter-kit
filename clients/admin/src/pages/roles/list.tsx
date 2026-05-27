import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Plus, ShieldCheck, ChevronRight } from "lucide-react";
import { listRoles, type RoleDto } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader, ErrorBand, LoadingRow } from "@/components/list";
import { EmptyState } from "@/components/empty-state";
import { ApiRequestError } from "@/lib/api-client";

const ROOT_ROLE_NAMES = new Set(["Admin", "Basic"]);

export function RolesListPage() {
  const navigate = useNavigate();
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

  return (
    <div className="space-y-8">
      <PageHeader
        crumbs={[{ label: "\\ Roles" }, { label: "Registry", muted: true }]}
        trailing={roles.length ? `${String(roles.length).padStart(2, "0")} roles` : "—"}
        title="Roles"
        description="Define what people can do. Each role bundles a set of permissions; users inherit a role's permissions by being assigned to it."
        actions={
          <Button onClick={() => navigate("/roles/new")}>
            <Plus className="mr-1 h-4 w-4" /> New role
          </Button>
        }
      />

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load roles."
          }
        />
      )}

      <div className="border-t border-[var(--color-border)]">
        {query.isLoading && <LoadingRow label="Loading roles" />}

        {!query.isLoading && roles.length === 0 && !query.isError && (
          <EmptyState
            icon={ShieldCheck}
            kicker="// no roles"
            title="No roles defined yet."
            description="Create your first role to start bundling permissions."
            action={
              <Button onClick={() => navigate("/roles/new")}>
                <Plus className="mr-1 h-4 w-4" /> New role
              </Button>
            }
          />
        )}

        {roles.length > 0 && (
          <ol className="divide-y divide-[var(--color-border)]">
            {roles.map((role, i) => (
              <RoleRow
                key={role.id}
                index={i + 1}
                role={role}
                onClick={() => navigate(`/roles/${role.id}`)}
              />
            ))}
          </ol>
        )}
      </div>
    </div>
  );
}

function RoleRow({
  role,
  index,
  onClick,
}: {
  role: RoleDto;
  index: number;
  onClick: () => void;
}) {
  const num = String(index).padStart(2, "0");
  const isSystem = ROOT_ROLE_NAMES.has(role.name);
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className="group grid w-full grid-cols-[3rem_auto_1fr_auto] items-center gap-4 px-1 py-4 text-left transition-colors hover:bg-[var(--color-muted)]/50 focus:outline-none focus-visible:bg-[var(--color-muted)]/50"
      >
        <span className="font-mono text-xs tabular-nums text-[var(--color-muted-foreground)]">
          #{num}
        </span>

        <span className="grid h-9 w-9 place-items-center rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-muted-foreground)]">
          <ShieldCheck className="h-4 w-4" />
        </span>

        <div className="min-w-0">
          <div className="flex flex-wrap items-baseline gap-2">
            <span className="truncate font-display text-lg font-medium tracking-tight">
              {role.name}
            </span>
            {isSystem && (
              <Badge variant="outline" className="font-mono uppercase tracking-[0.14em]">
                system
              </Badge>
            )}
          </div>
          {role.description && (
            <div className="mt-0.5 truncate text-xs text-[var(--color-muted-foreground)]">
              {role.description}
            </div>
          )}
          <div className="mt-1 font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            id · <code className="code-chip ml-1">{role.id}</code>
          </div>
        </div>

        <ChevronRight className="h-4 w-4 text-[var(--color-muted-foreground)] transition-transform group-hover:translate-x-0.5" />
      </button>
    </li>
  );
}
