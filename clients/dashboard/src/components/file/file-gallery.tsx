import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Download,
  FileArchive,
  FileImage,
  FileText,
  File as FileIcon,
  Trash2,
  ExternalLink,
} from "lucide-react";
import { toast } from "sonner";
import {
  deleteFile,
  getFileDownloadUrl,
  type FileAssetDto,
  FileAssetStatus,
} from "@/api/files";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { formatBytes } from "@/hooks/use-file-upload";
import { FilePreviewDialog } from "@/components/file/file-preview-dialog";

type Props = {
  files: FileAssetDto[] | undefined;
  isLoading?: boolean;
  /** When set, queries with this key are invalidated after delete. */
  queryKey?: readonly unknown[];
  /** Hide the delete control (e.g. read-only consumers like ticket attachments). */
  readOnly?: boolean;
  /** Show section headers grouping items by inferred kind (Images / Documents / Archives / Other). */
  groupByKind?: boolean;
  className?: string;
};

type Kind = "Images" | "Documents" | "Archives" | "Other";
const KIND_ORDER: readonly Kind[] = ["Images", "Documents", "Archives", "Other"];

function kindOf(file: FileAssetDto): Kind {
  const ct = file.contentType.toLowerCase();
  if (ct.startsWith("image/")) return "Images";
  if (ct === "application/zip" || ct.includes("compressed") || ct === "application/x-zip-compressed") {
    return "Archives";
  }
  if (
    ct.startsWith("text/")
    || ct === "application/pdf"
    || ct.includes("officedocument")
    || ct === "application/msword"
    || ct === "application/vnd.ms-excel"
    || ct === "application/vnd.ms-powerpoint"
  ) {
    return "Documents";
  }
  return "Other";
}

/**
 * FileGallery — grid of finalized file cards with mime-aware iconography,
 * an inline download CTA (which mints a fresh presigned GET each click) and
 * an optional delete CTA backed by the soft-delete endpoint.
 */
export function FileGallery({ files, isLoading, queryKey, readOnly, groupByKind, className }: Props) {
  const [previewId, setPreviewId] = useState<string | null>(null);
  const previewSeed = files?.find((f) => f.id === previewId);

  if (isLoading) {
    return (
      <div className={cn("grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3", className)}>
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-28 w-full rounded-2xl" />
        ))}
      </div>
    );
  }
  if (!files || files.length === 0) {
    return null;
  }

  if (groupByKind) {
    const grouped = new Map<Kind, FileAssetDto[]>();
    for (const f of files) {
      const k = kindOf(f);
      const bucket = grouped.get(k) ?? [];
      bucket.push(f);
      grouped.set(k, bucket);
    }
    return (
      <>
        <div className={cn("space-y-6", className)}>
          {KIND_ORDER.filter((k) => grouped.has(k)).map((k) => {
            const items = grouped.get(k)!;
            return (
              <section key={k} className="space-y-3">
                <h3 className="flex items-baseline gap-2 font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  <span>{k}</span>
                  <span className="text-[var(--color-foreground)]/40">·</span>
                  <span>{items.length}</span>
                </h3>
                <div role="list" className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {items.map((f) => (
                    <FileCard
                      key={f.id}
                      file={f}
                      queryKey={queryKey}
                      readOnly={readOnly}
                      onOpen={() => setPreviewId(f.id)}
                    />
                  ))}
                </div>
              </section>
            );
          })}
        </div>
        <FilePreviewDialog
          fileAssetId={previewId}
          initial={previewSeed}
          onClose={() => setPreviewId(null)}
        />
      </>
    );
  }

  return (
    <>
      <div role="list" className={cn("grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3", className)}>
        {files.map((f) => (
          <FileCard
            key={f.id}
            file={f}
            queryKey={queryKey}
            readOnly={readOnly}
            onOpen={() => setPreviewId(f.id)}
          />
        ))}
      </div>
      <FilePreviewDialog
        fileAssetId={previewId}
        initial={previewSeed}
        onClose={() => setPreviewId(null)}
      />
    </>
  );
}

