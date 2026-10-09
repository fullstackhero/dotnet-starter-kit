import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
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
  icon: React.ComponentType<{ className?: string }>;
  /** Permission gating this tab — mirrors what its trash endpoint enforces. */
  perm: string;
}> = [
  { key: "products", icon: Package, perm: TRASH_TAB_PERMISSIONS.products },
  { key: "brands", icon: Tags, perm: TRASH_TAB_PERMISSIONS.brands },
  { key: "categories", icon: FolderTree, perm: TRASH_TAB_PERMISSIONS.categories },
  { key: "tickets", icon: Ticket, perm: TRASH_TAB_PERMISSIONS.tickets },
  { key: "files", icon: FileText, perm: TRASH_TAB_PERMISSIONS.files },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TrashPage() {
  const { t } = useTranslation("system");
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
        title={t("trash.title")}
        description={t("trash.description")}
      />

      {visibleTabs.length === 0 ? (
        <EntityEmpty
          icon={Trash2}
          title={t("trash.noAccessTitle")}
          body={t("trash.noAccessBody")}
        />
      ) : (
        <>
      {/* Tab pills */}
      <nav
        aria-label={t("trash.sections")}
        className="flex flex-wrap items-center gap-2"
      >
        {visibleTabs.map(({ key, icon: Icon }) => {
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
              {t(`trash.tab.${key}`)}
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
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "products", pageNumber],
    queryFn: () => listTrashedProducts(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreProduct(id),
    onSuccess: () => {
      toast.success(t("trash.toast.products"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "products"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="products"
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
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "brands", pageNumber],
    queryFn: () => listTrashedBrands(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreBrand(id),
    onSuccess: () => {
      toast.success(t("trash.toast.brands"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "brands"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="brands"
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
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "categories", pageNumber],
    queryFn: () => listTrashedCategories(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreCategory(id),
    onSuccess: () => {
      toast.success(t("trash.toast.categories"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "categories"] });
      void queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="categories"
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
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "tickets", pageNumber],
    queryFn: () => listTrashedTickets(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreTicket(id),
    onSuccess: () => {
      toast.success(t("trash.toast.tickets"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "tickets"] });
      void queryClient.invalidateQueries({ queryKey: ["tickets"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="tickets"
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
  const { t } = useTranslation("system");
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["trash", "files", pageNumber],
    queryFn: () => listTrashedFiles(pageNumber, PAGE_SIZE),
  });
  const restore = useMutation({
    mutationFn: (id: string) => restoreFile(id),
    onSuccess: () => {
      toast.success(t("trash.toast.files"));
      void queryClient.invalidateQueries({ queryKey: ["trash", "files"] });
      void queryClient.invalidateQueries({ queryKey: ["files"] });
    },
    onError: (e) => toast.error(describe(e)),
  });
  return (
    <TrashShell
      tabKey="files"
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
  tabKey,
  query,
  pageNumber,
  setPageNumber,
  mapRow,
}: {
  tabKey: TabKey;
  query: TrashQuery<T>;
  pageNumber: number;
  setPageNumber: (n: number) => void;
  mapRow: (item: T) => RowVm;
}) {
  const { t } = useTranslation("system");
  const navigate = useNavigate();
  const [pendingId, setPendingId] = useState<string | null>(null);
  const items = query.data?.items ?? [];
  const total = query.data?.totalCount ?? 0;
  const rows = items.map(mapRow);
  const pendingRow = rows.find((r) => r.id === pendingId) ?? null;
  const plural = t(`trash.entityPlural.${tabKey}`);

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
        title={t("trash.emptyTitle", { entity: plural })}
        body={t("trash.emptyBody", { entity: plural })}
        action={
          <Button
            variant="outline"
            onClick={() => navigate(`/${tabPath(tabKey)}`)}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            {t("trash.backTo", { entity: plural })}
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
          {t("trash.count", { n: total, entity: plural })}
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
          <span>{t("trash.col.entity")}</span>
          <span>{t("trash.col.deletedBy")}</span>
          <span>{t("trash.col.deletedAt")}</span>
          <span className="text-right">{t("trash.col.actions")}</span>
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
        tabKey={tabKey}
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
  tabKey,
  onClose,
  onConfirm,
}: {
  row: RowVm | null;
  tabKey: TabKey;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const { t } = useTranslation("system");
  const singular = t(`trash.entitySingular.${tabKey}`);
  return (
    <Dialog open={row !== null} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("trash.restore.title", { entity: singular })}</DialogTitle>
          <DialogDescription>
            <span className="font-medium text-[var(--color-foreground)]">{row?.title}</span>{" "}
            {t("trash.restore.body", { entity: singular })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              {t("trash.restore.cancel")}
            </Button>
          </DialogClose>
          <Button type="button" onClick={onConfirm} className="gap-1.5">
            <RotateCcw className="size-3.5" />
            {t("trash.restore.confirm", { entity: singular })}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function tabPath(tabKey: TabKey): string {
  switch (tabKey) {
    case "products": return "catalog/products";
    case "brands": return "catalog/brands";
    case "categories": return "catalog/categories";
    case "tickets": return "tickets";
    case "files": return "files";
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
  const { t } = useTranslation("system");
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
          {row.isRestoring ? "…" : t("trash.action.restore")}
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
          <code className="font-mono">{t("trash.by", { id: row.deletedBy.slice(0, 8) })}</code>
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
  const { t } = useTranslation("system");
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
          {row.isRestoring ? t("trash.action.restoring") : t("trash.action.restore")}
        </Button>
      </div>
    </EntityListRow>
  );
}