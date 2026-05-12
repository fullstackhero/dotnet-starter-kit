import { useCallback, useRef, useState } from "react";
import { CloudUpload, Loader2, X, AlertCircle, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { useFileUpload, formatBytes, type UploadOptions } from "@/hooks/use-file-upload";
import type { FileAssetDto } from "@/api/files";

type Props = {
  /** Server-side category that decides allowed extensions + size cap (e.g. Image, Document, Archive). */
  options: UploadOptions;
  /** Fires after the server finalizes the upload to Available. */
  onUploaded?: (asset: FileAssetDto) => void;
  /** When false, dropzone is read-only — used during list/preview-only views. */
  disabled?: boolean;
  /** Comma-separated `accept` attribute hint for the native picker. Server enforces the real rule. */
  accept?: string;
  className?: string;
};

/**
 * FileDropzone — drag-drop + click-to-pick uploader, wired to the presigned-URL
 * flow via {@link useFileUpload}. Shows live progress, surfaces server-side
 * rejections (size/extension/quota) in-flow, and re-arms after each upload so
 * the user can keep dropping files without leaving the surface.
 */
export function FileDropzone({ options, onUploaded, disabled, accept, className }: Props) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const { upload, progress, isUploading, reset, cancel } = useFileUpload(options);

  const trigger = useCallback(() => {
    if (disabled || isUploading) return;
    inputRef.current?.click();
  }, [disabled, isUploading]);

  const handleFiles = useCallback(
    async (list: FileList | null) => {
      if (!list || list.length === 0) return;
      const file = list[0];
      if (!file) return;
      try {
        const asset = await upload(file);
        onUploaded?.(asset);
      } catch {
        // Surfaced via progress.status === "error" — nothing else to do here.
      } finally {
        if (inputRef.current) inputRef.current.value = "";
      }
    },
    [upload, onUploaded],
  );

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      setDragOver(false);
      if (disabled || isUploading) return;
      void handleFiles(e.dataTransfer.files);
    },
    [disabled, isUploading, handleFiles],
  );

  const status = progress?.status;
  const isError = status === "error";
  const isDone = status === "done";

  return (
    <div className={cn("space-y-3", className)}>
      <div
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-disabled={disabled}
        onClick={trigger}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            trigger();
          }
        }}
        onDragOver={(e) => {
          e.preventDefault();
          if (!disabled && !isUploading) setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={onDrop}
        className={cn(
          "relative isolate flex flex-col items-center justify-center gap-3",
          "rounded-2xl border border-dashed px-6 py-10 text-center transition-colors duration-[var(--duration-default)]",
          "outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
          disabled
            ? "cursor-not-allowed border-[var(--color-border)] bg-[var(--color-surface-2)] opacity-60"
            : "cursor-pointer border-[var(--color-border-strong)] bg-[var(--color-surface-2)] hover:bg-[var(--color-surface-3)]",
          dragOver
            && "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.06)]",
          isError && "border-[var(--color-destructive)]",
        )}
      >
        {/* Soft brand halo — visible on drag */}
        {dragOver && (
          <span
            aria-hidden
            className="pointer-events-none absolute inset-0 -z-10 rounded-2xl"
            style={{
              backgroundImage:
                "radial-gradient(50% 60% at 50% 50%, oklch(from var(--color-primary) l c h / 0.10), transparent 70%)",
            }}
          />
        )}

        <input
          ref={inputRef}
          type="file"
          className="sr-only"
          accept={accept}
          onChange={(e) => void handleFiles(e.target.files)}
          disabled={disabled || isUploading}
        />

        <DropzoneIcon status={status} />

        <div className="space-y-1">
          <p className="text-sm font-medium tracking-tight text-[var(--color-foreground)]">
            {captionFor(status, progress?.fileName)}
          </p>
          <p className="text-xs text-[var(--color-muted-foreground)]">
            {detailFor(status, progress, options)}
          </p>
        </div>

        {isUploading && progress && (
          <ProgressBar percent={progress.percent} loaded={progress.loaded} total={progress.totalBytes} />
        )}

        {(isError || isDone) && (
          <div className="flex items-center gap-2 pt-1">
            <Button
              size="sm"
              variant="outline"
              onClick={(e) => {
                e.stopPropagation();
                reset();
              }}
            >
              <X className="h-3.5 w-3.5" />
              {isError ? "Dismiss" : "Upload another"}
            </Button>
          </div>
        )}

        {isUploading && (
          <Button
            size="sm"
            variant="outline"
            className="absolute right-3 top-3"
            onClick={(e) => {
              e.stopPropagation();
              cancel();
              reset();
            }}
          >
            Cancel
          </Button>
        )}
      </div>
    </div>
  );
}

