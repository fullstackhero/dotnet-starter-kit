import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ChevronRight,
  File as FileIcon,
  FileArchive,
  FileImage,
  FileText,
  Files,
  FolderOpen,
  Search,
  Share2,
  X,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import {
  Visibility,
  listMyFiles,
  listSharedFiles,
  type FileAssetDto,
  type VisibilityValue,
} from "@/api/files";
import { FileDropzone } from "@/components/file/file-dropzone";
import { FilePreviewDialog } from "@/components/file/file-preview-dialog";
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
  ErrorBand,
  ToneIconTile,
} from "@/components/list";
import { Button } from "@/components/ui/button";
import { formatBytes } from "@/hooks/use-file-upload";
import { useUserDisplay } from "@/lib/use-user-display";
import { formatDate } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

const MY_FILES_KEY = ["files", "mine"] as const;
const SHARED_FILES_KEY = ["files", "shared"] as const;

const EXTENSION_TO_CATEGORY: Record<string, string> = {
  ".jpg": "Image",
  ".jpeg": "Image",
  ".png": "Image",
  ".webp": "Image",
  ".gif": "Image",
  ".ico": "Image",
  ".pdf": "Document",
  ".docx": "Document",
  ".xlsx": "Document",
  ".pptx": "Document",
  ".txt": "Document",
  ".csv": "Document",
  ".zip": "Archive",
};

const ALL_EXTENSIONS = Object.keys(EXTENSION_TO_CATEGORY);
const CLIENT_MAX_BYTES = 50 * 1024 * 1024;

function categoryFor(file: File): string {
  const dot = file.name.lastIndexOf(".");
  const ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
  return EXTENSION_TO_CATEGORY[ext] ?? "Document";
}

// ─────────────────────────────────────────────────────────────────────
//  Type classification — bucket every file into one of four buckets
//  based on its content type. Used by the filter chips, the mime icon
//  in each row, and the chip count badges.
// ─────────────────────────────────────────────────────────────────────

type FileKind = "image" | "document" | "archive" | "other";

function fileKind(contentType: string): FileKind {
  const ct = contentType.toLowerCase();
  if (ct.startsWith("image/")) return "image";
  if (
    ct.startsWith("text/") ||
    ct.includes("pdf") ||
    ct.includes("officedocument") ||
    ct.includes("msword") ||
    ct.includes("ms-excel") ||
    ct.includes("ms-powerpoint")
  ) {
    return "document";
  }
  if (
    ct.includes("zip") ||
    ct.includes("compressed") ||
    ct.includes("tar") ||
    ct.includes("rar") ||
    ct.includes("7z") ||
    ct.includes("gzip")
  ) {
    return "archive";
  }
  return "other";
}

function mimeIcon(contentType: string): LucideIcon {
  const k = fileKind(contentType);
  if (k === "image") return FileImage;
  if (k === "document") return FileText;
  if (k === "archive") return FileArchive;
  return FileIcon;
}

type KindFilter = "all" | FileKind;
type TabId = "mine" | "shared";

const DESKTOP_GRID_MINE = "grid-cols-[1fr_120px_120px_160px]";
const DESKTOP_GRID_SHARED = "grid-cols-[1fr_160px_120px_160px]";

// ─────────────────────────────────────────────────────────────────────
//  Page
// ─────────────────────────────────────────────────────────────────────

