import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowDown,
  CircleDollarSign,
  Minus,
  Package,
  PackageX,
  Pencil,
  Plus,
  Search,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import { Link } from "react-router-dom";
import { toast } from "sonner";
import {
  adjustProductStock,
  changeProductPrice,
  createProduct,
  deleteProduct,
  searchBrands,
  searchCategories,
  searchProducts,
  updateProduct,
  type AdjustProductStockInput,
  type BrandDto,
  type CategoryDto,
  type ChangeProductPriceInput,
  type CreateProductInput,
  type ProductDto,
  type UpdateProductInput,
} from "@/api/catalog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import {
  Combobox,
  DensityToggle,
  EmptyState,
  ErrorBand,
  Field,
  ListHero,
  Pagination,
  SortChips,
  Stat,
  StatStrip,
  usePersistedDensity,
  type Density,
  type SortDir,
  type SortOption,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatDateMono,
  formatMoney,
  formatRelative,
  pad2,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DENSITY_KEY = "fsh.dashboard.catalog.products.density";
const LOW_STOCK = 10;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; product: ProductDto }
  | { mode: "delete"; product: ProductDto }
  | { mode: "price"; product: ProductDto }
  | { mode: "stock"; product: ProductDto };

type SortKey = "name" | "sku" | "createdAtUtc" | "stock";

