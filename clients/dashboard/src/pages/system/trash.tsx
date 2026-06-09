import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  FileText,
  FolderTree,
  Package,
  RotateCcw,
  Tags,
  Ticket,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import {
  listTrashedBrands,
  listTrashedCategories,
  listTrashedProducts,
  restoreBrand,
  restoreCategory,
  restoreProduct,
  type BrandDto,
  type CategoryDto,
  type PagedResponse,
  type ProductDto,
} from "@/api/catalog";
import {
  listTrashedTickets,
  restoreTicket,
  type TicketDto,
} from "@/api/tickets";
import {
  listTrashedFiles,
  restoreFile,
  type FileAssetDto,
} from "@/api/files";
import { useAuth } from "@/auth/use-auth";
import {
  TRASH_TAB_PERMISSIONS,
  type TrashTabKey,
} from "@/lib/trash-permissions";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/cn";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityPager,
} from "@/components/list";
import {
  describe,
  formatDateMono,
  formatRelative,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1.5fr_140px_140px_100px]";

type TabKey = TrashTabKey;

const TABS: ReadonlyArray<{
  key: TabKey;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Permission gating this tab — mirrors what its trash endpoint enforces. */
  perm: string;
}> = [
  { key: "products", label: "Products", icon: Package, perm: TRASH_TAB_PERMISSIONS.products },
  { key: "brands", label: "Brands", icon: Tags, perm: TRASH_TAB_PERMISSIONS.brands },
  { key: "categories", label: "Categories", icon: FolderTree, perm: TRASH_TAB_PERMISSIONS.categories },
  { key: "tickets", label: "Tickets", icon: Ticket, perm: TRASH_TAB_PERMISSIONS.tickets },
  { key: "files", label: "Files", icon: FileText, perm: TRASH_TAB_PERMISSIONS.files },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TrashPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<TabKey>("products");
  const [pageNumber, setPageNumber] = useState(1);

  // Show only the tabs whose trash endpoint the user can actually reach, so they
  // never click into a guaranteed 403. The server still enforces (defence in
  // depth) — this is the UX layer. The nav already hides the Trash entry when
  // the user has none of these, but the route is still directly reachable, so
  // we handle the empty case here too.
  const perms = user?.permissions;
  const visibleTabs = useMemo(
    () => TABS.filter((t) => perms?.includes(t.perm) ?? false),
    [perms],
  );
  // The selected tab may be one the user can't see (initial default, or a
  // permission they lost) — fall back to the first visible tab.
  const activeTab = visibleTabs.some((t) => t.key === tab)
    ? tab
    : visibleTabs[0]?.key;

  // Reset paging when switching tabs.
  const onTab = (next: TabKey) => {
    setTab(next);
    setPageNumber(1);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Trash2}
        title="Recycle bin"
        description="Soft-deleted records, kept indefinitely until you restore them. Restoring a row brings it back to its parent list with the same ID and history intact."
      />

      {visibleTabs.length === 0 ? (
        <EntityEmpty
          icon={Trash2}
          title="No recycle bins available"
          body="You don't have permission to restore deleted records in this tenant. Ask an administrator if you think you should."
        />
      ) : (
        <>
      {/* Tab pills */}
      <nav
        aria-label="Trash sections"
        className="flex flex-wrap items-center gap-2"
      >
        {visibleTabs.map(({ key, label, icon: Icon }) => {
          const active = activeTab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => onTab(key)}
              aria-pressed={active}
              className={cn(
                "inline-flex h-8 cursor-pointer items-center gap-1.5 rounded-full border px-3 text-[12px] font-medium transition-colors duration-[var(--duration-fast)]",
                active
                  ? "border-transparent bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                  : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
              )}
            >
              <Icon className="size-3.5" aria-hidden />
              {label}
            </button>
          );
        })}
      </nav>

      {/* Active panel */}
      {activeTab === "products" && (
        <ProductsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {activeTab === "brands" && (
        <BrandsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {activeTab === "categories" && (
        <CategoriesTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {activeTab === "tickets" && (
        <TicketsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
      {activeTab === "files" && (
        <FilesTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
      )}
        </>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Per-resource tabs (each owns its own query + restore mutation)
// ───────────────────────────────────────────────────────────────────────

function ProductsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "products", pageNumber],
    queryFn: () => listTrashedProducts(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreProduct(id),
    onSuccess: () => {
      toast.success("Product restored");
      void queryClient.invalidateQueries({ queryKey: ["trash", "products"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      label="Products"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(p: ProductDto) => ({
        id: p.id,
        title: p.name,
        subtitle: `SKU ${p.sku}`,
        deletedOnUtc: p.deletedOnUtc,
        deletedBy: p.deletedBy,
        isRestoring: restore.isPending && restore.variables === p.id,
        onRestore: () => restore.mutate(p.id),
      })}
    />
  );
}

function BrandsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "brands", pageNumber],
    queryFn: () => listTrashedBrands(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreBrand(id),
    onSuccess: () => {
      toast.success("Brand restored");
      void queryClient.invalidateQueries({ queryKey: ["trash", "brands"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      label="Brands"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(b: BrandDto) => ({
        id: b.id,
        title: b.name,
        subtitle: `/${b.slug}`,
        deletedOnUtc: b.deletedOnUtc,
        deletedBy: b.deletedBy,
        isRestoring: restore.isPending && restore.variables === b.id,
        onRestore: () => restore.mutate(b.id),
      })}
    />
  );
}

function CategoriesTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "categories", pageNumber],
    queryFn: () => listTrashedCategories(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreCategory(id),
    onSuccess: () => {
      toast.success("Category restored");
      void queryClient.invalidateQueries({ queryKey: ["trash", "categories"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      label="Categories"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(c: CategoryDto) => ({
        id: c.id,
        title: c.name,
        subtitle: `/${c.slug}`,
        deletedOnUtc: c.deletedOnUtc,
        deletedBy: c.deletedBy,
        isRestoring: restore.isPending && restore.variables === c.id,
        onRestore: () => restore.mutate(c.id),
      })}
    />
  );
}

function TicketsTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "tickets", pageNumber],
    queryFn: () => listTrashedTickets(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreTicket(id),
    onSuccess: () => {
      toast.success("Ticket restored");
      void queryClient.invalidateQueries({ queryKey: ["trash", "tickets"] });
      void queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      label="Tickets"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(t: TicketDto) => ({
        id: t.id,
        title: t.title,
        subtitle: t.number,
        deletedOnUtc: t.deletedOnUtc,
        deletedBy: t.deletedBy,
        isRestoring: restore.isPending && restore.variables === t.id,
        onRestore: () => restore.mutate(t.id),
      })}
    />
  );
}

function FilesTab({
  pageNumber,
  setPageNumber,
}: {
  pageNumber: number;
  setPageNumber: (n: number) => void;
}) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "files", pageNumber],
    queryFn: () => listTrashedFiles(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreFile(id),
    onSuccess: () => {
      toast.success("File restored");
      void queryClient.invalidateQueries({ queryKey: ["trash", "files"] });
      void queryClient.invalidateQueries({ queryKey: ["files"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      label="Files"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      mapRow={(f: FileAssetDto) => ({
        id: f.id,
        title: f.originalFileName,
        subtitle: f.contentType,
        deletedOnUtc: f.deletedOnUtc,
        deletedBy: f.deletedBy,
        isRestoring: restore.isPending && restore.variables === f.id,
        onRestore: () => restore.mutate(f.id),
      })}
    />
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Shared shell — list/loading/empty/error rendering for each tab body.
// ───────────────────────────────────────────────────────────────────────

type RowVm = {
  id: string;
  title: string;
  subtitle: string;
  deletedOnUtc: string | null | undefined;
  deletedBy: string | null | undefined;
  isRestoring: boolean;
  onRestore: () => void;
};

type TrashQuery<T> = {
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  data: PagedResponse<T> | undefined;
};

function TrashShell<T>({
  label,
  query,
  pageNumber,
  setPageNumber,
  mapRow,
}: {
  label: string;
  query: TrashQuery<T>;
  pageNumber: number;
  setPageNumber: (n: number) => void;
  mapRow: (item: T) => RowVm;
}) {
  const navigate = useNavigate();
  const [pendingId, setPendingId] = useState<string | null>(null);
  const items = query.data?.items ?? [];
  const total = query.data?.totalCount ?? 0;
  const rows = items.map(mapRow);
  const pendingRow = rows.find((r) => r.id === pendingId) ?? null;

  if (query.isLoading && rows.length === 0) {
    return <EntityListLoading desktopColumns={DESKTOP_COLS} />;
  }

  if (query.isError) {
    return (
      <div
        role="alert"
        className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
      >
        <span>{describe(query.error)}</span>
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <EntityEmpty
        icon={Trash2}
        title={`The ${label.toLowerCase()} trash is empty`}
        body={`Soft-deleted ${label.toLowerCase()} land here for as long as you want — no automatic purge. Anything you remove from the main list can be recovered.`}
        action={
          <Button
            variant="outline"
            onClick={() => navigate(`/${tabPath(label)}`)}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            Back to {label.toLowerCase()}
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
          {total} {label.toLowerCase()}
          {total !== 1 ? "" : ""} in trash
        </p>
      </div>

      {/* Mobile cards */}
      <div className="space-y-2 md:hidden">
        {rows.map((row) => (
          <TrashMobileCard
            key={row.id}
            row={row}
            onRequestRestore={() => setPendingId(row.id)}
          />
        ))}
      </div>

      {/* Desktop list */}
      <EntityListCard className="hidden md:block">
        <EntityListHeader className={DESKTOP_COLS}>
          <span>Entity</span>
          <span>Deleted by</span>
          <span>Deleted at</span>
          <span className="text-right">Actions</span>
        </EntityListHeader>
        {rows.map((row, i) => (
          <TrashDesktopRow
            key={row.id}
            row={row}
            isLast={i === rows.length - 1}
            onRequestRestore={() => setPendingId(row.id)}
          />
        ))}
      </EntityListCard>

      <EntityPager
        page={query.data?.pageNumber ?? pageNumber}
        totalPages={Math.max(query.data?.totalPages ?? 1, 1)}
        hasPrev={query.data?.hasPrevious ?? false}
        hasNext={query.data?.hasNext ?? false}
        onPrev={() => setPageNumber(Math.max(1, pageNumber - 1))}
        onNext={() => setPageNumber(pageNumber + 1)}
      />

      <RestoreConfirmDialog
        row={pendingRow}
        label={label}
        onClose={() => setPendingId(null)}
        onConfirm={() => {
          pendingRow?.onRestore();
          setPendingId(null);
        }}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Restore confirmation
// ───────────────────────────────────────────────────────────────────────

function RestoreConfirmDialog({
  row,
  label,
  onClose,
  onConfirm,
}: {
  row: RowVm | null;
  label: string;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const singular = label.replace(/s$/, "").toLowerCase();
  return (
    <Dialog open={row !== null} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Restore {singular}?</DialogTitle>
          <DialogDescription>
            <span className="font-medium text-[var(--color-foreground)]">{row?.title}</span>{" "}
            will be moved back to its {singular} list with the same ID and history intact.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button type="button" onClick={onConfirm} className="gap-1.5">
            <RotateCcw className="size-3.5" />
            Restore {singular}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function tabPath(label: string): string {
  switch (label) {
    case "Products": return "catalog/products";
    case "Brands": return "catalog/brands";
    case "Categories": return "catalog/categories";
    case "Tickets": return "tickets";
    case "Files": return "files";
    default: return "";
  }
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function TrashMobileCard({
  row,
  onRequestRestore,
}: {
  row: RowVm;
  onRequestRestore: () => void;
}) {
  return (
    <div
      className={cn(
        "block rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
      )}
    >
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={row.title} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {row.title}
            </p>
            <code className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {row.subtitle}
            </code>
          </div>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={onRequestRestore}
          disabled={row.isRestoring}
          className="shrink-0 gap-1.5"
        >
          <RotateCcw className={cn("size-3.5", row.isRestoring && "animate-spin")} />
          {row.isRestoring ? "…" : "Restore"}
        </Button>
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11px] text-[var(--color-muted-foreground)]">
        <span className="tabular-nums">
          {row.deletedOnUtc ? formatRelative(row.deletedOnUtc) : "—"}
        </span>
        {row.deletedOnUtc && (
          <span className="opacity-60">({formatDateMono(row.deletedOnUtc)})</span>
        )}
        {row.deletedBy && (
          <code className="font-mono">by {row.deletedBy.slice(0, 8)}…</code>
        )}
      </div>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function TrashDesktopRow({
  row,
  isLast,
  onRequestRestore,
}: {
  row: RowVm;
  isLast: boolean;
  onRequestRestore: () => void;
}) {
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast}>
      {/* Entity */}
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={row.title} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
            {row.title}
          </div>
          <code className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {row.subtitle}
          </code>
        </div>
      </div>

      {/* Deleted by */}
      <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
        {row.deletedBy ? `${row.deletedBy.slice(0, 8)}…` : "—"}
      </code>

      {/* Deleted at */}
      <div className="text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {row.deletedOnUtc ? (
          <>
            <div>{formatRelative(row.deletedOnUtc)}</div>
            <div className="text-[10.5px] opacity-70">{formatDateMono(row.deletedOnUtc)}</div>
          </>
        ) : (
          "—"
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end">
        <Button
          variant="outline"
          size="sm"
          onClick={onRequestRestore}
          disabled={row.isRestoring}
          className="gap-1.5"
        >
          <RotateCcw className={cn("size-3.5", row.isRestoring && "animate-spin")} />
          {row.isRestoring ? "Restoring…" : "Restore"}
        </Button>
      </div>
    </EntityListRow>
  );
}