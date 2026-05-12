import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowDown,
  ArrowLeft,
  ChevronRight,
  CircleDollarSign,
  EyeOff,
  Minus,
  Package,
  PackageX,
  Pencil,
  Plus,
  RefreshCw,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import {
  adjustProductStock,
  changeProductPrice,
  deleteProduct,
  getBrandById,
  getCategoryById,
  getProductById,
  searchBrands,
  searchCategories,
  updateProduct,
  type AdjustProductStockInput,
  type BrandDto,
  type CategoryDto,
  type ChangeProductPriceInput,
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
  ErrorBand,
  Field,
} from "@/components/list";
import { ProductImageManager } from "@/components/file/product-image-manager";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatDateMono,
  formatMoney,
  formatRelative,
} from "@/lib/list-helpers";

const LOW_STOCK = 10;

type DialogState =
  | { mode: "closed" }
  | { mode: "edit" }
  | { mode: "delete" }
  | { mode: "price" }
  | { mode: "stock" };

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function ProductDetailPage() {
  const { productId = "" } = useParams<{ productId: string }>();
  const navigate = useNavigate();
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });

  const productQuery = useQuery({
    queryKey: ["catalog", "products", productId],
    queryFn: () => getProductById(productId),
    enabled: !!productId,
  });

  const product = productQuery.data;

  const brandQuery = useQuery({
    queryKey: ["catalog", "brands", product?.brandId ?? "none"],
    queryFn: () => getBrandById(product!.brandId),
    enabled: !!product?.brandId,
    staleTime: 60_000,
  });

  const categoryQuery = useQuery({
    queryKey: ["catalog", "categories", product?.categoryId ?? "none"],
    queryFn: () => getCategoryById(product!.categoryId),
    enabled: !!product?.categoryId,
    staleTime: 60_000,
  });

  return (
    <div className="space-y-6 pb-12">
      <Breadcrumb
        productName={product?.name}
        onBack={() => navigate("/catalog/products")}
      />

      {productQuery.isError && (
        <ErrorBand message={describe(productQuery.error)} />
      )}

      {productQuery.isLoading ? (
        <DetailSkeleton />
      ) : product ? (
        <>
          <Hero
            product={product}
            brand={brandQuery.data}
            category={categoryQuery.data}
            isFetching={productQuery.isFetching}
            onRefresh={() => void productQuery.refetch()}
            onEdit={() => setDialog({ mode: "edit" })}
            onDelete={() => setDialog({ mode: "delete" })}
            onPriceChange={() => setDialog({ mode: "price" })}
            onStockAdjust={() => setDialog({ mode: "stock" })}
          />

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_320px]">
            <DescriptionPanel product={product} />
            <MetadataPanel product={product} brand={brandQuery.data} category={categoryQuery.data} />
          </div>

          <section
            aria-label="Product images"
            className="fsh-enter fsh-enter-3 card-shell space-y-4 rounded-2xl bg-[var(--color-surface-2)] p-5"
          >
            <header className="flex items-baseline justify-between gap-3">
              <h2 className="text-display text-lg font-semibold tracking-tight">Images</h2>
              <span className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Drop more to add · star to set cover
              </span>
            </header>
            <ProductImageManager
              productId={product.id}
              images={product.images}
              invalidateKey={["catalog", "products", productId]}
            />
          </section>

          <ProductEditorDialog
            open={dialog.mode === "edit"}
            product={product}
            onClose={() => setDialog({ mode: "closed" })}
          />
          <DeleteDialog
            open={dialog.mode === "delete"}
            product={product}
            onClose={() => setDialog({ mode: "closed" })}
            onDeleted={() => navigate("/catalog/products")}
          />
          <PriceDialog
            open={dialog.mode === "price"}
            product={product}
            onClose={() => setDialog({ mode: "closed" })}
          />
          <StockDialog
            open={dialog.mode === "stock"}
            product={product}
            onClose={() => setDialog({ mode: "closed" })}
          />
        </>
      ) : (
        <NotFoundPanel />
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Breadcrumb
// ───────────────────────────────────────────────────────────────────────

function Breadcrumb({
  productName,
  onBack,
}: {
  productName: string | undefined;
  onBack: () => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-1 flex items-center justify-between gap-3">
      <nav
        aria-label="Breadcrumb"
        className="flex flex-wrap items-center gap-1.5 font-mono text-[10.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]"
      >
        <Link
          to="/catalog/products"
          className="rounded px-1.5 py-0.5 transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          Catalog
        </Link>
        <ChevronRight className="h-3 w-3 opacity-60" />
        <Link
          to="/catalog/products"
          className="rounded px-1.5 py-0.5 transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          Products
        </Link>
        <ChevronRight className="h-3 w-3 opacity-60" />
        <span className="rounded px-1.5 py-0.5 text-[var(--color-foreground)]">
          {productName ?? "…"}
        </span>
      </nav>

      <Button variant="outline" size="sm" onClick={onBack} className="gap-1.5">
        <ArrowLeft className="h-3.5 w-3.5" />
        <span className="hidden sm:inline">Back to products</span>
      </Button>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Hero — atmospheric showroom with image left, identity right
// ───────────────────────────────────────────────────────────────────────

function Hero({
  product,
  brand,
  category,
  isFetching,
  onRefresh,
  onEdit,
  onDelete,
  onPriceChange,
  onStockAdjust,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
  isFetching: boolean;
  onRefresh: () => void;
  onEdit: () => void;
  onDelete: () => void;
  onPriceChange: () => void;
  onStockAdjust: () => void;
}) {
  const stockTone =
    product.stock === 0 ? "danger" : product.stock < LOW_STOCK ? "warning" : "default";

  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-2 card-shell relative overflow-hidden rounded-[20px]",
        "bg-[var(--color-surface-3)]",
      )}
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: `
            radial-gradient(60% 70% at 0% 0%, oklch(from var(--color-primary) l c h / 0.18), transparent 60%),
            radial-gradient(50% 60% at 100% 0%, oklch(0.700 0.155 195 / 0.10), transparent 65%),
            radial-gradient(80% 80% at 100% 100%, oklch(from var(--color-primary) l c h / 0.05), transparent 70%)
          `,
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.06] mix-blend-overlay"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%' height='100%' filter='url(%23n)'/></svg>\")",
        }}
      />

      <div className="relative grid grid-cols-1 gap-6 p-6 md:grid-cols-[minmax(260px,360px)_1fr] md:p-8 lg:p-10">
        {/* Showroom image */}
        <div className="relative">
          <ProductShowcase
            imageUrl={product.thumbnailUrl}
            initial={product.name.trim().charAt(0).toUpperCase() || "·"}
          />
          {!product.isActive && (
            <span className="absolute left-3 top-3 inline-flex items-center gap-1.5 rounded-full bg-[oklch(from_var(--color-foreground)_l_c_h_/_0.85)] px-2.5 py-1 font-mono text-[10px] font-medium uppercase tracking-[0.16em] text-[var(--color-background)]">
              <EyeOff className="h-3 w-3" />
              hidden
            </span>
          )}
        </div>

        {/* Identity stack */}
        <div className="flex min-w-0 flex-col gap-5">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex flex-wrap items-center gap-x-2 gap-y-1.5">
              <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Catalog · Item
              </span>
              <span aria-hidden className="h-px w-6 bg-[var(--color-border-strong)]" />
              <code className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight text-[var(--color-foreground)]">
                {product.sku}
              </code>
            </div>
            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="sm"
                disabled={isFetching}
                onClick={onRefresh}
                className="gap-1.5"
              >
                <RefreshCw className={cn("h-3.5 w-3.5", isFetching && "animate-spin")} />
                <span className="hidden sm:inline">Refresh</span>
              </Button>
              <Button variant="outline" size="sm" onClick={onEdit} className="gap-1.5">
                <Pencil className="h-3.5 w-3.5" />
                Edit
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={onDelete}
                className="gap-1.5 hover:!text-[var(--color-destructive)] hover:!border-[var(--color-destructive)]"
              >
                <Trash2 className="h-3.5 w-3.5" />
                <span className="hidden sm:inline">Delete</span>
              </Button>
            </div>
          </div>

          {/* Display name */}
          <div>
            <h1 className="text-display text-[36px] font-semibold leading-[1.05] tracking-[-0.025em] sm:text-[40px]">
              {product.name}
            </h1>
            <div className="mt-2 flex flex-wrap items-center gap-2 text-[12.5px] text-[var(--color-muted-foreground)]">
              {brand && (
                <ChipLink
                  label="Brand"
                  to={`/catalog/products?brand=${brand.id}`}
                  value={brand.name}
                  swatchUrl={brand.logoUrl}
                />
              )}
              {category && (
                <ChipLink
                  label="In"
                  to={`/catalog/products?category=${category.id}`}
                  value={category.name}
                />
              )}
            </div>
          </div>

          {/* Price + stock — the two click-to-mutate anchors. */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <button
              type="button"
              onClick={onPriceChange}
              title="Change price"
              className={cn(
                "group/p card-shell card-shell-interactive relative flex h-full min-h-[112px] cursor-pointer flex-col justify-between rounded-xl bg-[var(--color-surface-2)] p-4 text-left",
                "transition-colors duration-[var(--duration-fast)]",
                "hover:bg-[var(--color-surface-4)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
              )}
            >
              <div className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Price
              </div>
              <div className="text-display mt-1.5 text-[28px] font-semibold leading-none tracking-[-0.02em] tabular-nums">
                {formatMoney(product.price.amount, product.price.currency)}
              </div>
              <div className="mt-1.5 flex items-center gap-1 text-[11px] text-[var(--color-muted-foreground)]">
                <CircleDollarSign className="h-3 w-3" />
                <span className="opacity-70 transition-colors group-hover/p:text-[var(--color-primary)] group-hover/p:opacity-100">
                  Click to change
                </span>
              </div>
            </button>

            <button
              type="button"
              onClick={onStockAdjust}
              title="Adjust stock"
              className={cn(
                "group/s card-shell card-shell-interactive relative flex h-full min-h-[112px] cursor-pointer flex-col justify-between rounded-xl bg-[var(--color-surface-2)] p-4 text-left",
                "transition-colors duration-[var(--duration-fast)]",
                "hover:bg-[var(--color-surface-4)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
              )}
            >
              <div className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                Stock
              </div>
              <div
                className={cn(
                  "text-display mt-1.5 text-[28px] font-semibold leading-none tracking-[-0.02em] tabular-nums",
                  stockTone === "danger" && "text-[var(--color-destructive)]",
                  stockTone === "warning" && "text-[var(--color-warning)]",
                )}
              >
                {product.stock}
              </div>
              <div className="mt-1.5 flex items-center gap-1 text-[11px] text-[var(--color-muted-foreground)]">
                {stockTone === "danger" ? (
                  <>
                    <AlertTriangle className="h-3 w-3 text-[var(--color-destructive)]" />
                    <span className="text-[var(--color-destructive)]">
                      Out of stock
                    </span>
                  </>
                ) : stockTone === "warning" ? (
                  <>
                    <AlertTriangle className="h-3 w-3 text-[var(--color-warning)]" />
                    <span className="text-[var(--color-warning)]">
                      Below {LOW_STOCK} units
                    </span>
                  </>
                ) : (
                  <>
                    <Package className="h-3 w-3" />
                    <span className="opacity-70 transition-colors group-hover/s:text-[var(--color-primary)] group-hover/s:opacity-100">
                      Click to adjust
                    </span>
                  </>
                )}
              </div>
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}

