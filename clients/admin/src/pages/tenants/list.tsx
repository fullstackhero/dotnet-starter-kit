import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Plus } from "lucide-react";
import { listTenants, type TenantDto } from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ApiRequestError } from "@/lib/api-client";

const PAGE_SIZE = 10;

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
}

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span
      className={
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium " +
        (active
          ? "bg-emerald-500/15 text-emerald-500"
          : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]")
      }
    >
      {active ? "Active" : "Inactive"}
    </span>
  );
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

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Tenants</h1>
          <p className="text-sm text-[var(--color-muted-foreground)]">
            Manage tenants registered on this instance.
          </p>
        </div>
        <Button onClick={() => navigate("/tenants/new")}>
          <Plus className="mr-1 h-4 w-4" /> New tenant
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All tenants</CardTitle>
          <CardDescription>
            {data ? `${data.totalCount} total` : "Loading…"}
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {query.isError && (
            <div className="border-t border-[var(--color-border)] px-6 py-4 text-sm text-[var(--color-destructive)]">
              {query.error instanceof ApiRequestError
                ? query.error.problem?.detail ?? query.error.message
                : "Failed to load tenants."}
            </div>
          )}
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Identifier</TableHead>
                <TableHead>Admin email</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Valid until</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {query.isLoading && items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-8 text-center text-sm text-[var(--color-muted-foreground)]">
                    Loading…
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="py-8 text-center text-sm text-[var(--color-muted-foreground)]">
                    No tenants found.
                  </TableCell>
                </TableRow>
              ) : (
                items.map((tenant) => (
                  <TableRow
                    key={tenant.id}
                    className="cursor-pointer hover:bg-[var(--color-muted)]/50"
                    onClick={() => navigate(`/tenants/${tenant.id}`)}
                  >
                    <TableCell className="font-medium">{tenant.name}</TableCell>
                    <TableCell className="font-mono text-xs text-[var(--color-muted-foreground)]">
                      {tenant.id}
                    </TableCell>
                    <TableCell>{tenant.adminEmail}</TableCell>
                    <TableCell>
                      <StatusBadge active={tenant.isActive} />
                    </TableCell>
                    <TableCell>{formatDate(tenant.validUpto)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <div className="flex items-center justify-between text-sm">
        <div className="text-[var(--color-muted-foreground)]">
          {data ? `Page ${data.pageNumber} of ${Math.max(data.totalPages, 1)}` : ""}
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasPrevious || query.isFetching}
            onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft className="mr-1 h-4 w-4" /> Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={!data?.hasNext || query.isFetching}
            onClick={() => setPageNumber((p) => p + 1)}
          >
            Next <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
