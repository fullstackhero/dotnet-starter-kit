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
  ChevronsRight,
  Folder,
  FolderTree,
  GitBranch,
  Pencil,
  Search,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  createCategory,
  deleteCategory,
  getCategoryTree,
  searchCategories,
  updateCategory,
  type CategoryDto,
  type CategoryTreeNodeDto,
  type CreateCategoryInput,
  type UpdateCategoryInput,
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
  formatRelative,
  pad2,
  slugify,
} from "@/lib/list-helpers";

const PAGE_SIZE = 50;
const DENSITY_KEY = "fsh.dashboard.catalog.categories.density";

type FlatNode = {
  id: string;
  name: string;
  slug: string;
  depth: number;
  ancestorIds: string[];
};
function flattenTree(
  nodes: CategoryTreeNodeDto[],
  depth = 0,
  ancestors: string[] = [],
): FlatNode[] {
  const out: FlatNode[] = [];
  for (const n of nodes) {
    out.push({ id: n.id, name: n.name, slug: n.slug, depth, ancestorIds: ancestors });
    if (n.children?.length)
      out.push(...flattenTree(n.children, depth + 1, [...ancestors, n.id]));
  }
  return out;
}

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; category: CategoryDto }
  | { mode: "delete"; category: CategoryDto };

type SortKey = "name" | "slug" | "createdAtUtc";

