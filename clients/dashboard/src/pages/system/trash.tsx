import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Boxes,
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
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import {
  EmptyState,
  ErrorBand,
  PageHero,
  Pagination,
} from "@/components/list";
import {
  describe,
  formatDateMono,
  formatRelative,
  pad2,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;

type TabKey = "products" | "brands" | "categories" | "tickets";

const TABS: ReadonlyArray<{
  key: TabKey;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
}> = [
  { key: "products", label: "Products", icon: Package },
  { key: "brands", label: "Brands", icon: Tags },
  { key: "categories", label: "Categories", icon: FolderTree },
  { key: "tickets", label: "Tickets", icon: Ticket },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function TrashPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<TabKey>("products");
  const [pageNumber, setPageNumber] = useState(1);

  // Reset paging when switching tabs.
  const onTab = (next: TabKey) => {
    setTab(next);
    setPageNumber(1);
  };

  return (
    <div className="space-y-6 pb-12">
      <PageHero
        eyebrow="System · Trash"
        tenant={user?.tenant ?? "—"}
        title="Recycle bin"
        subtitle="Soft-deleted records, kept indefinitely until you restore or purge them. Restoring a row brings it back to its parent list with the same ID and history intact."
      />

      {/* Tab bar — pill nav, mono-caps eyebrow per tab */}
      <nav
        aria-label="Trash sections"
        className="fsh-enter fsh-enter-2 -mx-1 flex flex-wrap gap-1"
      >
        {TABS.map(({ key, label, icon: Icon }) => {
          const active = tab === key;
          return (
            <button
              key={key}
              type="button"
              onClick={() => onTab(key)}
              className={cn(
                "group/tab inline-flex h-9 cursor-pointer items-center gap-1.5 rounded-full px-3.5 text-sm font-medium",
                "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
                "focus-visible:outline-none",
                active
                  ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
              )}
              aria-pressed={active}
            >
              <Icon className="h-3.5 w-3.5" aria-hidden />
              {label}
            </button>
          );
        })}
      </nav>

      {/* Active panel */}
      <section
        className={cn(
          "fsh-enter fsh-enter-3 card-shell relative overflow-hidden rounded-2xl",
          "bg-[var(--color-surface-3)]",
        )}
      >
        {tab === "products" && (
          <ProductsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
        )}
        {tab === "brands" && (
          <BrandsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
        )}
        {tab === "categories" && (
          <CategoriesTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
        )}
        {tab === "tickets" && (
          <TicketsTab pageNumber={pageNumber} setPageNumber={setPageNumber} />
        )}
      </section>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Generic trash tab body — one component per resource so each can read
//  its own typed list/restore from the api modules without unsafe casts.
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
      icon={Package}
      label="Products"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      renderRow={(p: ProductDto) => (
        <TrashRow
          key={p.id}
          title={p.name}
          id={p.id}
          subtitle={`SKU ${p.sku}`}
          deletedOnUtc={p.deletedOnUtc}
          deletedBy={p.deletedBy}
          onRestore={() => restore.mutate(p.id)}
          isRestoring={restore.isPending && restore.variables === p.id}
        />
      )}
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
      icon={Tags}
      label="Brands"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      renderRow={(b: BrandDto) => (
        <TrashRow
          key={b.id}
          title={b.name}
          id={b.id}
          subtitle={`/${b.slug}`}
          deletedOnUtc={b.deletedOnUtc}
          deletedBy={b.deletedBy}
          onRestore={() => restore.mutate(b.id)}
          isRestoring={restore.isPending && restore.variables === b.id}
        />
      )}
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
      icon={FolderTree}
      label="Categories"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      renderRow={(c: CategoryDto) => (
        <TrashRow
          key={c.id}
          title={c.name}
          id={c.id}
          subtitle={`/${c.slug}`}
          deletedOnUtc={c.deletedOnUtc}
          deletedBy={c.deletedBy}
          onRestore={() => restore.mutate(c.id)}
          isRestoring={restore.isPending && restore.variables === c.id}
        />
      )}
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
      icon={Ticket}
      label="Tickets"
      query={query}
      pageNumber={pageNumber}
      setPageNumber={setPageNumber}
      renderRow={(t: TicketDto) => (
        <TrashRow
          key={t.id}
          title={t.title}
          id={t.id}
          subtitle={t.number}
          deletedOnUtc={t.deletedOnUtc}
          deletedBy={t.deletedBy}
          onRestore={() => restore.mutate(t.id)}
          isRestoring={restore.isPending && restore.variables === t.id}
        />
      )}
    />
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Shared shell — handles the loading/empty/error/list rendering so each
//  tab body stays focused on the per-resource details.
// ───────────────────────────────────────────────────────────────────────

type TrashQuery<T> = {
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  data: PagedResponse<T> | undefined;
};

function TrashShell<T>({
  icon: Icon,
  label,
  query,
  pageNumber,
  setPageNumber,
  renderRow,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  query: TrashQuery<T>;
  pageNumber: number;
  setPageNumber: (n: number) => void;
  renderRow: (item: T) => React.ReactNode;
}) {
  const items = query.data?.items ?? [];
  const total = query.data?.totalCount ?? 0;

  return (
    <div>
      {/* Sub-header strip — shows the resource label and total count */}
      <div className="flex flex-wrap items-baseline justify-between gap-2 border-b border-[var(--color-border)] px-6 py-4">
        <div className="flex items-center gap-2.5">
          <span
            aria-hidden
            className={cn(
              "grid h-7 w-7 place-items-center rounded-md",
              "bg-[var(--color-primary-soft)] text-[var(--color-primary)]",
            )}
          >
            <Icon className="h-3.5 w-3.5" />
          </span>
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
            Soft-deleted · {label}
          </span>
        </div>
        <div className="flex items-center gap-2 font-mono text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
          {!query.isLoading && (
            <>
              <span>{total} item{total === 1 ? "" : "s"}</span>
              <span aria-hidden className="opacity-50">·</span>
              <span>page {pad2(pageNumber)}</span>
            </>
          )}
        </div>
      </div>

      {query.isError && (
        <div className="px-6 pt-4">
          <ErrorBand message={describe(query.error)} />
        </div>
      )}

      {query.isLoading ? (
        <ul aria-busy>
          {Array.from({ length: 4 }).map((_, i) => (
            <li
              key={i}
              className="flex items-center gap-4 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
            >
              <Skeleton className="h-9 w-9 rounded-md" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-3 w-1/3" />
              </div>
              <Skeleton className="h-7 w-20 rounded-md" />
            </li>
          ))}
        </ul>
      ) : items.length === 0 ? (
        <EmptyState
          eyebrow="Nothing to recover"
          headline={`The ${label.toLowerCase()} trash is empty.`}
          body={`Soft-deleted ${label.toLowerCase()} land here for as long as you want — no hard-delete clock, no automatic purge. Anything you remove from the main list is recoverable from this view.`}
          icon={<Trash2 className="h-6 w-6 text-[var(--color-primary)]" />}
          primaryAction={{
            label: `Back to ${label.toLowerCase()}`,
            onClick: () => {
              window.location.href = `/${tabPath(label)}`;
            },
          }}
        />
      ) : (
        <>
          <ul role="list">{items.map((it) => renderRow(it))}</ul>
          <div className="px-6 py-4">
            <Pagination
              page={query.data?.pageNumber ?? pageNumber}
              totalPages={Math.max(query.data?.totalPages ?? 1, 1)}
              totalCount={total}
              shown={items.length}
              fetching={false}
              hasPrev={query.data?.hasPrevious ?? false}
              hasNext={query.data?.hasNext ?? false}
              onPrev={() => setPageNumber(Math.max(1, pageNumber - 1))}
              onNext={() => setPageNumber(pageNumber + 1)}
            />
          </div>
        </>
      )}
    </div>
  );
}

function tabPath(label: string): string {
  switch (label) {
    case "Products": return "catalog/products";
    case "Brands": return "catalog/brands";
    case "Categories": return "catalog/categories";
    case "Tickets": return "tickets";
    default: return "";
  }
}

// ───────────────────────────────────────────────────────────────────────
//  Single trashed-item row
// ───────────────────────────────────────────────────────────────────────

function TrashRow({
  title,
  id,
  subtitle,
  deletedOnUtc,
  deletedBy,
  onRestore,
  isRestoring,
}: {
  title: string;
  id: string;
  subtitle: string;
  deletedOnUtc: string | null | undefined;
  deletedBy: string | null | undefined;
  onRestore: () => void;
  isRestoring: boolean;
}) {
  return (
    <li
      className={cn(
        "fsh-enter group/trash-row flex items-center gap-4 border-t border-[var(--color-border)]",
        "first:border-t-0 px-6 py-4",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-surface-4)]",
      )}
    >
      {/* Crossed-out icon plate */}
      <span
        aria-hidden
        className={cn(
          "grid h-9 w-9 shrink-0 place-items-center rounded-md",
          "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          "ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <Trash2 className="h-4 w-4" />
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
          <span className="text-display truncate text-[14.5px] font-medium leading-tight text-[var(--color-foreground)]">
            {title}
          </span>
          <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]">
            {subtitle}
          </code>
        </div>
        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
          <span className="inline-flex items-center gap-1">
            <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
              deleted
            </span>
            <span className="tabular-nums">
              {deletedOnUtc ? formatRelative(deletedOnUtc) : "—"}
            </span>
            {deletedOnUtc && (
              <span className="opacity-50">({formatDateMono(deletedOnUtc)})</span>
            )}
          </span>
          {deletedBy && (
            <span className="inline-flex items-center gap-1">
              <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">
                by
              </span>
              <code className="font-mono text-[10.5px]">{deletedBy.slice(0, 8)}…</code>
            </span>
          )}
          <code className="font-mono text-[10px] opacity-50">id {id.slice(0, 8)}…</code>
        </div>
      </div>

      <Button
        variant="outline"
        size="sm"
        onClick={onRestore}
        disabled={isRestoring}
        className="gap-1.5"
      >
        <RotateCcw className={cn("h-3.5 w-3.5", isRestoring && "animate-spin")} />
        {isRestoring ? "Restoring…" : "Restore"}
      </Button>
    </li>
  );
}

// Suppress unused warnings for shared icons when bundling per-tab views.
void Boxes;