export function MyFilesPage() {
  void useAuth();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<TabId>("mine");
  const [searchQuery, setSearchQuery] = useState("");
  const [kindFilter, setKindFilter] = useState<KindFilter>("all");
  const [selectedFileId, setSelectedFileId] = useState<string | null>(null);

  const myFilesQuery = useQuery({
    queryKey: MY_FILES_KEY,
    queryFn: () => listMyFiles(1, 100),
  });
  const sharedFilesQuery = useQuery({
    queryKey: SHARED_FILES_KEY,
    queryFn: () => listSharedFiles(1, 100),
    enabled: tab === "shared",
    staleTime: 30_000,
  });

  const onUploaded = () => {
    void queryClient.invalidateQueries({ queryKey: MY_FILES_KEY });
    void queryClient.invalidateQueries({ queryKey: SHARED_FILES_KEY });
  };

  const allFiles = useMemo(
    () => (tab === "mine" ? myFilesQuery.data ?? [] : sharedFilesQuery.data ?? []),
    [tab, myFilesQuery.data, sharedFilesQuery.data],
  );
  const activeQuery = tab === "mine" ? myFilesQuery : sharedFilesQuery;

  // Type-bucket counts run against the unfiltered list so the chips show
  // "Images 12" even when a search has narrowed the visible rows. Search
  // matches across filename + uploader id substring.
  const kindCounts = useMemo(() => {
    const counts: Record<FileKind, number> = { image: 0, document: 0, archive: 0, other: 0 };
    for (const f of allFiles) counts[fileKind(f.contentType)] += 1;
    return counts;
  }, [allFiles]);

  const filteredFiles = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return allFiles.filter((f) => {
      if (kindFilter !== "all" && fileKind(f.contentType) !== kindFilter) return false;
      if (q && !f.originalFileName.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [allFiles, searchQuery, kindFilter]);

  const filtersActive = searchQuery.trim() !== "" || kindFilter !== "all";
  const selectedFile = selectedFileId
    ? allFiles.find((f) => f.id === selectedFileId)
    : undefined;

  const onDeletedOrVisibilityChanged = () => {
    setSelectedFileId(null);
    void queryClient.invalidateQueries({ queryKey: MY_FILES_KEY });
    void queryClient.invalidateQueries({ queryKey: SHARED_FILES_KEY });
  };

  const clearFilters = () => {
    setSearchQuery("");
    setKindFilter("all");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Files}
        title="Files"
        total={allFiles.length}
        unit="file"
        description="Drop images, documents, or archives. Uploads are private to you by default; flip a file to public from its preview to make it visible to the rest of the tenant under Shared."
      />

      {/* Tab strip */}
      <div
        role="tablist"
        aria-label="File scopes"
        className="inline-flex h-9 items-center rounded-full border border-[var(--color-border)] bg-[var(--color-card)] p-0.5"
      >
        <TabButton
          active={tab === "mine"}
          onClick={() => {
            setTab("mine");
            clearFilters();
          }}
          icon={Files}
          label="My files"
          count={myFilesQuery.data?.length}
        />
        <TabButton
          active={tab === "shared"}
          onClick={() => {
            setTab("shared");
            clearFilters();
          }}
          icon={Share2}
          label="Shared in tenant"
          count={sharedFilesQuery.data?.length}
        />
      </div>

      {tab === "mine" && (
        <FileDropzone
          options={{
            ownerType: "MyFiles",
            ownerId: null,
            category: categoryFor,
            visibility: Visibility.Private,
            allowedExtensions: ALL_EXTENSIONS,
            maxBytes: CLIENT_MAX_BYTES,
          }}
          accept={ALL_EXTENSIONS.join(",")}
          onUploaded={onUploaded}
        />
      )}

      {/* Search + filter chips. Only render once we have at least one file in
          this scope — otherwise the empty state below is what the user needs. */}
      {allFiles.length > 0 && (
        <FilterBar
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          kindFilter={kindFilter}
          onKindChange={setKindFilter}
          counts={kindCounts}
          totalCount={allFiles.length}
        />
      )}

      {activeQuery.isError ? (
        <ErrorBand
          message={
            activeQuery.error instanceof Error
              ? activeQuery.error.message
              : tab === "mine"
                ? "Couldn't load your files."
                : "Couldn't load shared files."
          }
        />
      ) : activeQuery.isLoading ? (
        <EntityListLoading
          desktopColumns={tab === "mine" ? DESKTOP_GRID_MINE : DESKTOP_GRID_SHARED}
          rows={4}
        />
      ) : allFiles.length === 0 ? (
        tab === "mine" ? (
          <EntityEmpty
            icon={FolderOpen}
            title="No files yet"
            body="Drop a file above to get started. Your uploads are private by default."
            action={
              <Button
                variant="outline"
                onClick={() => void myFilesQuery.refetch()}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Refresh
              </Button>
            }
          />
        ) : (
          <EntityEmpty
            icon={Share2}
            title="Nothing shared yet"
            body="When a teammate flips one of their files to public, it shows up here for everyone in the tenant."
            action={
              <Button
                variant="outline"
                onClick={() => void sharedFilesQuery.refetch()}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Refresh
              </Button>
            }
          />
        )
      ) : filteredFiles.length === 0 ? (
        <EntityEmpty
          icon={Search}
          title="No matches"
          body={`Nothing matches the current filter${searchQuery ? ` "${searchQuery}"` : ""}.`}
          action={
            <Button
              variant="outline"
              onClick={clearFilters}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              <X className="size-3.5" /> Reset filters
            </Button>
          }
        />
      ) : (
        <FileList
          files={filteredFiles}
          totalCount={allFiles.length}
          filtered={filtersActive}
          tab={tab}
          onOpen={setSelectedFileId}
        />
      )}

      <FilePreviewDialog
        fileAssetId={selectedFileId}
        initial={selectedFile}
        onClose={() => setSelectedFileId(null)}
        onDeleted={onDeletedOrVisibilityChanged}
      />
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────
//  Tab button — small pill, used twice in the tab strip
// ─────────────────────────────────────────────────────────────────────

function TabButton({
  active,
  onClick,
  icon: Icon,
  label,
  count,
}: {
  active: boolean;
  onClick: () => void;
  icon: LucideIcon;
  label: string;
  count?: number;
}) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={cn(
        "inline-flex h-8 cursor-pointer items-center gap-2 rounded-full px-3 text-[12.5px] font-medium",
        "transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
      )}
    >
      <Icon className="size-3.5" />
      <span>{label}</span>
      {typeof count === "number" && (
        <span
          className={cn(
            "ml-0.5 rounded-full px-1.5 py-0.5 font-mono text-[10px] tabular-nums",
            active
              ? "bg-[oklch(from_var(--color-primary-foreground)_l_c_h_/_0.20)] text-[var(--color-primary-foreground)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          )}
        >
          {count}
        </span>
      )}
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────
//  FilterBar — search input + type filter chips. Search matches against
//  filename (case-insensitive substring); type chips bucket by content-
//  type. Both compose; counts shown on the chips are scope-wide so the
//  user can see how many of each bucket exist before narrowing.
// ─────────────────────────────────────────────────────────────────────

function FilterBar({
  searchQuery,
  onSearchChange,
  kindFilter,
  onKindChange,
  counts,
  totalCount,
}: {
  searchQuery: string;
  onSearchChange: (next: string) => void;
  kindFilter: KindFilter;
  onKindChange: (next: KindFilter) => void;
  counts: Record<FileKind, number>;
  totalCount: number;
}) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <div className="relative min-w-0 flex-1">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
        <input
          type="search"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search by filename…"
          aria-label="Search files"
          className={cn(
            "h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-9",
            "text-[13px] outline-none transition-colors",
            "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]",
            "focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
          )}
        />
        {searchQuery && (
          <button
            type="button"
            onClick={() => onSearchChange("")}
            aria-label="Clear search"
            className="absolute right-2 top-1/2 grid size-6 -translate-y-1/2 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
          >
            <X className="size-3" />
          </button>
        )}
      </div>
      <div className="flex items-center gap-1 overflow-x-auto">
        <KindChip
          active={kindFilter === "all"}
          count={totalCount}
          onClick={() => onKindChange("all")}
        >
          All
        </KindChip>
        <KindChip
          active={kindFilter === "image"}
          count={counts.image}
          onClick={() => onKindChange("image")}
          disabled={counts.image === 0}
          icon={FileImage}
        >
          Images
        </KindChip>
        <KindChip
          active={kindFilter === "document"}
          count={counts.document}
          onClick={() => onKindChange("document")}
          disabled={counts.document === 0}
          icon={FileText}
        >
          Documents
        </KindChip>
        <KindChip
          active={kindFilter === "archive"}
          count={counts.archive}
          onClick={() => onKindChange("archive")}
          disabled={counts.archive === 0}
          icon={FileArchive}
        >
          Archives
        </KindChip>
        {counts.other > 0 && (
          <KindChip
            active={kindFilter === "other"}
            count={counts.other}
            onClick={() => onKindChange("other")}
            icon={FileIcon}
          >
            Other
          </KindChip>
        )}
      </div>
    </div>
  );
}

function KindChip({
  active,
  count,
  onClick,
  disabled,
  icon: Icon,
  children,
}: {
  active: boolean;
  count: number;
  onClick: () => void;
  disabled?: boolean;
  icon?: LucideIcon;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      className={cn(
        "inline-flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-full px-2.5 text-[11.5px] font-medium",
        "transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)] ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        disabled &&
          "cursor-not-allowed opacity-40 hover:bg-transparent hover:text-[var(--color-muted-foreground)]",
      )}
    >
      {Icon && <Icon className="size-3" />}
      <span>{children}</span>
      <span
        className={cn(
          "rounded-full px-1.5 py-0.5 text-[10px] font-semibold tabular-nums",
          active
            ? "bg-[oklch(from_var(--color-primary)_l_c_h_/_0.18)] text-[var(--color-primary)]"
            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
        )}
      >
        {count}
      </span>
    </button>
  );
}

// ─────────────────────────────────────────────────────────────────────
//  List + rows
// ─────────────────────────────────────────────────────────────────────

function FileList({
  files,
  totalCount,
  filtered,
  tab,
  onOpen,
}: {
  files: FileAssetDto[];
  totalCount: number;
  filtered: boolean;
  tab: TabId;
  onOpen: (id: string) => void;
}) {
  const isShared = tab === "shared";
  const desktopGrid = isShared ? DESKTOP_GRID_SHARED : DESKTOP_GRID_MINE;
  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
          {filtered
            ? `Showing ${files.length} of ${totalCount} file${totalCount === 1 ? "" : "s"}`
            : `${files.length} file${files.length === 1 ? "" : "s"}`}
        </p>
      </div>

      {/* Mobile: card list */}
      <div className="space-y-2 md:hidden">
        {files.map((file) => (
          <MobileCard
            key={file.id}
            file={file}
            showUploader={isShared}
            showVisibility={!isShared}
            onOpen={() => onOpen(file.id)}
          />
        ))}
      </div>

      {/* Desktop: table */}
      <EntityListCard className="hidden md:block">
        <EntityListHeader className={desktopGrid}>
          <span>Filename</span>
          <span>{isShared ? "Uploaded by" : "Visibility"}</span>
          <span>Size</span>
          <span>Uploaded</span>
        </EntityListHeader>
        {files.map((file, i) => (
          <DesktopRow
            key={file.id}
            file={file}
            isLast={i === files.length - 1}
            showUploader={isShared}
            showVisibility={!isShared}
            desktopGrid={desktopGrid}
            onOpen={() => onOpen(file.id)}
          />
        ))}
      </EntityListCard>
    </div>
  );
}

function VisibilityChip({ visibility }: { visibility: VisibilityValue }) {
  const isPublic = visibility === Visibility.Public;
  return (
    <EntityStatusBadge tone={isPublic ? "info" : "default"}>
      {isPublic ? "Public" : "Private"}
    </EntityStatusBadge>
  );
}

function MobileCard({
  file,
  showUploader,
  showVisibility,
  onOpen,
}: {
  file: FileAssetDto;
  showUploader: boolean;
  showVisibility: boolean;
  onOpen: () => void;
}) {
  const uploader = useUserDisplay(showUploader ? file.createdByUserId : null);
  const Icon = mimeIcon(file.contentType);
  return (
    <button
      type="button"
      onClick={onOpen}
      className={cn(
        "block w-full cursor-pointer rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
        "shadow-xs",
        "transition-colors duration-[var(--duration-fast)]",
        "hover:border-[var(--color-border-strong)] hover:bg-[oklch(from_var(--color-primary)_l_c_h_/_0.03)]",
      )}
    >
      <div className="flex items-center gap-3">
        <ToneIconTile icon={Icon} tone="primary" size="md" className="rounded-xl" />
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {file.originalFileName}
            </p>
            {showVisibility && <VisibilityChip visibility={file.visibility} />}
          </div>
          <p className="mt-0.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {formatBytes(file.sizeBytes)} · {formatDate(file.createdAtUtc)}
          </p>
          {showUploader && file.createdByUserId && (
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              by{" "}
              <span className="text-[var(--color-foreground)]">
                {uploader.loading ? "…" : uploader.name}
              </span>
            </p>
          )}
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-muted-foreground)]" />
      </div>
    </button>
  );
}

