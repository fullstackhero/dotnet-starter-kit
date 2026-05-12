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
  Check,
  Image as ImageIcon,
  ImageOff,
  Pencil,
  Search,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  createBrand,
  deleteBrand,
  searchBrands,
  updateBrand,
  type BrandDto,
  type CreateBrandInput,
  type UpdateBrandInput,
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
import {
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
  formatRelative,
  pad2,
  slugify,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DENSITY_KEY = "fsh.dashboard.catalog.brands.density";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; brand: BrandDto }
  | { mode: "delete"; brand: BrandDto };

type SortKey = "name" | "slug" | "createdAtUtc";

const SORT_OPTIONS: SortOption<SortKey>[] = [
  { key: "name", label: "Name" },
  { key: "slug", label: "Slug" },
  { key: "createdAtUtc", label: "Created" },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function BrandsPage() {
  const { user } = useAuth();
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

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

  const query = useQuery({
    queryKey: [
      "catalog",
      "brands",
      { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE, sortKey, sortDir },
    ],
    queryFn: () =>
      searchBrands({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: sortKey,
        sortDir,
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  // Server-side sort drives the order. Items are already sorted on arrival.
  const sortedItems = items;

  const featured = useMemo(() => {
    if (debouncedSearch) return [];
    return [...items]
      .sort(
        (a, b) =>
          new Date(b.createdAtUtc).getTime() -
          new Date(a.createdAtUtc).getTime(),
      )
      .slice(0, 6);
  }, [items, debouncedSearch]);

  const stats = useMemo(() => {
    if (!data) return null;
    const total = data.totalCount;
    const withLogos = items.filter((b) => b.logoUrl).length;
    const pct = items.length === 0 ? 0 : Math.round((withLogos / items.length) * 100);
    const latest =
      items.length === 0
        ? null
        : items.reduce(
            (best, b) =>
              new Date(b.createdAtUtc) > new Date(best.createdAtUtc) ? b : best,
            items[0],
          );
    return { total, withLogosPct: pct, latest };
  }, [data, items]);

  const onSort = useCallback(
    (key: SortKey) => {
      if (sortKey === key) {
        setSortDir((d) => (d === "asc" ? "desc" : "asc"));
      } else {
        setSortKey(key);
        setSortDir(key === "createdAtUtc" ? "desc" : "asc");
      }
    },
    [sortKey],
  );

  return (
    <div className="space-y-7 pb-12">
      <ListHero
        eyebrow="Catalog · Inventory"
        tenant={user?.tenant ?? "—"}
        subEyebrow="registry of maker imprints"
        title="Brands"
        totalCount={data?.totalCount ?? null}
        subtitle="Curate the maker imprints behind every product in your catalog. Each brand collects its own slug, story, and visual mark."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find a brand by name or slug…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="New brand"
        onCreate={() => setEditor({ mode: "create" })}
      />

      {stats && data && data.totalCount > 0 && (
        <StatStrip cols={3}>
          <Stat label="Total brands" value={pad2(stats.total)} hint="across this tenant" />
          <Stat
            label="With logo"
            value={`${stats.withLogosPct}%`}
            hint={stats.withLogosPct === 0 ? "none" : "of this page"}
            accent
          />
          <Stat
            label="Latest entry"
            value={stats.latest ? formatRelative(stats.latest.createdAtUtc) : "—"}
            hint={stats.latest ? stats.latest.name : "no entries"}
          />
        </StatStrip>
      )}

      {!debouncedSearch && featured.length > 1 && (
        <FeaturedRail
          items={featured}
          onPick={(brand) => setEditor({ mode: "edit", brand })}
        />
      )}

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section className="fsh-enter fsh-enter-3 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <SortChips
            options={SORT_OPTIONS}
            sortKey={sortKey}
            sortDir={sortDir}
            onSort={onSort}
            prefixLabel={debouncedSearch ? "results" : "registry"}
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
              const filtered = debouncedSearch.length > 0;
              return (
                <EmptyState
                  eyebrow={filtered ? "No matches" : "Empty registry"}
                  headline={
                    filtered
                      ? `Nothing matches "${debouncedSearch}".`
                      : "Your catalog is awaiting its first brand."
                  }
                  body={
                    filtered
                      ? "Search runs across name and slug. Try a different term, or clear the filter and start fresh."
                      : "A brand is the maker imprint behind a product — its name, slug, and visual mark. Create one to start curating."
                  }
                  icon={
                    filtered ? (
                      <Search className="h-6 w-6 text-[var(--color-primary)]" />
                    ) : (
                      <ImageOff className="h-6 w-6 text-[var(--color-primary)]" />
                    )
                  }
                  primaryAction={{
                    label: filtered ? "Add a new brand" : "Add the first brand",
                    onClick: () => setEditor({ mode: "create" }),
                    icon: <Sparkles className="h-3.5 w-3.5" />,
                  }}
                  secondaryAction={
                    filtered
                      ? {
                          label: "Clear search",
                          onClick: () => setSearch(""),
                          icon: <X className="h-3.5 w-3.5" />,
                        }
                      : undefined
                  }
                />
              );
            })()
          ) : (
            <ul>
              {sortedItems.map((brand, i) => (
                <Row
                  key={brand.id}
                  brand={brand}
                  density={density}
                  delayMs={Math.min(i, 8) * 30}
                  onEdit={() => setEditor({ mode: "edit", brand })}
                  onDelete={() => setEditor({ mode: "delete", brand })}
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

      <BrandEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
      />
      <DeleteBrandDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Featured rail (Brand-specific — not lifted into shared primitives)
// ───────────────────────────────────────────────────────────────────────

function FeaturedRail({
  items,
  onPick,
}: {
  items: BrandDto[];
  onPick: (brand: BrandDto) => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-3">
      <div className="mb-3 flex items-end justify-between">
        <div className="flex items-baseline gap-2.5">
          <h2 className="text-display text-[15px] font-semibold tracking-[-0.01em]">
            Recently added
          </h2>
          <span className="font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
            {pad2(items.length)} on file
          </span>
        </div>
      </div>

      <div
        className="relative"
        style={{
          maskImage:
            "linear-gradient(to right, black calc(100% - 28px), transparent)",
          WebkitMaskImage:
            "linear-gradient(to right, black calc(100% - 28px), transparent)",
        }}
      >
        <ul className="-mx-1 flex snap-x snap-mandatory gap-3 overflow-x-auto px-1 pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
          {items.map((brand) => (
            <li key={brand.id} className="snap-start">
              <FeaturedCard brand={brand} onClick={() => onPick(brand)} />
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function FeaturedCard({ brand, onClick }: { brand: BrandDto; onClick: () => void }) {
  const initial = brand.name.trim().charAt(0).toUpperCase() || "·";
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "group/feat card-shell card-shell-interactive relative flex w-[260px] shrink-0 cursor-pointer flex-col gap-3 overflow-hidden rounded-xl text-left",
        "bg-[var(--color-surface-3)] p-4",
        "transition-colors duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
        "hover:bg-[var(--color-surface-4)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
      )}
    >
      <div className="flex items-center gap-3">
        <Swatch logoUrl={brand.logoUrl} initial={initial} size={40} />
        <div className="min-w-0 flex-1">
          <div className="truncate text-[15px] font-semibold leading-tight tracking-[-0.005em]">
            {brand.name}
          </div>
          <div className="mt-0.5 truncate font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {brand.slug}
          </div>
        </div>
      </div>
      <p
        className={cn(
          "line-clamp-2 text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]",
          !brand.description && "italic opacity-70",
        )}
      >
        {brand.description ?? "No description on file."}
      </p>
      <div className="mt-auto flex items-center justify-between font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
        <span>{formatDateMono(brand.createdAtUtc)}</span>
        <span className="opacity-70 transition-colors group-hover/feat:text-[var(--color-primary)] group-hover/feat:opacity-100">
          {formatRelative(brand.createdAtUtc)}
        </span>
      </div>
    </button>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row
// ───────────────────────────────────────────────────────────────────────

function Row({
  brand,
  density,
  delayMs,
  onEdit,
  onDelete,
}: {
  brand: BrandDto;
  density: Density;
  delayMs: number;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const padY = density === "compact" ? "py-3" : "py-4";

  return (
    <li
      className={cn(
        "fsh-enter group/row relative flex items-center gap-4 border-b border-[var(--color-border)] px-5 last:border-b-0 sm:px-6",
        padY,
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "hover:bg-[var(--color-surface-4)]",
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

      <Swatch
        logoUrl={brand.logoUrl}
        initial={brand.name.trim().charAt(0).toUpperCase() || "·"}
        size={density === "compact" ? 36 : 48}
      />

      <div className="flex min-w-0 flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.4]">
          <div className="flex items-baseline gap-2">
            <span className="text-display truncate text-[15.5px] font-semibold leading-tight tracking-[-0.01em] sm:text-[16px]">
              {brand.name}
            </span>
            <code
              title={brand.slug}
              className="hidden truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)] sm:inline-block"
            >
              {brand.slug}
            </code>
          </div>
          <div
            className={cn(
              "mt-1 truncate text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]",
              !brand.description && "italic opacity-60",
            )}
            title={brand.description ?? undefined}
          >
            {brand.description ?? "No description on file."}
          </div>
        </div>

        <div className="hidden min-w-[110px] text-right tabular-nums sm:block">
          <div className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-foreground)]/85">
            {formatDateMono(brand.createdAtUtc)}
          </div>
          <div className="mt-0.5 text-[10.5px] text-[var(--color-muted-foreground)]">
            {formatRelative(brand.createdAtUtc)}
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
        <RowAction label={`Edit ${brand.name}`} onClick={onEdit}>
          <Pencil className="h-3.5 w-3.5" />
        </RowAction>
        <RowAction label={`Delete ${brand.name}`} onClick={onDelete} tone="danger">
          <Trash2 className="h-3.5 w-3.5" />
        </RowAction>
      </div>
    </li>
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

function Swatch({
  logoUrl,
  initial,
  size,
}: {
  logoUrl: string | null | undefined;
  initial: string;
  size: number;
}) {
  const style = { width: size, height: size };
  if (logoUrl) {
    return (
      <span
        style={style}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
          "bg-[var(--color-surface-2)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img
          src={logoUrl}
          alt=""
          className="h-full w-full object-contain p-1.5"
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
      <span
        className={cn(
          "text-display relative font-semibold leading-none tracking-[-0.02em] text-[var(--color-primary)]",
          size >= 48 ? "text-[18px]" : "text-[14px]",
        )}
      >
        {initial}
      </span>
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
      <Skeleton className={cn("rounded-xl", density === "compact" ? "h-9 w-9" : "h-12 w-12")} />
      <div className="flex flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.4] space-y-2">
          <Skeleton className="h-4 w-44" />
          <Skeleton className="h-3 w-72" />
        </div>
        <div className="hidden min-w-[110px] space-y-1.5 text-right sm:block">
          <Skeleton className="ml-auto h-3 w-24" />
          <Skeleton className="ml-auto h-2.5 w-16" />
        </div>
      </div>
      <Skeleton className="h-7 w-16" />
    </li>
  );
}


// ───────────────────────────────────────────────────────────────────────
//  Editor dialog
// ───────────────────────────────────────────────────────────────────────

function BrandEditorDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const brand = state.mode === "edit" ? state.brand : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: brand?.name ?? "",
      description: brand?.description ?? "",
      logoUrl: brand?.logoUrl ?? "",
    }),
    [brand?.id, brand?.name, brand?.description, brand?.logoUrl],
  );

  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [logoUrl, setLogoUrl] = useState(initial.logoUrl);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setDescription(initial.description);
      setLogoUrl(initial.logoUrl);
    }
  }, [isOpen, initial.name, initial.description, initial.logoUrl]);

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);

  const createMutation = useMutation({
    mutationFn: (input: CreateBrandInput) => createBrand(input),
    onSuccess: () => {
      toast.success("Brand created");
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateBrandInput) => updateBrand(input),
    onSuccess: () => {
      toast.success("Brand updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = name.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedName) return;
    const payload = {
      name: trimmedName,
      description: description.trim() || null,
      logoUrl: logoUrl.trim() || null,
    };
    if (state.mode === "edit" && brand) {
      updateMutation.mutate({ brandId: brand.id, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {brand ? "Edit entry" : "New entry"}
            </span>
            <DialogTitle>{brand ? "Edit brand" : "Add a brand"}</DialogTitle>
            <DialogDescription>
              {brand
                ? `Update details for ${brand.name}. The slug is re-derived from the name.`
                : "Add a brand to your catalog. The slug is generated automatically from the name."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <RowPreview
              name={trimmedName}
              slug={slugPreview}
              description={description}
              logoUrl={logoUrl}
            />

            <Field id="brand-name" label="Name" required>
              <Input
                id="brand-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Acme Goods"
                autoFocus
                required
                maxLength={128}
              />
            </Field>

            <Field
              id="brand-slug"
              label="Slug"
              hint="Auto-derived from the name. Used in URLs."
            >
              <div className="surface-edge flex h-9 items-center gap-2 rounded-md bg-[var(--color-muted)] px-3">
                <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
                  →
                </span>
                <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                  {slugPreview}
                </code>
                <span className="ml-auto inline-flex items-center gap-1 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-success)]">
                  <Check className="h-3 w-3" /> ready
                </span>
              </div>
            </Field>

            <Field
              id="brand-description"
              label="Description"
              hint="Shown on listing and product detail pages."
            >
              <textarea
                id="brand-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={1024}
                className={cn(
                  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                )}
                placeholder="Quality essentials for the modern home."
              />
            </Field>

            <Field
              id="brand-logo"
              label="Logo URL"
              hint="Optional. Public URL to the brand's logo image."
            >
              <Input
                id="brand-logo"
                value={logoUrl}
                onChange={(e) => setLogoUrl(e.target.value)}
                placeholder="https://…"
                maxLength={512}
                type="url"
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={isPending || !trimmedName}
              className="brand-glow gradient-sheen"
            >
              {isPending ? "Saving…" : brand ? "Save changes" : "Add brand"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function RowPreview({
  name,
  slug,
  description,
  logoUrl,
}: {
  name: string;
  slug: string;
  description: string;
  logoUrl: string;
}) {
  const initial = name.trim().charAt(0).toUpperCase() || "·";
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
        {logoUrl.trim() ? (
          <span className="grid h-11 w-11 place-items-center overflow-hidden rounded-xl bg-[var(--color-surface-3)] ring-1 ring-inset ring-[var(--color-border)]">
            <ImageIcon className="h-4 w-4 text-[var(--color-muted-foreground)]" />
          </span>
        ) : (
          <span
            aria-hidden
            className="grid h-11 w-11 place-items-center rounded-xl bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))] text-[15px] font-semibold text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]"
          >
            {initial}
          </span>
        )}
        <div className="min-w-0 flex-1">
          <div
            className={cn(
              "truncate text-[15px] font-semibold leading-tight tracking-[-0.005em]",
              !name.trim() && "text-[var(--color-muted-foreground)]",
            )}
          >
            {name.trim() || "Brand name"}
          </div>
          <div className="mt-0.5 truncate font-mono text-[10.5px] uppercase tracking-[0.12em] text-[var(--color-muted-foreground)]">
            {slug}
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
//  Delete confirmation
// ───────────────────────────────────────────────────────────────────────

function DeleteBrandDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "delete";
  const brand = state.mode === "delete" ? state.brand : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteBrand(id),
    onSuccess: () => {
      toast.success("Brand deleted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
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
          <DialogTitle>Delete brand</DialogTitle>
          <DialogDescription>
            This permanently removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{brand?.name}</span>{" "}
            <span className="opacity-70">
              ({brand && formatDate(brand.createdAtUtc)})
            </span>
            . Products referencing this brand will need to be reassigned.
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
            onClick={() => brand && deleteMutation.mutate(brand.id)}
            disabled={deleteMutation.isPending || !brand}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete brand"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