const SORT_OPTIONS: SortOption<SortKey>[] = [
  { key: "createdAtUtc", label: "Created" },
  { key: "name", label: "Name" },
  { key: "sku", label: "SKU" },
  { key: "stock", label: "Stock" },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function ProductsPage() {
  const { user } = useAuth();
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const [brandFilter, setBrandFilter] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<boolean | null>(null);

  const [sortKey, setSortKey] = useState<SortKey>("createdAtUtc");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  const [density, setDensity] = usePersistedDensity(DENSITY_KEY);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPageNumber(1);
  }, [brandFilter, categoryFilter, activeFilter]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "products",
      {
        search: debouncedSearch,
        brandId: brandFilter,
        categoryId: categoryFilter,
        isActive: activeFilter,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortKey,
        sortDir,
      },
    ],
    queryFn: () =>
      searchProducts({
        search: debouncedSearch || undefined,
        brandId: brandFilter,
        categoryId: categoryFilter,
        isActive: activeFilter,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: sortKey,
        sortDir,
      }),
    placeholderData: keepPreviousData,
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "all-for-products-filter"],
    queryFn: () => searchBrands({ pageSize: 200 }),
    staleTime: 60_000,
  });
  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "all-for-products-filter"],
    queryFn: () => searchCategories({ pageSize: 200 }),
    staleTime: 60_000,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const brandsById = useMemo(() => {
    const map = new Map<string, BrandDto>();
    (brandsQuery.data?.items ?? []).forEach((b) => map.set(b.id, b));
    return map;
  }, [brandsQuery.data]);

  const categoriesById = useMemo(() => {
    const map = new Map<string, CategoryDto>();
    (categoriesQuery.data?.items ?? []).forEach((c) => map.set(c.id, c));
    return map;
  }, [categoriesQuery.data]);

  // Server-side sort drives the order. Items are already sorted on arrival.
  const sortedItems = items;

  const stats = useMemo(() => {
    if (!data) return null;
    const total = data.totalCount;
    const active = items.filter((p) => p.isActive).length;
    const lowStock = items.filter((p) => p.stock > 0 && p.stock < LOW_STOCK).length;
    const outOfStock = items.filter((p) => p.stock === 0).length;
    const avgPrice =
      items.length === 0
        ? 0
        : items.reduce((s, p) => s + p.price.amount, 0) / items.length;
    const currency = items[0]?.price.currency ?? "USD";
    return { total, active, lowStock, outOfStock, avgPrice, currency };
  }, [data, items]);

  const onSort = useCallback(
    (key: SortKey) => {
      if (sortKey === key) {
        setSortDir((d) => (d === "asc" ? "desc" : "asc"));
      } else {
        setSortKey(key);
        setSortDir(key === "createdAtUtc" || key === "stock" ? "desc" : "asc");
      }
    },
    [sortKey],
  );

  const filtersApplied =
    brandFilter !== null || categoryFilter !== null || activeFilter !== null;
  const searchActive = debouncedSearch.length > 0 || filtersApplied;

  return (
    <div className="space-y-7 pb-12">
      <ListHero
        eyebrow="Catalog · Inventory"
        tenant={user?.tenant ?? "—"}
        subEyebrow="stockable items"
        title="Products"
        totalCount={data?.totalCount ?? null}
        subtitle="The stockable items customers see. Each product carries a SKU, a brand, a category, a price, and a live stock count."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find a product by name, SKU, or slug…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="New product"
        onCreate={() => setEditor({ mode: "create" })}
      />

      {stats && data && data.totalCount > 0 && (
        <StatStrip cols={4}>
          <Stat label="Total products" value={pad2(stats.total)} hint="across this tenant" />
          <Stat
            label="Active"
            value={pad2(stats.active)}
            hint={`${stats.total === 0 ? 0 : Math.round((stats.active / stats.total) * 100)}% of the page`}
            accent
          />
          <Stat
            label="Stock health"
            value={
              stats.outOfStock > 0
                ? `${pad2(stats.outOfStock)} OOS`
                : stats.lowStock > 0
                  ? `${pad2(stats.lowStock)} LOW`
                  : "Healthy"
            }
            hint={
              stats.outOfStock > 0
                ? `${stats.outOfStock} item${stats.outOfStock === 1 ? "" : "s"} out of stock`
                : stats.lowStock > 0
                  ? `${stats.lowStock} below ${LOW_STOCK} units`
                  : "every item above the floor"
            }
            tone={stats.outOfStock > 0 ? "danger" : stats.lowStock > 0 ? "warning" : "default"}
          />
          <Stat
            label="Avg price"
            value={stats.avgPrice === 0 ? "—" : formatMoney(stats.avgPrice, stats.currency)}
            hint="across this page"
          />
        </StatStrip>
      )}

      <FilterBar
        brands={brandsQuery.data?.items ?? []}
        categories={categoriesQuery.data?.items ?? []}
        brandFilter={brandFilter}
        setBrandFilter={setBrandFilter}
        categoryFilter={categoryFilter}
        setCategoryFilter={setCategoryFilter}
        activeFilter={activeFilter}
        setActiveFilter={setActiveFilter}
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section className="fsh-enter fsh-enter-3 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <SortChips
            options={SORT_OPTIONS}
            sortKey={sortKey}
            sortDir={sortDir}
            onSort={onSort}
            prefixLabel={searchActive ? "results" : "registry"}
          />
          <DensityToggle density={density} onChange={setDensity} />
        </div>

        <div
          className={cn(
            "card-shell overflow-hidden rounded-2xl",
            "bg-[var(--color-surface-3)]",
          )}
        >
          {query.isLoading && items.length === 0 ? (
            <ul aria-busy>
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonRow key={i} delayMs={i * 40} density={density} />
              ))}
            </ul>
          ) : sortedItems.length === 0 ? (
            (() => {
              const filtered = debouncedSearch.length > 0 || filtersApplied;
              return (
                <EmptyState
                  eyebrow={filtered ? "No matches" : "Empty inventory"}
                  headline={
                    filtered
                      ? debouncedSearch
                        ? `Nothing matches "${debouncedSearch}".`
                        : "No products match the current filters."
                      : "No products on file yet."
                  }
                  body={
                    filtered
                      ? "Try widening the filter set, or clear everything and start fresh."
                      : "Add a product to start selling. Each carries its own SKU, price, stock count, and image."
                  }
                  icon={
                    filtered ? (
                      <Search className="h-6 w-6 text-[var(--color-primary)]" />
                    ) : (
                      <PackageX className="h-6 w-6 text-[var(--color-primary)]" />
                    )
                  }
                  primaryAction={{
                    label: filtered ? "Add a new product" : "Add the first product",
                    onClick: () => setEditor({ mode: "create" }),
                    icon: <Sparkles className="h-3.5 w-3.5" />,
                  }}
                  secondaryAction={
                    filtered
                      ? {
                          label: "Clear filters",
                          onClick: () => {
                            setSearch("");
                            setBrandFilter(null);
                            setCategoryFilter(null);
                            setActiveFilter(null);
                          },
                          icon: <X className="h-3.5 w-3.5" />,
                        }
                      : undefined
                  }
                />
              );
            })()
          ) : (
            <ul>
              {sortedItems.map((product, i) => (
                <Row
                  key={product.id}
                  product={product}
                  brand={brandsById.get(product.brandId)}
                  category={categoriesById.get(product.categoryId)}
                  density={density}
                  delayMs={Math.min(i, 8) * 30}
                  onEdit={() => setEditor({ mode: "edit", product })}
                  onDelete={() => setEditor({ mode: "delete", product })}
                  onPriceChange={() => setEditor({ mode: "price", product })}
                  onStockAdjust={() => setEditor({ mode: "stock", product })}
                />
              ))}
            </ul>
          )}
        </div>
      </section>

      {data && data.totalCount > 0 && (
        <Pagination
          page={data.pageNumber}
          totalPages={Math.max(data.totalPages, 1)}
          totalCount={data.totalCount}
          shown={items.length}
          fetching={query.isFetching}
          onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
          onNext={() => setPageNumber((p) => p + 1)}
          hasPrev={data.hasPrevious}
          hasNext={data.hasNext}
        />
      )}

      <ProductEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        brands={brandsQuery.data?.items ?? []}
        categories={categoriesQuery.data?.items ?? []}
      />
      <PriceDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <StockDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteProductDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Filter bar — pill chip selects + active toggle