function ProductShowcase({
  imageUrl,
  initial,
}: {
  imageUrl: string | null | undefined;
  initial: string;
}) {
  if (imageUrl) {
    return (
      <div
        className={cn(
          "card-shell relative aspect-square overflow-hidden rounded-2xl",
          "bg-[var(--color-surface-2)]",
        )}
      >
        <img
          src={imageUrl}
          alt=""
          loading="eager"
          referrerPolicy="no-referrer"
          className="h-full w-full object-cover"
          onError={(e) => {
            const target = e.currentTarget;
            target.style.display = "none";
            target.parentElement
              ?.querySelector<HTMLElement>("[data-fallback]")
              ?.style.removeProperty("display");
          }}
        />
        <div
          data-fallback
          style={{ display: "none" }}
          className="absolute inset-0 grid place-items-center"
        >
          <FallbackArtwork initial={initial} />
        </div>
      </div>
    );
  }
  return (
    <div
      className={cn(
        "surface-edge gradient-border relative aspect-square overflow-hidden rounded-2xl",
        "grid place-items-center",
        "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.16),oklch(from_var(--color-primary)_l_c_h_/_0.02))]",
      )}
    >
      <FallbackArtwork initial={initial} />
    </div>
  );
}

function FallbackArtwork({ initial }: { initial: string }) {
  return (
    <div className="relative">
      {/* Soft diffused glow behind the glyph. */}
      <span
        aria-hidden
        className="absolute -inset-10 rounded-full bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] blur-3xl"
      />
      <Package
        className="relative h-20 w-20 text-[var(--color-primary)] opacity-50"
        aria-hidden
      />
      <span
        aria-hidden
        className="absolute inset-0 grid place-items-center text-display text-[64px] font-semibold leading-none tracking-[-0.04em] text-[var(--color-primary)]"
      >
        {initial}
      </span>
    </div>
  );
}

