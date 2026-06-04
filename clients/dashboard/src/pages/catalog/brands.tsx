import {
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
  ChevronRight,
  Pencil,
  Plus,
  Search,
  Tag,
  Trash2,
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
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import {
  describe,
  formatDate,
  formatRelative,
  slugify,
} from "@/lib/list-helpers";

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; brand: BrandDto }
  | { mode: "delete"; brand: BrandDto };

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function BrandsPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

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
      { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE },
    ],
    queryFn: () =>
      searchBrands({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: "createdAtUtc",
        sortDir: "desc",
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Tag}
        title="Brands"
        total={data?.totalCount ?? null}
        unit="brand"
        description="Curate the maker imprints behind every product. Each brand carries its own slug, story, and logo."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New brand
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name or slug…"
      />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_180px_140px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Tag}
          title={searchActive ? "No brands found" : "No brands yet"}
          body={
            searchActive
              ? debouncedSearch
                ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
                : "No brands match the current filters."
              : "Add your first brand to start building the catalog. Each brand carries its own slug, description, and logo."
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => setSearch("")}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Clear search
              </Button>
            ) : (
              <Button
                onClick={() => setEditor({ mode: "create" })}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                Add brand
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} brand
              {(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((brand) => (
              <MobileCard
                key={brand.id}
                brand={brand}
                onEdit={() => setEditor({ mode: "edit", brand })}
              />
            ))}
          </div>

          {/* Desktop: list card */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_180px_140px_24px]">
              <span>Brand</span>
              <span>Slug</span>
              <span>Created</span>
              <span />
            </EntityListHeader>

            {items.map((brand, i) => (
              <DesktopRow
                key={brand.id}
                brand={brand}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", brand })}
                onDelete={() => setEditor({ mode: "delete", brand })}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={data?.totalPages ?? 1}
            hasPrev={!!data?.hasPrevious}
            hasNext={!!data?.hasNext}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
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
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function MobileCard({
  brand,
  onEdit,
}: {
  brand: BrandDto;
  onEdit: () => void;
}) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit brand ${brand.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <BrandAvatar brand={brand} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {brand.name}
            </p>
            <code className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {brand.slug}
            </code>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <ChevronRight className="size-4 text-[var(--color-border)]" />
        </div>
      </div>
      {brand.description && (
        <p className="mt-2 ml-[52px] line-clamp-2 text-[12px] text-[var(--color-muted-foreground)]">
          {brand.description}
        </p>
      )}
    </EntityMobileCard>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function DesktopRow({
  brand,
  isLast,
  onEdit,
  onDelete,
}: {
  brand: BrandDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow
      className="grid-cols-[1fr_180px_140px_24px]"
      isLast={isLast}
    >
      {/* Name + avatar */}
      <div className="flex min-w-0 items-center gap-3">
        <BrandAvatar brand={brand} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {brand.name}
          </div>
          {brand.description && (
            <div
              className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]"
              title={brand.description}
            >
              {brand.description}
            </div>
          )}
        </div>
      </div>

      {/* Slug */}
      <code
        title={brand.slug}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {brand.slug}
      </code>

      {/* Created */}
      <div className="min-w-0 text-[12px] text-[var(--color-muted-foreground)] tabular-nums">
        <div className="truncate">{formatDate(brand.createdAtUtc)}</div>
        <div className="truncate text-[11px] opacity-70">
          {formatRelative(brand.createdAtUtc)}
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${brand.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${brand.name}`}
          onClick={onDelete}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)] group-hover:opacity-100"
        >
          <Trash2 className="size-3.5" />
        </button>
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Brand avatar — image if present, else initials tile.
// ───────────────────────────────────────────────────────────────────────

function BrandAvatar({ brand, size }: { brand: BrandDto; size: number }) {
  if (brand.logoUrl) {
    return (
      <span
        style={{ width: size, height: size }}
        className={cn(
          "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
          "bg-[var(--color-muted)] ring-1 ring-inset ring-[var(--color-border)]",
        )}
      >
        <img
          src={brand.logoUrl}
          alt=""
          className="h-full w-full object-contain p-1"
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
          className="absolute inset-0 grid place-items-center font-display text-[14px] font-bold tracking-tight text-[var(--color-muted-foreground)]"
        >
          {brand.name.trim().charAt(0).toUpperCase() || "·"}
        </span>
      </span>
    );
  }
  return <EntityInitialsAvatar name={brand.name} size={size} />;
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
    [brand?.name, brand?.description, brand?.logoUrl],
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
            <DialogTitle>{brand ? "Edit brand" : "Add a brand"}</DialogTitle>
            <DialogDescription>
              {brand
                ? `Update details for ${brand.name}. The slug is re-derived from the name.`
                : "Add a brand to your catalog. The slug is generated automatically from the name."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
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
              <div className="flex h-9 items-center gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3">
                <code className="truncate font-mono text-[12.5px] tracking-tight text-[var(--color-foreground)]">
                  {slugPreview}
                </code>
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
                  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
                  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
                  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
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
            <Button type="submit" disabled={isPending || !trimmedName}>
              {isPending ? "Saving…" : brand ? "Save changes" : "Add brand"}
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
      queryClient.invalidateQueries({ queryKey: ["trash", "brands"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">
            Delete brand
          </DialogTitle>
          <DialogDescription>
            This permanently removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">
              {brand?.name}
            </span>{" "}
            <span className="opacity-70">
              (created {brand && formatDate(brand.createdAtUtc)})
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
