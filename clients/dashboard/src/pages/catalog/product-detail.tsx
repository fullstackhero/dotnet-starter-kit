import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowDown,
  CircleDollarSign,
  FileText,
  Hash,
  Image as ImageIcon,
  Info,
  Layers,
  Minus,
  Package,
  PackageX,
  Pencil,
  Plus,
  RefreshCw,
  Tag,
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
  EntityDetailAvatar,
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailMeta,
  EntityDetailSection,
  EntityDetailStat,
  EntityStatusBadge,
  ErrorBand,
  Field,
} from "@/components/list";
import { ProductImageManager } from "@/components/file/product-image-manager";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatDateTimeMono,
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
  const { t } = useTranslation("catalog");
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

  const brand = brandQuery.data;
  const category = categoryQuery.data;

  return (
    <div className="pb-12">
      <EntityDetailBack to="/catalog/products" label={t("detail.back")} />

      {productQuery.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(productQuery.error)} />
        </div>
      )}

      {productQuery.isLoading ? (
        <DetailSkeleton />
      ) : product ? (
        <>
          <ProductHero
            product={product}
            brand={brand}
            category={category}
            isFetching={productQuery.isFetching}
            onRefresh={() => void productQuery.refetch()}
            onEdit={() => setDialog({ mode: "edit" })}
            onDelete={() => setDialog({ mode: "delete" })}
          />

          <div className="grid grid-cols-1 gap-5 lg:grid-cols-[300px_1fr]">
            {/* Left: sidebar with at-a-glance numbers + audit */}
            <aside className="space-y-5">
              <EntityDetailSection title={t("detail.section.pricing")} icon={CircleDollarSign}>
                <PricingPanel
                  product={product}
                  onPriceChange={() => setDialog({ mode: "price" })}
                />
              </EntityDetailSection>

              <EntityDetailSection title={t("detail.section.inventory")} icon={Package}>
                <InventoryPanel
                  product={product}
                  onStockAdjust={() => setDialog({ mode: "stock" })}
                />
              </EntityDetailSection>

              <EntityDetailSection title={t("detail.section.identifiers")} icon={Hash}>
                <IdentifiersPanel
                  product={product}
                  brand={brand}
                  category={category}
                />
              </EntityDetailSection>
            </aside>

            {/* Right: masonry-ish content area */}
            <div className="space-y-5">
              <EntityDetailSection
                title={t("detail.section.description")}
                icon={FileText}
                description={t("detail.section.descriptionDesc")}
                action={
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setDialog({ mode: "edit" })}
                    className="gap-1.5"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                    {t("action.edit")}
                  </Button>
                }
              >
                <DescriptionBody product={product} />
              </EntityDetailSection>

              <EntityDetailSection
                title={t("detail.section.images")}
                icon={ImageIcon}
                description={t("detail.section.imagesDesc")}
              >
                <ProductImageManager
                  productId={product.id}
                  images={product.images}
                  invalidateKey={["catalog", "products", productId]}
                />
              </EntityDetailSection>

              <EntityDetailSection title={t("detail.section.audit")} icon={Info}>
                <AuditPanel product={product} />
              </EntityDetailSection>
            </div>
          </div>

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
//  Hero
// ───────────────────────────────────────────────────────────────────────

function ProductHero({
  product,
  brand,
  category,
  isFetching,
  onRefresh,
  onEdit,
  onDelete,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
  isFetching: boolean;
  onRefresh: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const { t } = useTranslation("catalog");
  const stockTone: "default" | "warning" | "danger" =
    product.stock === 0 ? "danger" : product.stock < LOW_STOCK ? "warning" : "default";

  const subtitleParts: React.ReactNode[] = [
    <code
      key="sku"
      className="rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[11px] tracking-tight text-[var(--color-foreground)]"
    >
      {product.sku}
    </code>,
  ];
  if (brand) subtitleParts.push(<span key="brand">{brand.name}</span>);
  if (category) subtitleParts.push(<span key="cat">{category.name}</span>);

  const subtitle = (
    <span className="inline-flex flex-wrap items-center gap-x-2 gap-y-1">
      {subtitleParts.map((node, i) => (
        <span key={i} className="inline-flex items-center gap-x-2">
          {i > 0 && (
            <span aria-hidden className="text-[var(--color-border)]">
              ·
            </span>
          )}
          {node}
        </span>
      ))}
    </span>
  );

  return (
    <EntityDetailHero
      avatar={
        <EntityDetailAvatar
          src={product.thumbnailUrl}
          name={product.name}
          icon={Package}
        />
      }
      title={product.name}
      badges={
        <>
          {product.isActive ? (
            <EntityStatusBadge tone="success">{t("badge.active")}</EntityStatusBadge>
          ) : (
            <EntityStatusBadge tone="danger">{t("badge.hidden")}</EntityStatusBadge>
          )}
        </>
      }
      subtitle={subtitle}
      actions={
        <>
          <Button
            variant="outline"
            size="sm"
            disabled={isFetching}
            onClick={onRefresh}
            className="gap-1.5"
          >
            <RefreshCw className={cn("h-3.5 w-3.5", isFetching && "animate-spin")} />
            <span className="hidden sm:inline">{t("action.refresh")}</span>
          </Button>
          <Button variant="outline" size="sm" onClick={onEdit} className="gap-1.5">
            <Pencil className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">{t("action.edit")}</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onDelete}
            className="gap-1.5 hover:!border-[var(--color-destructive)] hover:!text-[var(--color-destructive)]"
          >
            <Trash2 className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">{t("action.delete")}</span>
          </Button>
        </>
      }
      stats={
        <>
          <EntityDetailStat
            icon={CircleDollarSign}
            value={formatMoney(product.price.amount, product.price.currency)}
            label={t("detail.stat.price")}
            tone="primary"
          />
          <EntityDetailStat
            icon={Package}
            value={product.stock}
            label={
              stockTone === "danger"
                ? t("detail.stat.outOfStock")
                : stockTone === "warning"
                  ? t("detail.stat.low", { n: LOW_STOCK })
                  : t("detail.stat.inStock")
            }
            tone={stockTone}
          />
          <EntityDetailStat
            icon={Layers}
            value={product.images?.length ?? 0}
            label={t("detail.stat.images")}
          />
        </>
      }
      meta={
        <>
          {brand && (
            <EntityDetailMeta icon={Tag}>
              <Link
                to={`/catalog/products?brand=${brand.id}`}
                className="transition-colors hover:text-[var(--color-foreground)]"
              >
                {brand.name}
              </Link>
            </EntityDetailMeta>
          )}
          {category && (
            <EntityDetailMeta icon={Layers}>
              <Link
                to={`/catalog/products?category=${category.id}`}
                className="transition-colors hover:text-[var(--color-foreground)]"
              >
                {category.name}
              </Link>
            </EntityDetailMeta>
          )}
          <EntityDetailMeta icon={Info} hideOnMobile>
            {t("detail.meta.created", { rel: formatRelative(product.createdAtUtc) })}
          </EntityDetailMeta>
          {product.updatedAtUtc && (
            <EntityDetailMeta icon={Info} hideOnTablet>
              {t("detail.meta.updated", { rel: formatRelative(product.updatedAtUtc) })}
            </EntityDetailMeta>
          )}
        </>
      }
    />
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Sidebar panels
// ───────────────────────────────────────────────────────────────────────

function PricingPanel({
  product,
  onPriceChange,
}: {
  product: ProductDto;
  onPriceChange: () => void;
}) {
  const { t } = useTranslation("catalog");
  return (
    <div className="space-y-3">
      <div>
        <div className="font-display text-[24px] font-semibold leading-none tracking-[-0.02em] tabular-nums text-[var(--color-foreground)]">
          {formatMoney(product.price.amount, product.price.currency)}
        </div>
        <div className="mt-1 text-[11.5px] text-[var(--color-muted-foreground)]">
          {t("detail.pricing.listed", { currency: product.price.currency })}
        </div>
      </div>
      <Button
        variant="outline"
        size="sm"
        onClick={onPriceChange}
        className="w-full gap-1.5"
      >
        <CircleDollarSign className="h-3.5 w-3.5" />
        {t("action.changePrice")}
      </Button>
    </div>
  );
}

function InventoryPanel({
  product,
  onStockAdjust,
}: {
  product: ProductDto;
  onStockAdjust: () => void;
}) {
  const { t } = useTranslation("catalog");
  const tone: "default" | "warning" | "danger" =
    product.stock === 0 ? "danger" : product.stock < LOW_STOCK ? "warning" : "default";
  return (
    <div className="space-y-3">
      <div>
        <div
          className={cn(
            "font-display text-[24px] font-semibold leading-none tracking-[-0.02em] tabular-nums",
            tone === "danger" && "text-[var(--color-destructive)]",
            tone === "warning" && "text-[var(--color-warning)]",
            tone === "default" && "text-[var(--color-foreground)]",
          )}
        >
          {product.stock}
        </div>
        <div className="mt-1 flex items-center gap-1 text-[11.5px] text-[var(--color-muted-foreground)]">
          {tone === "danger" ? (
            <>
              <AlertTriangle className="h-3 w-3 text-[var(--color-destructive)]" />
              <span className="text-[var(--color-destructive)]">{t("detail.inventory.outOfStock")}</span>
            </>
          ) : tone === "warning" ? (
            <>
              <AlertTriangle className="h-3 w-3 text-[var(--color-warning)]" />
              <span className="text-[var(--color-warning)]">
                {t("detail.inventory.below", { n: LOW_STOCK })}
              </span>
            </>
          ) : (
            <span>{t("detail.inventory.unitsOnHand")}</span>
          )}
        </div>
      </div>
      <Button
        variant="outline"
        size="sm"
        onClick={onStockAdjust}
        className="w-full gap-1.5"
      >
        <Package className="h-3.5 w-3.5" />
        {t("action.adjustStock")}
      </Button>
    </div>
  );
}

function IdentifiersPanel({
  product,
  brand,
  category,
}: {
  product: ProductDto;
  brand: BrandDto | undefined;
  category: CategoryDto | undefined;
}) {
  const { t } = useTranslation("catalog");
  return (
    <dl className="space-y-3 text-[13px]">
      <MetaRow label={t("detail.id.sku")} value={<IdCode value={product.sku} />} />
      <MetaRow label={t("detail.id.slug")} value={<IdCode value={product.slug} />} />
      <MetaRow label={t("detail.id.productId")} value={<IdCode value={product.id} />} />
      <MetaRow
        label={t("detail.id.brandId")}
        value={<IdCode value={brand?.id ?? product.brandId} />}
      />
      <MetaRow
        label={t("detail.id.categoryId")}
        value={<IdCode value={category?.id ?? product.categoryId} />}
      />
    </dl>
  );
}

function AuditPanel({ product }: { product: ProductDto }) {
  const { t } = useTranslation("catalog");
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow
        label={t("detail.audit.created")}
        value={formatDateTimeMono(product.createdAtUtc)}
        hint={formatRelative(product.createdAtUtc)}
      />
      {product.updatedAtUtc ? (
        <MetaRow
          label={t("detail.audit.revised")}
          value={formatDateTimeMono(product.updatedAtUtc)}
          hint={formatRelative(product.updatedAtUtc)}
        />
      ) : (
        <MetaRow label={t("detail.audit.revised")} value={t("detail.audit.never")} hint={t("detail.audit.noEdits")} />
      )}
      <MetaRow
        label={t("detail.audit.status")}
        value={product.isActive ? t("detail.audit.active") : t("detail.audit.hidden")}
        tone={product.isActive ? "success" : "muted"}
      />
    </dl>
  );
}

function DescriptionBody({ product }: { product: ProductDto }) {
  const { t } = useTranslation("catalog");
  if (product.description) {
    return (
      <p className="whitespace-pre-wrap text-[14px] leading-relaxed text-[var(--color-foreground)]/90">
        {product.description}
      </p>
    );
  }
  return (
    <p className="text-[13px] italic leading-relaxed text-[var(--color-muted-foreground)]">
      {t("detail.description.empty")}
    </p>
  );
}

function MetaRow({
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
    <div className="grid grid-cols-[90px_1fr] items-baseline gap-3">
      <dt className="text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
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
          <div className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]/70">
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
    <div className="space-y-5">
      <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
        <Skeleton className="h-1 w-full rounded-none" />
        <div className="p-5 sm:px-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex min-w-0 items-center gap-4">
              <Skeleton className="size-14 rounded-2xl" />
              <div className="space-y-2">
                <Skeleton className="h-5 w-48" />
                <Skeleton className="h-3 w-64" />
              </div>
            </div>
            <Skeleton className="h-8 w-40" />
          </div>
          <div className="mt-4 flex flex-wrap gap-2">
            <Skeleton className="h-7 w-24 rounded-lg" />
            <Skeleton className="h-7 w-24 rounded-lg" />
            <Skeleton className="h-7 w-24 rounded-lg" />
          </div>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[300px_1fr]">
        <div className="space-y-5">
          <Skeleton className="h-40 rounded-xl" />
          <Skeleton className="h-40 rounded-xl" />
        </div>
        <div className="space-y-5">
          <Skeleton className="h-32 rounded-xl" />
          <Skeleton className="h-64 rounded-xl" />
        </div>
      </div>
    </div>
  );
}

function NotFoundPanel() {
  const { t } = useTranslation("catalog");
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-8 py-16 text-center">
      <div className="mb-5 grid size-16 place-items-center rounded-2xl bg-[oklch(from_var(--color-primary)_l_c_h_/_0.08)]">
        <PackageX className="size-7 text-[var(--color-primary)]" />
      </div>
      <h3 className="mb-1.5 text-[17px] font-semibold text-[var(--color-foreground)]">
        {t("detail.notFound.title")}
      </h3>
      <p className="mb-6 text-[13px] text-[var(--color-muted-foreground)]">
        {t("detail.notFound.body")}
      </p>
      <Button asChild variant="outline" size="sm">
        <Link to="/catalog/products">{t("detail.back")}</Link>
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
  const { t } = useTranslation("catalog");
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
      toast.success(t("toast.productUpdated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error(t("toast.updateFailed"), { description: describe(err) }),
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
            <DialogTitle>{t("detail.editor.title")}</DialogTitle>
            <DialogDescription>
              {t("detail.editor.desc", { name: product.name })}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="edit-name" label={t("field.name")} required>
                <Input
                  id="edit-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                  maxLength={200}
                  autoFocus
                />
              </Field>
              <Field id="edit-sku" label={t("field.sku")} hint={t("hint.skuFixed")}>
                <Input
                  id="edit-sku"
                  value={product.sku}
                  disabled
                  className="font-mono text-[13px] tracking-tight"
                />
              </Field>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="edit-brand" label={t("field.brand")} required>
                <Combobox
                  id="edit-brand"
                  label={t("field.brand")}
                  value={brandId || null}
                  onChange={(v) => setBrandId(v ?? "")}
                  options={brands.map((b) => ({ value: b.id, label: b.name }))}
                  searchable
                  required
                />
              </Field>
              <Field id="edit-category" label={t("field.category")} required>
                <Combobox
                  id="edit-category"
                  label={t("field.category")}
                  value={categoryId || null}
                  onChange={(v) => setCategoryId(v ?? "")}
                  options={categories.map((c) => ({ value: c.id, label: c.name }))}
                  searchable
                  required
                />
              </Field>
            </div>

            <Field id="edit-description" label={t("field.description")} hint={t("hint.description")}>
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

            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
              <div>
                <div className="text-[12px] font-medium text-[var(--color-foreground)]">
                  {t("visibility.title")}
                </div>
                <div className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                  {isActive ? t("visibility.listed") : t("visibility.hidden")}
                </div>
              </div>
              <Switch checked={isActive} onCheckedChange={setIsActive} aria-label={t("visibility.active")} />
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={updateMutation.isPending}>
                {t("action.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={updateMutation.isPending || !valid}>
              {updateMutation.isPending ? t("action.saving") : t("action.saveChanges")}
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
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: () => deleteProduct(product.id),
    onSuccess: () => {
      toast.success(t("toast.productDeleted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      queryClient.invalidateQueries({ queryKey: ["trash", "products"] });
      onClose();
      onDeleted();
    },
    onError: (err: unknown) => toast.error(t("toast.deleteFailed"), { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("delete.productTitle")}</DialogTitle>
          <DialogDescription>
            {t("delete.removesPrefix")}
            <span className="font-medium text-[var(--color-foreground)]">{product.name}</span>{" "}
            <span className="opacity-70">{t("delete.dateOnly", { date: formatDate(product.createdAtUtc) })}</span>
            {t("delete.productBody")}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={mutation.isPending}>
              {t("action.cancel")}
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            {mutation.isPending ? t("action.deleting") : t("delete.productTitle")}
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
  const { t } = useTranslation("catalog");
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
      toast.success(t("toast.priceUpdated"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error(t("toast.priceChangeFailed"), { description: describe(err) }),
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
            <DialogTitle>{t("price.title")}</DialogTitle>
            <DialogDescription>
              {t("price.emitsDetailPrefix", { name: product.name })}
              <code className="font-mono text-[11px]">ProductPriceChanged</code>
              {t("price.emitsDetailSuffix")}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="flex items-center justify-between rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
              <div>
                <div className="text-[11px] uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("price.was")}
                </div>
                <div className="font-display mt-1 text-[18px] font-semibold tabular-nums">
                  {formatMoney(product.price.amount, product.price.currency)}
                </div>
              </div>
              <ArrowDown className="h-4 w-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="text-[11px] uppercase tracking-wider text-[var(--color-primary)]">
                  {t("price.becomes")}
                </div>
                <div
                  className={cn(
                    "font-display mt-1 text-[18px] font-semibold tabular-nums",
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
              <Field id="pd-price-amount" label={t("field.newAmount")} required>
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
              <Field id="pd-price-currency" label={t("field.currency")} required>
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
                {t("action.cancel")}
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? t("action.saving") : t("action.changePrice")}
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
  const { t } = useTranslation("catalog");
  const queryClient = useQueryClient();
  const [delta, setDelta] = useState("0");

  useEffect(() => {
    if (open) setDelta("0");
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: AdjustProductStockInput) => adjustProductStock(input),
    onSuccess: () => {
      toast.success(t("toast.stockAdjusted"));
      queryClient.invalidateQueries({ queryKey: ["catalog", "products"] });
      onClose();
    },
    onError: (err: unknown) => toast.error(t("toast.adjustmentFailed"), { description: describe(err) }),
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
            <DialogTitle>{t("stock.title")}</DialogTitle>
            <DialogDescription>
              {t("stock.emitsDetailPrefix", { name: product.name })}
              <code className="font-mono text-[11px]">ProductStockAdjusted</code>
              {t("stock.emitsDetailSuffix")}
            </DialogDescription>
          </DialogHeader>
          <DialogBody className="space-y-4">
            <div className="flex items-center justify-between rounded-xl border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3 tabular-nums">
              <div>
                <div className="text-[11px] uppercase tracking-wider text-[var(--color-muted-foreground)]">
                  {t("stock.current")}
                </div>
                <div className="font-display mt-1 text-[18px] font-semibold">{product.stock}</div>
              </div>
              <ArrowDown className="h-4 w-4 -rotate-90 text-[var(--color-muted-foreground)]" />
              <div className="text-right">
                <div className="text-[11px] uppercase tracking-wider text-[var(--color-primary)]">
                  {t("stock.becomes")}
                </div>
                <div
                  className={cn(
                    "font-display mt-1 text-[18px] font-semibold",
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
                aria-label={t("stock.deltaLabel")}
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
                  {t("stock.negativeDetailPrefix")}
                  <span className="font-mono">{product.stock}</span>
                  {t("stock.negativeSuffix")}
                </span>
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                {t("action.cancel")}
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !valid || willGoNegative}
            >
              {mutation.isPending ? t("action.adjusting") : t("action.adjustStock")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