function ChipLink({
  label,
  to,
  value,
  swatchUrl,
}: {
  label: string;
  to: string;
  value: string;
  swatchUrl?: string | null;
}) {
  return (
    <Link
      to={to}
      className={cn(
        "group/chip inline-flex items-center gap-1.5 rounded-full bg-[var(--color-surface-2)] px-2.5 py-1",
        "ring-1 ring-inset ring-[var(--color-border)]",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-surface-4)] hover:ring-[var(--color-border-strong)]",
      )}
    >
      <span className="font-mono text-[9.5px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]/80">
        {label}
      </span>
      {swatchUrl && (
        <span className="grid h-4 w-4 place-items-center overflow-hidden rounded-sm bg-[var(--color-surface-1)] ring-1 ring-inset ring-[var(--color-border)]">
          <img src={swatchUrl} alt="" className="h-full w-full object-contain p-0.5" />
        </span>
      )}
      <span className="text-[12px] font-medium text-[var(--color-foreground)]">{value}</span>
      <ChevronRight className="h-3 w-3 opacity-50 transition-transform duration-[var(--duration-fast)] group-hover/chip:translate-x-0.5 group-hover/chip:opacity-90" />
    </Link>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Description + metadata panels
// ───────────────────────────────────────────────────────────────────────

function DescriptionPanel({ product }: { product: ProductDto }) {
  return (
    <section
      className={cn(
        "fsh-enter fsh-enter-3 card-shell rounded-2xl",
        "bg-[var(--color-surface-3)] p-6 md:p-7",
      )}
    >
      <div className="flex items-baseline gap-2.5">
        <h2 className="text-display text-[15px] font-semibold tracking-[-0.01em]">
          Description
        </h2>
        <span className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
          customer-facing
        </span>
      </div>
      <div className="mt-3">
        {product.description ? (
          <p className="whitespace-pre-wrap text-[14px] leading-relaxed text-[var(--color-foreground)]/90">
            {product.description}
          </p>
        ) : (
          <p className="italic text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
            No description on file. Customers will see a blank description on the
            product page until you add one.
          </p>
        )}
      </div>
    </section>
  );
}