// ───────────────────────────────────────────────────────────────────────

function FilterBar({
  brands,
  categories,
  brandFilter,
  setBrandFilter,
  categoryFilter,
  setCategoryFilter,
  activeFilter,
  setActiveFilter,
}: {
  brands: BrandDto[];
  categories: CategoryDto[];
  brandFilter: string | null;
  setBrandFilter: (v: string | null) => void;
  categoryFilter: string | null;
  setCategoryFilter: (v: string | null) => void;
  activeFilter: boolean | null;
  setActiveFilter: (v: boolean | null) => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-2 flex flex-wrap items-center gap-2">
      <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
        filter
      </span>
      <Combobox
        label="Brand"
        value={brandFilter}
        onChange={setBrandFilter}
        options={brands.map((b) => ({ value: b.id, label: b.name }))}
        variant="filter"
        searchable
        clearable
      />
      <Combobox
        label="Category"
        value={categoryFilter}
        onChange={setCategoryFilter}
        options={categories.map((c) => ({ value: c.id, label: c.name }))}
        variant="filter"
        searchable
        clearable
      />
      <ActiveFilter value={activeFilter} onChange={setActiveFilter} />
    </div>
  );
}

function ActiveFilter({
  value,
  onChange,
}: {
  value: boolean | null;
  onChange: (v: boolean | null) => void;
}) {
  return (
    <div
      role="group"
      aria-label="Active filter"
      className="surface-edge inline-flex h-7 items-center rounded-full bg-[var(--color-surface-3)] p-0.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.14em]"
    >
      {[
        { v: null, label: "All" },
        { v: true, label: "Active" },
        { v: false, label: "Hidden" },
      ].map((opt) => {
        const isActive = value === opt.v;
        return (
          <button
            key={String(opt.v)}
            type="button"
            onClick={() => onChange(opt.v)}
            aria-pressed={isActive}
            className={cn(
              "h-6 cursor-pointer rounded-full px-2.5 transition-colors duration-[var(--duration-fast)]",
              isActive
                ? "bg-[var(--color-surface-1)] text-[var(--color-foreground)] shadow-[var(--highlight-top),0_1px_2px_oklch(0.115_0.010_270/0.06)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row
// ───────────────────────────────────────────────────────────────────────

function Row({
  product,
  brand,
  category,
  density,
  delayMs,
  onEdit,
  onDelete,
  onPriceChange,
  onStockAdjust,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
  density: Density;
  delayMs: number;
  onEdit: () => void;
  onDelete: () => void;
  onPriceChange: () => void;
  onStockAdjust: () => void;
}) {
  const padY = density === "compact" ? "py-3" : "py-4";

  return (
    <li
      className={cn(
        "fsh-enter group/row relative flex items-center gap-4 border-b border-[var(--color-border)] px-5 last:border-b-0 sm:px-6",
        padY,
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "hover:bg-[var(--color-surface-4)]",
        !product.isActive && "opacity-75",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <span
        aria-hidden
        className={cn(
          "absolute inset-y-2.5 left-0 w-[2px] rounded-r-full bg-[var(--color-primary)]",
          "opacity-0 transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "group-hover/row:opacity-100 group-focus-within/row:opacity-100",
        )}
      />

      <ProductImage
        imageUrl={product.thumbnailUrl}
        initial={product.name.trim().charAt(0).toUpperCase() || "·"}
        size={density === "compact" ? 40 : 56}
      />

      <div className="flex min-w-0 flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.6]">
          <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <Link
              to={`/catalog/products/${product.id}`}
              className={cn(
                "text-display truncate text-[15.5px] font-semibold leading-tight tracking-[-0.01em] sm:text-[16px]",
                "decoration-[var(--color-border-strong)] decoration-1 underline-offset-[5px]",
                "transition-colors duration-[var(--duration-fast)]",
                "hover:text-[var(--color-primary)] hover:underline focus-visible:underline",
                "focus-visible:outline-none focus-visible:text-[var(--color-primary)]",
              )}
            >
              {product.name}
            </Link>
            <code
              title={product.sku}
              className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]"
            >
              {product.sku}
            </code>
            {!product.isActive && (
              <span className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                hidden
              </span>
            )}
          </div>

          <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-0.5 truncate text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            {brand && (
              <span className="inline-flex items-center gap-1">
                <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">brand</span>
                <span className="truncate">{brand.name}</span>
              </span>
            )}
            {brand && category && <span className="opacity-40">·</span>}
            {category && (
              <span className="inline-flex items-center gap-1">
                <span className="font-mono text-[10px] uppercase tracking-[0.14em] opacity-70">in</span>
                <span className="truncate">{category.name}</span>
              </span>
            )}
            {(brand || category) && product.description && (
              <span className="opacity-40">·</span>
            )}
            {product.description && (
              <span className="truncate" title={product.description}>
                {product.description}
              </span>
            )}
          </div>
        </div>

        <div className="hidden items-center gap-2 sm:flex">
          <button
            type="button"
            onClick={onPriceChange}
            className="text-display group/p inline-flex cursor-pointer items-baseline gap-1 rounded-md px-2 py-1 text-[15px] font-semibold tabular-nums tracking-[-0.01em] transition-colors hover:bg-[var(--color-muted)]"
            title="Change price"
          >
            {formatMoney(product.price.amount, product.price.currency)}
          </button>
          <StockBadge stock={product.stock} onClick={onStockAdjust} />
        </div>

        <div className="hidden min-w-[110px] text-right tabular-nums lg:block">
          <div className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-foreground)]/85">
            {formatDateMono(product.createdAtUtc)}
          </div>
          <div className="mt-0.5 text-[10.5px] text-[var(--color-muted-foreground)]">
            {formatRelative(product.createdAtUtc)}
          </div>
        </div>
      </div>

      <div
        className={cn(
          "flex items-center gap-1",
          "opacity-0 transition-opacity duration-[var(--duration-fast)]",
          "group-hover/row:opacity-100 group-focus-within/row:opacity-100",
        )}
      >
        <RowAction label={`Edit ${product.name}`} onClick={onEdit}>
          <Pencil className="h-3.5 w-3.5" />
        </RowAction>
        <RowAction label={`Delete ${product.name}`} onClick={onDelete} tone="danger">
          <Trash2 className="h-3.5 w-3.5" />
        </RowAction>
      </div>
    </li>
  );
}

function StockBadge({ stock, onClick }: { stock: number; onClick: () => void }) {
  const tone =
    stock === 0 ? "danger" : stock < LOW_STOCK ? "warning" : "default";
  const tones = {
    default:
      "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-surface-1)]",
    warning:
      "bg-[oklch(from_var(--color-warning)_l_c_h_/_0.14)] text-[var(--color-warning)] hover:bg-[oklch(from_var(--color-warning)_l_c_h_/_0.22)]",
    danger:
      "bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.14)] text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.22)]",
  } as const;
  return (
    <button
      type="button"
      onClick={onClick}
      title="Adjust stock"
      className={cn(
        "inline-flex h-7 cursor-pointer items-center gap-1 rounded-full px-2.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.14em] tabular-nums transition-colors",
        tones[tone],
      )}
    >
      <Package className="h-3 w-3" />
      {stock}
    </button>
  );
}

function RowAction({
  label,
  onClick,
  tone = "default",
  children,
}: {
  label: string;
  onClick: () => void;
  tone?: "default" | "danger";
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      onClick={onClick}
      className={cn(
        "grid h-8 w-8 cursor-pointer place-items-center rounded-md transition-colors duration-[var(--duration-fast)]",
        "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]",
        tone === "danger"
          ? "hover:text-[var(--color-destructive)]"
          : "hover:text-[var(--color-foreground)]",
      )}
    >
      {children}
    </button>
  );
}

function ProductImage({
  imageUrl,
  initial,
  size,
}: {
  imageUrl: string | null | undefined;
  initial: string;
  size: number;
}) {
  const style = { width: size, height: size };
  if (imageUrl) {
    return (
      <span
        style={style}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
          "bg-[var(--color-surface-2)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img
          src={imageUrl}
          alt=""
          className="h-full w-full object-cover"
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={(e) => {
            const target = e.currentTarget;
            target.style.display = "none";
            target.parentElement
              ?.querySelector<HTMLElement>("[data-fallback]")
              ?.style.removeProperty("display");
          }}
        />
        <span
          data-fallback
          style={{ display: "none" }}
          className="absolute inset-0 grid place-items-center text-[16px] font-semibold tracking-tight text-[var(--color-muted-foreground)]"
        >
          {initial}
        </span>
      </span>
    );
  }
  return (
    <span
      aria-hidden
      style={style}
      className={cn(
        "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
        "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))]",
        "ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.25)]",
        "shadow-[var(--highlight-top)]",
      )}
    >
      <span
        aria-hidden
        className="absolute -right-3 -top-3 h-8 w-8 rounded-full bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] blur-md"
      />
      <Package
        className={cn(
          "relative text-[var(--color-primary)]",
          size >= 48 ? "h-6 w-6" : "h-5 w-5",
        )}
      />
    </span>
  );
}

function SkeletonRow({ delayMs, density }: { delayMs: number; density: Density }) {
  const padY = density === "compact" ? "py-3" : "py-4";
  return (
    <li
      className={cn(
        "fsh-enter flex items-center gap-4 border-b border-[var(--color-border)] px-5 last:border-b-0 sm:px-6",
        padY,
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <Skeleton className={cn("rounded-xl", density === "compact" ? "h-10 w-10" : "h-14 w-14")} />
      <div className="flex flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.6] space-y-2">
          <Skeleton className="h-4 w-48" />
          <Skeleton className="h-3 w-72" />
        </div>
        <Skeleton className="hidden h-7 w-20 sm:block" />
        <Skeleton className="hidden h-7 w-14 rounded-full sm:block" />
        <div className="hidden min-w-[110px] space-y-1.5 text-right lg:block">
          <Skeleton className="ml-auto h-3 w-24" />
          <Skeleton className="ml-auto h-2.5 w-16" />
        </div>
      </div>
      <Skeleton className="h-7 w-16" />
    </li>
  );
}


// ───────────────────────────────────────────────────────────────────────
//  Editor dialog (create + edit)
// ───────────────────────────────────────────────────────────────────────

function ProductEditorDialog({
  state,
  onClose,
  brands,
  categories,
}: {
  state: EditorState;
  onClose: () => void;
  brands: BrandDto[];
  categories: CategoryDto[];
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const product = state.mode === "edit" ? state.product : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      sku: product?.sku ?? "",
      name: product?.name ?? "",
      description: product?.description ?? "",
      brandId: product?.brandId ?? "",
      categoryId: product?.categoryId ?? "",
      priceAmount: product?.price.amount ?? 0,
      priceCurrency: product?.price.currency ?? "USD",
      stock: product?.stock ?? 0,
      isActive: product?.isActive ?? true,
    }),
    [product],
  );

  const [sku, setSku] = useState(initial.sku);
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [brandId, setBrandId] = useState(initial.brandId);
  const [categoryId, setCategoryId] = useState(initial.categoryId);
  const [priceAmount, setPriceAmount] = useState(String(initial.priceAmount));
  const [priceCurrency, setPriceCurrency] = useState(initial.priceCurrency);
  const [stock, setStock] = useState(String(initial.stock));
  const [isActive, setIsActive] = useState(initial.isActive);

  useEffect(() => {
    if (isOpen) {
      setSku(initial.sku);
      setName(initial.name);
      setDescription(initial.description);
      setBrandId(initial.brandId);
      setCategoryId(initial.categoryId);
      setPriceAmount(String(initial.priceAmount));
      setPriceCurrency(initial.priceCurrency);
      setStock(String(initial.stock));
      setIsActive(initial.isActive);
    }
  }, [isOpen, initial]);

  const createMutation = useMutation({
    mutationFn: (input: CreateProductInput) => createProduct(input),
    onSuccess: () => {
      toast.success("Product created");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProductInput) => updateProduct(input),
    onSuccess: () => {
      toast.success("Product updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();
  const trimmedSku = sku.trim();
  const validBrand = brandId !== "";
  const validCategory = categoryId !== "";
  const priceNum = Number.parseFloat(priceAmount);
  const stockNum = Number.parseInt(stock, 10);
  const valid =
    trimmedName.length > 0 &&
    (product || trimmedSku.length > 0) &&
    validBrand &&
    validCategory &&
    !Number.isNaN(priceNum) &&
    priceNum >= 0 &&
    !Number.isNaN(stockNum) &&
    stockNum >= 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    if (state.mode === "edit" && product) {
      updateMutation.mutate({
        productId: product.id,
        name: trimmedName,
        description: description.trim() || null,
        brandId,
        categoryId,
        isActive,
      });
    } else {
      createMutation.mutate({
        sku: trimmedSku,
        name: trimmedName,
        description: description.trim() || null,
        brandId,
        categoryId,
        priceAmount: priceNum,
        priceCurrency,
        stock: stockNum,
      });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {product ? "Edit entry" : "New entry"}
            </span>
            <DialogTitle>{product ? "Edit product" : "Add a product"}</DialogTitle>
            <DialogDescription>
              {product
                ? `Update details for ${product.name}. Use the inline price/stock chips on the row to change those — they emit domain events.`
                : "Add a product to your catalog. Price and stock can be adjusted inline after creation."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <RowPreview
              name={trimmedName}
              sku={trimmedSku}
              description={description}
              imageUrl={product?.thumbnailUrl ?? null}
              brandName={brands.find((b) => b.id === brandId)?.name ?? null}
              categoryName={categories.find((c) => c.id === categoryId)?.name ?? null}
              priceAmount={Number.isNaN(priceNum) ? 0 : priceNum}
              priceCurrency={priceCurrency}
              stock={Number.isNaN(stockNum) ? 0 : stockNum}
              isActive={isActive}
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="product-name" label="Name" required>
                <Input
                  id="product-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Classic Cotton Tee"
                  autoFocus
                  required
                  maxLength={200}
                />
              </Field>
              <Field id="product-sku" label="SKU" required={!product} hint={product ? "SKU is fixed after creation." : "Stock-keeping unit. Becomes the canonical identifier."}>
                <Input
                  id="product-sku"
                  value={sku}
                  onChange={(e) => setSku(e.target.value.toUpperCase())}
                  placeholder="ACM-TS-001"
                  required={!product}
                  disabled={!!product}
                  maxLength={64}
                  className="font-mono text-[13px] tracking-tight"
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="product-brand" label="Brand" required>
                <Combobox
                  id="product-brand"
                  label="Brand"
                  placeholder="Select a brand…"
                  value={brandId || null}
                  onChange={(v) => setBrandId(v ?? "")}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))}
                  searchable
                  required
                />
              </Field>
              <Field id="product-category" label="Category" required>
                <Combobox
                  id="product-category"
                  label="Category"
                  placeholder="Select a category…"
                  value={categoryId || null}
                  onChange={(v) => setCategoryId(v ?? "")}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                  searchable
                  required
                />
              </Field>
            </div>

            {!product && (
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <Field id="product-price" label="Price" required>
                  <Input
                    id="product-price"
                    type="number"
                    inputMode="decimal"
                    step="0.01"
                    min="0"
                    value={priceAmount}
                    onChange={(e) => setPriceAmount(e.target.value)}
                    required
                    className="tabular-nums"
                  />
                </Field>
                <Field id="product-currency" label="Currency" required>
                  <Input
                    id="product-currency"
                    value={priceCurrency}
                    onChange={(e) => setPriceCurrency(e.target.value.toUpperCase().slice(0, 3))}
                    required
                    maxLength={3}
                    className="font-mono uppercase tracking-tight"
                  />
                </Field>
                <Field id="product-stock" label="Stock" required>
                  <Input
                    id="product-stock"
                    type="number"
                    inputMode="numeric"
                    step="1"
                    min="0"
                    value={stock}
                    onChange={(e) => setStock(e.target.value)}
                    required
                    className="tabular-nums"
                  />
                </Field>
              </div>
            )}

            {!product && (
              <p className="rounded-md border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-3 py-2 text-xs text-[var(--color-muted-foreground)]">
                Images can be uploaded on the product detail page after the product is created.
              </p>
            )}

            <Field id="product-description" label="Description" hint="Shown on listing and product detail pages.">
              <textarea
                id="product-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={4000}
                className={cn(
                  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                )}
                placeholder="100% organic cotton crew-neck."
              />
            </Field>

            {product && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] bg-[var(--color-surface-2)] px-4 py-3">
                <div>
                  <div className="font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                    Visibility
                  </div>
                  <div className="mt-1 text-[12.5px] text-[var(--color-muted-foreground)]">
                    {isActive ? "Listed for customers." : "Hidden from listings."}
                  </div>
                </div>
                <Switch checked={isActive} onCheckedChange={setIsActive} aria-label="Active" />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={isPending || !valid}
              className="brand-glow gradient-sheen"
            >
              {isPending ? "Saving…" : product ? "Save changes" : "Add product"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function RowPreview({
  name,
  sku,
  description,
  imageUrl,
  brandName,
  categoryName,
  priceAmount,
  priceCurrency,
  stock,
  isActive,
}: {
  name: string;
  sku: string;
  description: string;
  imageUrl: string | null;
  brandName: string | null;
  categoryName: string | null;
  priceAmount: number;
  priceCurrency: string;
  stock: number;
  isActive: boolean;
}) {
  return (
    <div
      aria-label="Live preview"
      className="surface-edge rounded-xl bg-[var(--color-surface-2)] p-3"
    >
      <div className="mb-2 flex items-center justify-between">
        <span className="font-mono text-[9.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
          Live preview · registry row
        </span>
      </div>
      <div className="flex items-center gap-3">
        <ProductImage
          imageUrl={imageUrl}
          initial={name.trim().charAt(0).toUpperCase() || "·"}
          size={48}
        />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <div
              className={cn(
                "truncate text-[15px] font-semibold leading-tight tracking-[-0.005em]",
                !name.trim() && "text-[var(--color-muted-foreground)]",
              )}
            >
              {name.trim() || "Product name"}
            </div>
            {sku && (
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]">
                {sku}
              </code>
            )}
            {!isActive && (
              <span className="inline-flex items-center gap-1 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                hidden
              </span>
            )}
          </div>
          <div className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">
            {brandName ? `Brand: ${brandName}` : "No brand"}
            {categoryName && ` · in ${categoryName}`}
          </div>
        </div>
        <div className="hidden text-right tabular-nums sm:block">
          <div className="text-display text-[14px] font-semibold leading-tight">
            {formatMoney(priceAmount, priceCurrency || "USD")}
          </div>
          <div
            className={cn(
              "mt-0.5 font-mono text-[10px] uppercase tracking-[0.14em]",
              stock === 0
                ? "text-[var(--color-destructive)]"
                : stock < LOW_STOCK
                  ? "text-[var(--color-warning)]"
                  : "text-[var(--color-muted-foreground)]",
            )}
          >
            {stock} in stock
          </div>
        </div>
      </div>
      {description.trim() && (
        <p className="mt-2 line-clamp-2 text-[12px] leading-relaxed text-[var(--color-muted-foreground)]">
          {description.trim()}
        </p>
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Price / Stock dialogs
// ───────────────────────────────────────────────────────────────────────

function PriceDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "price";
  const product = state.mode === "price" ? state.product : undefined;
  const queryClient = useQueryClient();

  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState("USD");

  useEffect(() => {
    if (isOpen && product) {
      setAmount(String(product.price.amount));
      setCurrency(product.price.currency);
    }
  }, [isOpen, product]);

  const mutation = useMutation({
    mutationFn: (input: ChangeProductPriceInput) => changeProductPrice(input),
    onSuccess: () => {
      toast.success("Price updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error("Price change failed", { description: describe(err) }),
  });

  const newAmount = Number.parseFloat(amount);
  const valid = !Number.isNaN(newAmount) && newAmount >= 0 && currency.length === 3;
  const oldAmount = product?.price.amount ?? 0;
  const delta = !Number.isNaN(newAmount) ? newAmount - oldAmount : 0;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid || !product) return;
            mutation.mutate({ productId: product.id, amount: newAmount, currency });
          }}
        >
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              <CircleDollarSign className="mr-1 inline h-3 w-3" />
              Price change
            </span>
            <DialogTitle>{product?.name}</DialogTitle>
            <DialogDescription>
              Emits a <code className="font-mono text-[11px]">ProductPriceChanged</code> domain event.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="surface-edge flex items-center justify-between rounded-xl bg-[var(--color-surface-2)] px-4 py-3">
              <div>
                <div className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                  was
                </div>
                <div className="text-display mt-1 text-[18px] font-semibold tabular-nums">
                  {product && formatMoney(product.price.amount, product.price.currency)}
                </div>
              </div>
              <ArrowDown className="h-4 w-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-primary)]">
                  becomes
                </div>
                <div
                  className={cn(
                    "text-display mt-1 text-[18px] font-semibold tabular-nums",
                    delta > 0
                      ? "text-[var(--color-success)]"
                      : delta < 0
                        ? "text-[var(--color-destructive)]"
                        : "",
                  )}
                >
                  {!Number.isNaN(newAmount)
                    ? formatMoney(newAmount, currency || "USD")
                    : "—"}
                </div>
              </div>
            </div>
            <div className="grid grid-cols-[1fr_auto] gap-3">
              <Field id="price-amount" label="New amount" required>
                <Input
                  id="price-amount"
                  type="number"
                  step="0.01"
                  min="0"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                  className="tabular-nums"
                  autoFocus
                />
              </Field>
              <Field id="price-currency" label="Currency" required>
                <Input
                  id="price-currency"
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value.toUpperCase().slice(0, 3))}
                  required
                  maxLength={3}
                  className="w-20 font-mono uppercase tracking-tight"
                />
              </Field>
            </div>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !valid}
              className="brand-glow gradient-sheen"
            >
              {mutation.isPending ? "Saving…" : "Change price"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function StockDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "stock";
  const product = state.mode === "stock" ? state.product : undefined;
  const queryClient = useQueryClient();

  const [delta, setDelta] = useState("0");

  useEffect(() => {
    if (isOpen) setDelta("0");
  }, [isOpen]);

  const mutation = useMutation({
    mutationFn: (input: AdjustProductStockInput) => adjustProductStock(input),
    onSuccess: () => {
      toast.success("Stock adjusted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error("Adjustment failed", { description: describe(err) }),
  });

  const deltaNum = Number.parseInt(delta, 10);
  const valid = !Number.isNaN(deltaNum) && deltaNum !== 0;
  const newStock = (product?.stock ?? 0) + (Number.isNaN(deltaNum) ? 0 : deltaNum);
  const willGoNegative = newStock < 0;

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid || willGoNegative || !product) return;
            mutation.mutate({ productId: product.id, delta: deltaNum });
          }}
        >
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              <Package className="mr-1 inline h-3 w-3" />
              Stock adjustment
            </span>
            <DialogTitle>{product?.name}</DialogTitle>
            <DialogDescription>
              Add or remove units. Emits a{" "}
              <code className="font-mono text-[11px]">ProductStockAdjusted</code> event.
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="surface-edge flex items-center justify-between rounded-xl bg-[var(--color-surface-2)] px-4 py-3 tabular-nums">
              <div>
                <div className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
                  current
                </div>
                <div className="text-display mt-1 text-[18px] font-semibold">
                  {product?.stock ?? 0}
                </div>
              </div>
              <ArrowDown className="h-4 w-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-primary)]">
                  becomes
                </div>
                <div
                  className={cn(
                    "text-display mt-1 text-[18px] font-semibold",
                    willGoNegative
                      ? "text-[var(--color-destructive)]"
                      : deltaNum > 0
                        ? "text-[var(--color-success)]"
                        : deltaNum < 0
                          ? "text-[var(--color-warning)]"
                          : "",
                  )}
                >
                  {newStock}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setDelta(String((Number.parseInt(delta, 10) || 0) - 1))}
              >
                <Minus className="h-3.5 w-3.5" />
              </Button>
              <Input
                value={delta}
                onChange={(e) => setDelta(e.target.value)}
                type="number"
                step="1"
                className="text-center font-mono text-[15px] tabular-nums"
                aria-label="Delta"
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setDelta(String((Number.parseInt(delta, 10) || 0) + 1))}
              >
                <Plus className="h-3.5 w-3.5" />
              </Button>
            </div>

            {willGoNegative && (
              <div className="flex items-start gap-2 rounded-md bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] px-3 py-2 text-[12.5px] text-[var(--color-destructive)]">
                <AlertTriangle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                <span>
                  Stock cannot go negative. The maximum decrement here is{" "}
                  <span className="font-mono">{product?.stock ?? 0}</span>.
                </span>
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !valid || willGoNegative}
              className="brand-glow gradient-sheen"
            >
              {mutation.isPending ? "Adjusting…" : "Adjust stock"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Delete confirmation
// ───────────────────────────────────────────────────────────────────────

function DeleteProductDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "delete";
  const product = state.mode === "delete" ? state.product : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    onSuccess: () => {
      toast.success("Product deleted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
            Permanent action
          </span>
          <DialogTitle>Delete product</DialogTitle>
          <DialogDescription>
            This permanently removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{product?.name}</span>{" "}
            <span className="opacity-70">
              ({product && formatDate(product.createdAtUtc)})
            </span>
            . The product will no longer appear in any listing or report.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => product && deleteMutation.mutate(product.id)}
            disabled={deleteMutation.isPending || !product}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete product"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