const SORT_OPTIONS: SortOption<SortKey>[] = [
  { key: "name", label: "Name" },
  { key: "slug", label: "Slug" },
  { key: "createdAtUtc", label: "Created" },
];

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function CategoriesPage() {
  const { user } = useAuth();
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const [sortKey, setSortKey] = useState<SortKey>("name");
  const [sortDir, setSortDir] = useState<SortDir>("asc");

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
      "categories",
      { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE, sortKey, sortDir },
    ],
    queryFn: () =>
      searchCategories({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: sortKey,
        sortDir,
      }),
    placeholderData: keepPreviousData,
  });

  const treeQuery = useQuery({
    queryKey: ["catalog", "categories", "tree"],
    queryFn: getCategoryTree,
    staleTime: 30_000,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const nameById = useMemo(() => {
    const map = new Map<string, string>();
    const walk = (nodes: CategoryTreeNodeDto[]) => {
      for (const n of nodes) {
        map.set(n.id, n.name);
        if (n.children?.length) walk(n.children);
      }
    };
    if (treeQuery.data) walk(treeQuery.data);
    return map;
  }, [treeQuery.data]);

  // Server-side sort drives the order. Items are already sorted on arrival.
  const sortedItems = items;

  const stats = useMemo(() => {
    if (!data) return null;
    const total = data.totalCount;
    const roots = items.filter((c) => !c.parentCategoryId).length;
    const branches = items.length - roots;
    const latest =
      items.length === 0
        ? null
        : items.reduce(
            (best, c) =>
              new Date(c.createdAtUtc) > new Date(best.createdAtUtc) ? c : best,
            items[0],
          );
    return { total, roots, branches, latest };
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
        eyebrow="Catalog · Taxonomy"
        tenant={user?.tenant ?? "—"}
        subEyebrow="shelves of the catalog"
        title="Categories"
        totalCount={data?.totalCount ?? null}
        subtitle="Group products into shelves. Categories nest under parents to form the taxonomy customers browse."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find a category by name or slug…"
        isFetching={query.isFetching}
        onRefresh={() => {
          void query.refetch();
          void treeQuery.refetch();
        }}
        ctaLabel="New category"
        onCreate={() => setEditor({ mode: "create" })}
      />

      {stats && data && data.totalCount > 0 && (
        <StatStrip cols={3}>
          <Stat label="Total categories" value={pad2(stats.total)} hint="across this tenant" />
          <Stat
            label="Tree shape"
            value={`${pad2(stats.roots)} / ${pad2(stats.branches)}`}
            hint="roots / branches on this page"
            accent
          />
          <Stat
            label="Latest entry"
            value={stats.latest ? formatRelative(stats.latest.createdAtUtc) : "—"}
            hint={stats.latest ? stats.latest.name : "no entries"}
          />
        </StatStrip>
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
                  eyebrow={filtered ? "No matches" : "Empty taxonomy"}
                  headline={
                    filtered
                      ? `Nothing matches "${debouncedSearch}".`
                      : "No categories on file yet."
                  }
                  body={
                    filtered
                      ? "Search runs across name and slug. Try a different term, or clear the filter."
                      : "Categories give your catalog its tree. Create root shelves, then nest sub-shelves under them."
                  }
                  icon={
                    filtered ? (
                      <Search className="h-6 w-6 text-[var(--color-primary)]" />
                    ) : (
                      <FolderTree className="h-6 w-6 text-[var(--color-primary)]" />
                    )
                  }
                  primaryAction={{
                    label: filtered ? "Add a new category" : "Add the first category",
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
            <ul role="list">
              {sortedItems.map((category, i) => (
                <Row
                  key={category.id}
                  category={category}
                  parentName={
                    category.parentCategoryId
                      ? nameById.get(category.parentCategoryId)
                      : undefined
                  }
                  density={density}
                  delayMs={Math.min(i, 8) * 30}
                  onEdit={() => setEditor({ mode: "edit", category })}
                  onDelete={() => setEditor({ mode: "delete", category })}
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

      <CategoryEditorDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
        tree={treeQuery.data ?? []}
      />
      <DeleteCategoryDialog
        state={editor}
        onClose={() => setEditor({ mode: "closed" })}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row
// ───────────────────────────────────────────────────────────────────────

function Row({
  category,
  parentName,
  density,
  delayMs,
  onEdit,
  onDelete,
}: {
  category: CategoryDto;
  parentName: string | undefined;
  density: Density;
  delayMs: number;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const padY = density === "compact" ? "py-3" : "py-4";
  const isRoot = !category.parentCategoryId;

  return (
    <li
      role="listitem"
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
        initial={category.name.trim().charAt(0).toUpperCase() || "·"}
        size={density === "compact" ? 36 : 48}
        isRoot={isRoot}
      />

      <div className="flex min-w-0 flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.4]">
          <div className="flex items-baseline gap-2">
            <span className="text-display truncate text-[15.5px] font-semibold leading-tight tracking-[-0.01em] sm:text-[16px]">
              {category.name}
            </span>
            <code
              title={category.slug}
              className="hidden truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)] sm:inline-block"
            >
              {category.slug}
            </code>
          </div>

          <div className="mt-1 flex items-center gap-2 truncate text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            {category.parentCategoryId ? (
              <>
                <ChevronsRight className="h-3 w-3 shrink-0 opacity-60" />
                <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] opacity-70">
                  under
                </span>
                <span className="truncate">{parentName ?? "(parent)"}</span>
              </>
            ) : (
              <>
                <GitBranch className="h-3 w-3 shrink-0 opacity-60" />
                <span className="font-mono text-[10.5px] uppercase tracking-[0.14em] opacity-70">
                  root
                </span>
              </>
            )}
            {category.description && (
              <>
                <span className="opacity-40">·</span>
                <span className="truncate" title={category.description}>
                  {category.description}
                </span>
              </>
            )}
          </div>
        </div>

        <div className="hidden min-w-[110px] text-right tabular-nums sm:block">
          <div className="font-mono text-[10.5px] uppercase tracking-[0.14em] text-[var(--color-foreground)]/85">
            {formatDateMono(category.createdAtUtc)}
          </div>
          <div className="mt-0.5 text-[10.5px] text-[var(--color-muted-foreground)]">
            {formatRelative(category.createdAtUtc)}
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
        <RowAction label={`Edit ${category.name}`} onClick={onEdit}>
          <Pencil className="h-3.5 w-3.5" />
        </RowAction>
        <RowAction label={`Delete ${category.name}`} onClick={onDelete} tone="danger">
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
  initial,
  size,
  isRoot,
}: {
  initial: string;
  size: number;
  isRoot: boolean;
}) {
  const style = { width: size, height: size };
  return (
    <span
      aria-hidden
      style={style}
      className={cn(
        "relative grid shrink-0 place-items-center overflow-hidden rounded-xl",
        isRoot
          ? "bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.25)]"
          : "bg-[var(--color-surface-2)] ring-1 ring-inset ring-[var(--color-border)]",
        "shadow-[var(--highlight-top)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "absolute -right-3 -top-3 h-8 w-8 rounded-full blur-md",
          isRoot
            ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            : "bg-[oklch(from_var(--color-foreground)_l_c_h_/_0.06)]",
        )}
      />
      <span
        className={cn(
          "text-display relative font-semibold leading-none tracking-[-0.02em]",
          isRoot
            ? "text-[var(--color-primary)]"
            : "text-[var(--color-muted-foreground)]",
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

function CategoryEditorDialog({
  state,
  onClose,
  tree,
}: {
  state: EditorState;
  onClose: () => void;
  tree: CategoryTreeNodeDto[];
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const category = state.mode === "edit" ? state.category : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: category?.name ?? "",
      description: category?.description ?? "",
      parentCategoryId: category?.parentCategoryId ?? "",
    }),
    [
      category?.id,
      category?.name,
      category?.description,
      category?.parentCategoryId,
    ],
  );

  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [parentCategoryId, setParentCategoryId] = useState(initial.parentCategoryId);

  useEffect(() => {
    if (isOpen) {
      setName(initial.name);
      setDescription(initial.description);
      setParentCategoryId(initial.parentCategoryId);
    }
  }, [
    isOpen,
    initial.name,
    initial.description,
    initial.parentCategoryId,
  ]);

  const slugPreview = useMemo(() => slugify(name) || "—", [name]);

  const parentOptions = useMemo<FlatNode[]>(() => {
    const flat = flattenTree(tree);
    if (!category) return flat;
    return flat.filter(
      (n) => n.id !== category.id && !n.ancestorIds.includes(category.id),
    );
  }, [tree, category]);

  const parentName = useMemo(() => {
    if (!parentCategoryId) return null;
    return parentOptions.find((n) => n.id === parentCategoryId)?.name ?? null;
  }, [parentCategoryId, parentOptions]);

  const createMutation = useMutation({
    mutationFn: (input: CreateCategoryInput) => createCategory(input),
    onSuccess: () => {
      toast.success("Category created");
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateCategoryInput) => updateCategory(input),
    onSuccess: () => {
      toast.success("Category updated");
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
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
      parentCategoryId: parentCategoryId || null,
    };
    if (state.mode === "edit" && category) {
      updateMutation.mutate({ categoryId: category.id, ...payload });
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
              {category ? "Edit entry" : "New entry"}
            </span>
            <DialogTitle>{category ? "Edit category" : "Add a category"}</DialogTitle>
            <DialogDescription>
              {category
                ? `Update details for ${category.name}. The slug is re-derived from the name.`
                : "Add a category to your catalog. The slug is generated automatically from the name."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <RowPreview
              name={trimmedName}
              slug={slugPreview}
              description={description}
              parentName={parentName}
            />

            <Field id="category-name" label="Name" required>
              <Input
                id="category-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Outdoor"
                autoFocus
                required
                maxLength={128}
              />
            </Field>

            <Field
              id="category-slug"
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
              id="category-parent"
              label="Parent"
              hint="Optional. Leave empty to make this a root category."
            >
              <Combobox
                id="category-parent"
                label="Parent category"
                placeholder="No parent (root)"
                value={parentCategoryId || null}
                onChange={(v) => setParentCategoryId(v ?? "")}
                options={parentOptions.map((opt) => ({
                  value: opt.id,
                  label: opt.name,
                  // Tree depth indicator — keeps hierarchy readable inside the popover.
                  prefix:
                    opt.depth > 0 ? (
                      <span
                        aria-hidden
                        className="font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]"
                        style={{ paddingLeft: `${(opt.depth - 1) * 12}px` }}
                      >
                        ↳
                      </span>
                    ) : (
                      <span
                        aria-hidden
                        className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/60"
                      >
                        root
                      </span>
                    ),
                }))}
                searchable
                clearable
                emptyOptionLabel="No parent (root)"
              />
            </Field>

            <Field
              id="category-description"
              label="Description"
              hint="Shown on category browse pages."
            >
              <textarea
                id="category-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                maxLength={1024}
                className={cn(
                  "flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm",
                  "placeholder:text-[var(--color-muted-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
                )}
                placeholder="Gear for the great outdoors."
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
              {isPending ? "Saving…" : category ? "Save changes" : "Add category"}
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
  parentName,
}: {
  name: string;
  slug: string;
  description: string;
  parentName: string | null;
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
        <span
          aria-hidden
          className="grid h-11 w-11 place-items-center rounded-xl bg-[linear-gradient(135deg,oklch(from_var(--color-primary)_l_c_h_/_0.22),oklch(from_var(--color-primary)_l_c_h_/_0.04))] text-[15px] font-semibold text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.22)]"
        >
          {initial}
        </span>
        <div className="min-w-0 flex-1">
          <div
            className={cn(
              "truncate text-[15px] font-semibold leading-tight tracking-[-0.005em]",
              !name.trim() && "text-[var(--color-muted-foreground)]",
            )}
          >
            {name.trim() || "Category name"}
          </div>
          <div className="mt-0.5 flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]">
            <code className="truncate font-mono text-[10.5px] uppercase tracking-[0.12em]">
              {slug}
            </code>
            {parentName ? (
              <>
                <span className="opacity-40">·</span>
                <ChevronsRight className="h-3 w-3 opacity-60" />
                <span className="truncate">under {parentName}</span>
              </>
            ) : (
              <>
                <span className="opacity-40">·</span>
                <Folder className="h-3 w-3 opacity-60" />
                <span>root</span>
              </>
            )}
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

function DeleteCategoryDialog({
  state,
  onClose,
}: {
  state: EditorState;
  onClose: () => void;
}) {
  const isOpen = state.mode === "delete";
  const category = state.mode === "delete" ? state.category : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    onSuccess: () => {
      toast.success("Category deleted");
      queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
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
          <DialogTitle>Delete category</DialogTitle>
          <DialogDescription>
            This permanently removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{category?.name}</span>{" "}
            <span className="opacity-70">
              ({category && formatDate(category.createdAtUtc)})
            </span>
            . Categories with child categories cannot be deleted — move or delete the
            children first.
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
            onClick={() => category && deleteMutation.mutate(category.id)}
            disabled={deleteMutation.isPending || !category}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete category"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