function MetadataPanel({
  product,
  brand,
  category,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
}) {
  return (
    <aside
      className={cn(
        "fsh-enter fsh-enter-3 card-shell rounded-2xl",
        "bg-[var(--color-surface-3)] p-6",
      )}
    >
      <div className="font-mono text-[10px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Audit
      </div>
      <dl className="mt-3 space-y-3 text-[13px]">
        <Meta label="Created" value={formatDateMono(product.createdAtUtc)} hint={formatRelative(product.createdAtUtc)} />
        {product.updatedAtUtc ? (
          <Meta label="Revised" value={formatDateMono(product.updatedAtUtc)} hint={formatRelative(product.updatedAtUtc)} />
        ) : (
          <Meta label="Revised" value="Never" hint="no edits since creation" />
        )}
        <Meta
          label="Status"
          value={product.isActive ? "Active" : "Hidden"}
          tone={product.isActive ? "success" : "muted"}
        />
        <Meta label="Slug" value={<code className="font-mono text-[11.5px] tracking-tight">{product.slug}</code>} />
        <Meta label="Brand ID" value={<IdCode value={brand?.id ?? product.brandId} />} />
        <Meta label="Category ID" value={<IdCode value={category?.id ?? product.categoryId} />} />
        <Meta label="Product ID" value={<IdCode value={product.id} />} />
      </dl>
    </aside>
  );
}

function Meta({
  label,
  value,
  hint,
  tone = "default",
}: {
  label: string;
  value: React.ReactNode;
  hint?: string;
  tone?: "default" | "success" | "muted";
}) {
  return (
    <div className="grid grid-cols-[88px_1fr] items-baseline gap-3">
      <dt className="font-mono text-[10px] font-medium uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd
        className={cn(
          "min-w-0 text-[13px] tabular-nums",
          tone === "success" && "text-[var(--color-success)]",
          tone === "muted" && "text-[var(--color-muted-foreground)]",
        )}
      >
        <div className="truncate">{value}</div>
        {hint && (
          <div className="mt-0.5 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
            {hint}
          </div>
        )}
      </dd>
    </div>
  );
}