function DesktopRow({
  file,
  isLast,
  showUploader,
  showVisibility,
  desktopGrid,
  onOpen,
}: {
  file: FileAssetDto;
  isLast: boolean;
  showUploader: boolean;
  showVisibility: boolean;
  desktopGrid: string;
  onOpen: () => void;
}) {
  const uploader = useUserDisplay(showUploader ? file.createdByUserId : null);
  const Icon = mimeIcon(file.contentType);
  return (
    <EntityListRow className={desktopGrid} isLast={isLast} onClick={onOpen}>
      <div className="flex min-w-0 items-center gap-3">
        <ToneIconTile icon={Icon} tone="primary" size="md" className="rounded-xl" />
        <span
          title={file.originalFileName}
          className="truncate text-[14px] font-medium text-[var(--color-foreground)]"
        >
          {file.originalFileName}
        </span>
      </div>
      {showVisibility && (
        <span className="flex items-center">
          <VisibilityChip visibility={file.visibility} />
        </span>
      )}
      {showUploader && (
        <span
          className="truncate text-[12px] text-[var(--color-muted-foreground)]"
          title={uploader.name}
        >
          {file.createdByUserId ? (uploader.loading ? "…" : uploader.name) : "—"}
        </span>
      )}
      <span className="font-mono text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
        {formatBytes(file.sizeBytes)}
      </span>
      <span className="flex items-center justify-between gap-2 text-[12px] text-[var(--color-muted-foreground)]">
        <span>{formatDate(file.createdAtUtc)}</span>
        <ChevronRight
          aria-hidden
          className="size-4 shrink-0 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)] transition-colors group-hover:text-[var(--color-muted-foreground)]"
        />
      </span>
    </EntityListRow>
  );
}