function DropzoneIcon({ status }: { status: string | undefined }) {
  if (status === "uploading" || status === "preparing" || status === "finalizing") {
    return (
      <span className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--color-surface-3)] ring-1 ring-[var(--color-border-strong)]">
        <Loader2 className="h-5 w-5 animate-spin text-[var(--color-primary)]" />
      </span>
    );
  }
  if (status === "done") {
    return (
      <span className="grid h-12 w-12 place-items-center rounded-xl bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] ring-1 ring-[oklch(from_var(--color-primary)_l_c_h_/_0.30)]">
        <CheckCircle2 className="h-5 w-5 text-[var(--color-primary)]" />
      </span>
    );
  }
  if (status === "error") {
    return (
      <span className="grid h-12 w-12 place-items-center rounded-xl bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] ring-1 ring-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)]">
        <AlertCircle className="h-5 w-5 text-[var(--color-destructive)]" />
      </span>
    );
  }
  return (
    <span className="grid h-12 w-12 place-items-center rounded-xl bg-[var(--color-surface-3)] ring-1 ring-[var(--color-border-strong)]">
      <CloudUpload className="h-5 w-5 text-[var(--color-muted-foreground)]" />
    </span>
  );
}

function captionFor(status: string | undefined, fileName?: string): string {
  switch (status) {
    case "preparing":
      return "Preparing upload…";
    case "uploading":
      return `Uploading ${fileName ?? "…"}`;
    case "finalizing":
      return "Finalizing…";
    case "done":
      return `Uploaded ${fileName ?? "file"}`;
    case "error":
      return "Upload failed";
    default:
      return "Drop a file or click to browse";
  }
}

function detailFor(
  status: string | undefined,
  progress: ReturnType<typeof useFileUpload>["progress"],
  options: UploadOptions,
): string {
  if (status === "error" && progress?.error) return progress.error;
  if (status === "done" && progress?.fileAsset) {
    return `${progress.fileAsset.contentType} · ${formatBytes(progress.fileAsset.sizeBytes)}`;
  }
  if (status === "uploading" && progress) {
    return `${formatBytes(progress.loaded)} of ${formatBytes(progress.totalBytes)}`;
  }
  if (options.allowedExtensions && options.allowedExtensions.length > 0) {
    const ext = options.allowedExtensions.join(", ");
    const cap = options.maxBytes ? ` · up to ${formatBytes(options.maxBytes)}` : "";
    return `Allowed: ${ext}${cap}`;
  }
  return typeof options.category === "string" ? options.category : "Drop a file";
}

function ProgressBar({ percent, loaded, total }: { percent: number; loaded: number; total: number }) {
  return (
    <div className="w-full max-w-sm space-y-1.5">
      <div className="relative h-1.5 w-full overflow-hidden rounded-full bg-[var(--color-surface-3)]">
        <span
          aria-hidden
          className="absolute inset-y-0 left-0 rounded-full bg-[var(--color-primary)] transition-[width] duration-200"
          style={{ width: `${percent}%` }}
        />
      </div>
      <div className="flex items-center justify-between text-[10.5px] font-mono uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        <span>{percent}%</span>
        <span>
          {formatBytes(loaded)} / {formatBytes(total)}
        </span>
      </div>
    </div>
  );
}