function IdCode({ value }: { value: string }) {
  return (
    <code
      title={value}
      className="block max-w-full truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]"
    >
      {value}
    </code>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Loading + not-found states
// ───────────────────────────────────────────────────────────────────────

function DetailSkeleton() {
  return (
    <>
      <div className="card-shell rounded-[20px] bg-[var(--color-surface-3)] p-6 md:p-8 lg:p-10">
        <div className="grid grid-cols-1 gap-6 md:grid-cols-[minmax(260px,360px)_1fr]">
          <Skeleton className="aspect-square rounded-2xl" />
          <div className="space-y-5">
            <div className="flex justify-between gap-3">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-8 w-40" />
            </div>
            <Skeleton className="h-10 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
            <div className="grid grid-cols-2 gap-3">
              <Skeleton className="h-24 rounded-xl" />
              <Skeleton className="h-24 rounded-xl" />
            </div>
          </div>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_320px]">
        <Skeleton className="h-32 rounded-2xl" />
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    </>
  );
}

function NotFoundPanel() {
  return (
    <div
      className={cn(
        "card-shell rounded-2xl bg-[var(--color-surface-3)]",
        "flex flex-col items-center gap-4 px-8 py-16 text-center",
      )}
    >
      <span
        aria-hidden
        className="grid h-14 w-14 place-items-center rounded-2xl bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.18),oklch(from_var(--color-primary)_l_c_h_/_0.02))] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]"
      >
        <PackageX className="h-6 w-6 text-[var(--color-primary)]" />
      </span>
      <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
        Product not found
      </span>
      <h3 className="text-display max-w-md text-xl font-semibold leading-tight tracking-[-0.02em]">
        We couldn't find that product.
      </h3>
      <p className="max-w-md text-sm leading-relaxed text-[var(--color-muted-foreground)]">
        It may have been deleted, or the link may be wrong. Head back to the list
        and try again.
      </p>
      <Button asChild className="brand-glow gradient-sheen mt-2 gap-1.5">
        <Link to="/catalog/products">
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to products
        </Link>
      </Button>
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Edit / Delete / Price / Stock dialogs
//  (Self-contained, take open + product + onClose. The product list page
//  has its own copies; if a third consumer appears, lift to a shared
//  module.)
// ───────────────────────────────────────────────────────────────────────

function ProductEditorDialog({
  open,
  product,
  onClose,
}: {
  open: boolean;
  product: ProductDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "all-for-products-filter"],
    queryFn: () => searchBrands({ pageSize: 200 }),
    staleTime: 60_000,
    enabled: open,
  });
  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "all-for-products-filter"],
    queryFn: () => searchCategories({ pageSize: 200 }),
    staleTime: 60_000,
    enabled: open,
  });

  const [name, setName] = useState(product.name);
  const [description, setDescription] = useState(product.description ?? "");
  const [brandId, setBrandId] = useState(product.brandId);
  const [categoryId, setCategoryId] = useState(product.categoryId);
  const [isActive, setIsActive] = useState(product.isActive);

  useEffect(() => {
    if (open) {
      setName(product.name);
      setDescription(product.description ?? "");
      setBrandId(product.brandId);
      setCategoryId(product.categoryId);
      setIsActive(product.isActive);
    }
  }, [open, product]);

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProductInput) => updateProduct(input),
    onSuccess: () => {
      toast.success("Product updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error("Update failed", { description: describe(err) }),
  });

  const trimmedName = name.trim();
  const valid = trimmedName.length > 0 && brandId && categoryId;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    updateMutation.mutate({
      productId: product.id,
      name: trimmedName,
      description: description.trim() || null,
      brandId,
      categoryId,
      isActive,
    });
  };

  const brands = brandsQuery.data?.items ?? [];
  const categories = categoriesQuery.data?.items ?? [];

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              Edit entry
            </span>
            <DialogTitle>Edit product</DialogTitle>
            <DialogDescription>
              Update details for {product.name}. Use the price/stock chips on the
              detail page to change those — they emit domain events.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="edit-name" label="Name" required>
                <Input
                  id="edit-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                  maxLength={200}
                  autoFocus
                />
              </Field>
              <Field id="edit-sku" label="SKU" hint="SKU is fixed after creation.">
                <Input
                  id="edit-sku"
                  value={product.sku}
                  disabled
                  className="font-mono text-[13px] tracking-tight"
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="edit-brand" label="Brand" required>
                <Combobox
                  id="edit-brand"
                  label="Brand"
                  value={brandId || null}
                  onChange={(v) => setBrandId(v ?? "")}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))}
                  searchable
                  required
                />
              </Field>
              <Field id="edit-category" label="Category" required>
                <Combobox
                  id="edit-category"
                  label="Category"
                  value={categoryId || null}
                  onChange={(v) => setCategoryId(v ?? "")}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                  searchable
                  required
                />
              </Field>
            </div>

            <Field id="edit-description" label="Description" hint="Shown on listing and product detail pages.">
              <textarea
                id="edit-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                maxLength={4000}
                className={cn(
                  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                )}
              />
            </Field>

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
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={updateMutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={updateMutation.isPending || !valid}
              className="brand-glow gradient-sheen"
            >
              {updateMutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDialog({
  open,
  product,
  onClose,
  onDeleted,
}: {
  open: boolean;
  product: ProductDto;
  onClose: () => void;
  onDeleted: () => void;
}) {
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => deleteProduct(product.id),
    onSuccess: () => {
      toast.success("Product deleted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
      onDeleted();
    },
    onError: (err: unknown) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-destructive)]">
            Permanent action
          </span>
          <DialogTitle>Delete product</DialogTitle>
          <DialogDescription>
            This permanently removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{product.name}</span>{" "}
            <span className="opacity-70">({formatDate(product.createdAtUtc)})</span>. The product
            will no longer appear in any listing or report.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={mutation.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            {mutation.isPending ? "Deleting…" : "Delete product"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function PriceDialog({
  open,
  product,
  onClose,
}: {
  open: boolean;
  product: ProductDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState(String(product.price.amount));
  const [currency, setCurrency] = useState(product.price.currency);

  useEffect(() => {
    if (open) {
      setAmount(String(product.price.amount));
      setCurrency(product.price.currency);
    }
  }, [open, product]);

  const mutation = useMutation({
    mutationFn: (input: ChangeProductPriceInput) => changeProductPrice(input),
    onSuccess: () => {
      toast.success("Price updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error("Price change failed", { description: describe(err) }),
  });

  const newAmount = Number.parseFloat(amount);
  const valid = !Number.isNaN(newAmount) && newAmount >= 0 && currency.length === 3;
  const delta = !Number.isNaN(newAmount) ? newAmount - product.price.amount : 0;

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid) return;
            mutation.mutate({ productId: product.id, amount: newAmount, currency });
          }}
        >
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              <CircleDollarSign className="mr-1 inline h-3 w-3" />
              Price change
            </span>
            <DialogTitle>{product.name}</DialogTitle>
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
                  {formatMoney(product.price.amount, product.price.currency)}
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
                  {!Number.isNaN(newAmount) ? formatMoney(newAmount, currency || "USD") : "—"}
                </div>
              </div>
            </div>
            <div className="grid grid-cols-[1fr_auto] gap-3">
              <Field id="pd-price-amount" label="New amount" required>
                <Input
                  id="pd-price-amount"
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
              <Field id="pd-price-currency" label="Currency" required>
                <Input
                  id="pd-price-currency"
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
  open,
  product,
  onClose,
}: {
  open: boolean;
  product: ProductDto;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [delta, setDelta] = useState("0");

  useEffect(() => {
    if (open) setDelta("0");
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: AdjustProductStockInput) => adjustProductStock(input),
    onSuccess: () => {
      toast.success("Stock adjusted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error("Adjustment failed", { description: describe(err) }),
  });

  const deltaNum = Number.parseInt(delta, 10);
  const valid = !Number.isNaN(deltaNum) && deltaNum !== 0;
  const newStock = product.stock + (Number.isNaN(deltaNum) ? 0 : deltaNum);
  const willGoNegative = newStock < 0;

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!valid || willGoNegative) return;
            mutation.mutate({ productId: product.id, delta: deltaNum });
          }}
        >
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              <Package className="mr-1 inline h-3 w-3" />
              Stock adjustment
            </span>
            <DialogTitle>{product.name}</DialogTitle>
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
                <div className="text-display mt-1 text-[18px] font-semibold">{product.stock}</div>
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
                  <span className="font-mono">{product.stock}</span>.
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
