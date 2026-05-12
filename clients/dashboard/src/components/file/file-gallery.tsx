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

type Props = {
  files: FileAssetDto[] | undefined;
  isLoading?: boolean;
  /** When set, queries with this key are invalidated after delete. */
  queryKey?: readonly unknown[];
  /** Hide the delete control (e.g. read-only consumers like ticket attachments). */
  readOnly?: boolean;
  className?: string;
};

/**
 * FileGallery — grid of finalized file cards with mime-aware iconography,
 * an inline download CTA (which mints a fresh presigned GET each click) and
 * an optional delete CTA backed by the soft-delete endpoint.
 */
export function FileGallery({ files, isLoading, queryKey, readOnly, className }: Props) {
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
  return (
    <ul className={cn("grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3", className)}>
      {files.map((f) => (
        <FileCard key={f.id} file={f} queryKey={queryKey} readOnly={readOnly} />
      ))}
    </ul>
  );
}

function FileCard({
  file,
  queryKey,
  readOnly,
}: {
  file: FileAssetDto;
  queryKey?: readonly unknown[];
  readOnly?: boolean;
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

  return (
    <li
      className={cn(
        "group relative flex items-start gap-3 rounded-2xl border bg-[var(--color-surface-2)] p-3 transition-colors",
        "border-[var(--color-border-strong)] hover:bg-[var(--color-surface-3)]",
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
          onClick={handleDownload}
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
            onClick={() => deleteMutation.mutate()}
            disabled={deleteMutation.isPending}
            title="Move to trash"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </div>
    </li>
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