function FileCard({
  file,
  queryKey,
  readOnly,
  onOpen,
}: {
  file: FileAssetDto;
  queryKey?: readonly unknown[];
  readOnly?: boolean;
  onOpen: () => void;
}) {
  const queryClient = useQueryClient();
  const [downloading, setDownloading] = useState(false);

  const deleteMutation = useMutation({
    mutationFn: () => deleteFile(file.id),
    onSuccess: () => {
      toast.success(`${file.originalFileName} moved to trash`);
      if (queryKey) {
        void queryClient.invalidateQueries({ queryKey });
      }
    },
    onError: (e: unknown) => {
      const message =
        e instanceof ApiRequestError
          ? (e.problem?.detail ?? e.problem?.title ?? e.message)
          : "Failed to delete file";
      toast.error(message);
    },
  });

  const handleDownload = async () => {
    if (downloading) return;
    setDownloading(true);
    try {
      const { url } = await getFileDownloadUrl(file.id);
      window.open(url, "_blank", "noopener,noreferrer");
    } catch (e) {
      const message =
        e instanceof ApiRequestError
          ? (e.problem?.detail ?? e.problem?.title ?? e.message)
          : "Failed to mint download URL";
      toast.error(message);
    } finally {
      setDownloading(false);
    }
  };

  const isImage = file.contentType.startsWith("image/");
  const isAvailable = file.status === FileAssetStatus.Available;

  // The card is clickable — Download / Delete are stopPropagation'd so they don't open
  // the preview dialog.
  const handleCardKey = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      onOpen();
    }
  };

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onOpen}
      onKeyDown={handleCardKey}
      aria-label={`Preview ${file.originalFileName}`}
      className={cn(
        "group relative flex cursor-pointer items-start gap-3 rounded-2xl border bg-[var(--color-surface-2)] p-3 transition-colors",
        "border-[var(--color-border-strong)] hover:bg-[var(--color-surface-3)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
      )}
    >
      {/* Thumbnail / icon plate */}
      <div className="grid h-12 w-12 flex-shrink-0 place-items-center overflow-hidden rounded-xl bg-[var(--color-surface-3)] ring-1 ring-[var(--color-border-strong)]">
        {isImage && file.publicUrl ? (
          <img
            src={file.publicUrl}
            alt=""
            className="h-full w-full object-cover"
            loading="lazy"
          />
        ) : (
          <MimeIcon contentType={file.contentType} />
        )}
      </div>

      <div className="min-w-0 flex-1">
        <p
          className="truncate text-sm font-medium text-[var(--color-foreground)]"
          title={file.originalFileName}
        >
          {file.originalFileName}
        </p>
        <p className="text-[11px] font-mono uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]">
          {file.contentType} · {formatBytes(file.sizeBytes)}
        </p>
        {!isAvailable && (
          <p className="mt-1 text-[11px] font-mono uppercase tracking-[0.14em] text-[var(--color-destructive)]">
            {file.status === FileAssetStatus.PendingUpload ? "Pending" : "Quarantined"}
          </p>
        )}
      </div>

      <div className="flex flex-shrink-0 items-center gap-1">
        <Button
          size="icon"
          variant="ghost"
          aria-label="Download"
          onClick={(e) => {
            e.stopPropagation();
            void handleDownload();
          }}
          disabled={!isAvailable || downloading}
          title={isAvailable ? "Download" : "Not yet available"}
        >
          {file.publicUrl
            ? <ExternalLink className="h-4 w-4" />
            : <Download className="h-4 w-4" />}
        </Button>
        {!readOnly && (
          <Button
            size="icon"
            variant="ghost"
            aria-label="Delete"
            onClick={(e) => {
              e.stopPropagation();
              deleteMutation.mutate();
            }}
            disabled={deleteMutation.isPending}
            title="Move to trash"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  );
}

function MimeIcon({ contentType }: { contentType: string }) {
  if (contentType.startsWith("image/")) return <FileImage className="h-5 w-5 text-[var(--color-muted-foreground)]" />;
  if (contentType.startsWith("application/zip") || contentType.includes("compressed")) {
    return <FileArchive className="h-5 w-5 text-[var(--color-muted-foreground)]" />;
  }
  if (contentType.startsWith("text/") || contentType.includes("pdf") || contentType.includes("officedocument")) {
    return <FileText className="h-5 w-5 text-[var(--color-muted-foreground)]" />;
  }
  return <FileIcon className="h-5 w-5 text-[var(--color-muted-foreground)]" />;
}
