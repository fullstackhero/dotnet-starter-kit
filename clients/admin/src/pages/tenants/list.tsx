import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Plus } from "lucide-react";
import { listTenants, type TenantDto } from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Monogram } from "@/components/monogram";
import { SectionRule } from "@/components/section-rule";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 12;

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
}

export function TenantsListPage() {
  const [pageNumber, setPageNumber] = useState(1);
  const navigate = useNavigate();

  const query = useQuery({
    queryKey: ["tenants", { pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listTenants({ pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items: TenantDto[] = data?.items ?? [];
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
        crumbs={[{ label: "\\ Tenants" }, { label: "Registry", muted: true }]}
        trailing={pageBadge.toUpperCase()}
      />

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-4xl font-semibold tracking-tight md:text-5xl">
            Registry
          </h1>
          <p className="mt-2 max-w-xl text-sm text-[var(--color-muted-foreground)]">
            {data
              ? `${data.totalCount} ${data.totalCount === 1 ? "tenant" : "tenants"} registered on this instance.`
              : "Loading the registry…"}
          </p>
        </div>
        <Button onClick={() => navigate("/tenants/new")} className="shrink-0">
          <Plus className="mr-1 h-4 w-4" /> New tenant
        </Button>
      </div>

      {/* Roster */}
      <div className="border-t border-[var(--color-border)]">
        {query.isError && (
          <div className="border-b border-[var(--color-border)] px-1 py-4 text-sm text-[var(--color-destructive)]">
            {query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : "Failed to load tenants."}
          </div>
        )}

        {query.isLoading && items.length === 0 && (
          <div className="px-1 py-12 text-center text-sm font-mono uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            Loading…
          </div>
        )}

        {!query.isLoading && items.length === 0 && (
          <div className="px-1 py-16 text-center">
            <p className="font-display text-2xl">No tenants yet.</p>
            <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
              Provision the first tenant to get started.
            </p>
          </div>
        )}

        <ol className="divide-y divide-[var(--color-border)]">
          {items.map((tenant, i) => (
            <TenantRow
              key={tenant.id}
              tenant={tenant}
              index={baseIndex + i + 1}
              onClick={() => navigate(`/tenants/${tenant.id}`)}
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
              disabled={!data.hasPrevious || query.isFetching}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            >
              <ChevronLeft className="mr-1 h-3.5 w-3.5" /> Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNext || query.isFetching}
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

function TenantRow({
  tenant,
  index,
  onClick,
}: {
  tenant: TenantDto;
  index: number;
  onClick: () => void;
}) {
  const num = String(index).padStart(3, "0");
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className="group grid w-full grid-cols-[3.5rem_2.5rem_1fr_auto] items-center gap-4 px-1 py-4 text-left transition-colors hover:bg-[var(--color-muted)]/50 focus:outline-none focus-visible:bg-[var(--color-muted)]/50"
      >
        <span className="font-mono text-xs tabular-nums text-[var(--color-muted-foreground)]">
          #{num}
        </span>

        <Monogram seed={tenant.id} fallback={tenant.name} size="md" />

        <div className="min-w-0">
          <div className="flex items-baseline gap-2">
            <span className="truncate font-display text-lg font-medium tracking-tight">
              {tenant.name}
            </span>
            <span className="truncate font-mono text-xs text-[var(--color-muted-foreground)]">
              {tenant.id}
            </span>
          </div>
          <div className="mt-0.5 flex flex-wrap items-center gap-3 text-xs text-[var(--color-muted-foreground)]">
            <span className="truncate font-mono">{tenant.adminEmail}</span>
            <span className="font-mono text-[10.5px] uppercase tracking-[0.18em]">
              valid · {formatDate(tenant.validUpto)}
            </span>
          </div>
        </div>

        <StatusDot active={tenant.isActive} />
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
      {active ? "Active" : "Inactive"}
    </span>
  );
}
