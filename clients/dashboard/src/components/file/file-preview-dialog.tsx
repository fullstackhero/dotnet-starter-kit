import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  Download,
  ExternalLink,
  FileArchive,
  File as FileIcon,
  FileImage,
  FileText,
  Loader2,
  Trash2,
} from "lucide-react";
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
import { Button } from "@/components/ui/button";
import { ErrorBand } from "@/components/list";
import { Switch } from "@/components/ui/switch";
import {
  FileAssetStatus,
  Visibility,
  changeFileVisibility,
  deleteFile,
  getFileDownloadUrl,
  getFileMetadata,
  type FileAssetDto,
  type FileAssetStatusValue,
} from "@/api/files";
import { useAuth } from "@/auth/use-auth";
import { useUserDisplay } from "@/lib/use-user-display";
import { ApiRequestError } from "@/lib/api-client";
import { formatBytes } from "@/hooks/use-file-upload";
import { cn } from "@/lib/cn";

type Props = {
  fileAssetId: string | null;
  /** Seed the dialog with what the list already knows so we paint immediately while the
   *  fresh metadata refetch is in flight. */
  initial?: FileAssetDto;
  /** Called when the dialog is dismissed (close button, overlay click, Esc). */
  onClose: () => void;
  /** Called after a successful delete. The parent should invalidate its list query
   *  and clear its `selectedFileId` state to dismiss the dialog. Omit to render
   *  the dialog as read-only (no delete affordance). */
  onDeleted?: (fileAssetId: string) => void;
};

/**
 * FilePreviewDialog — opens to a metadata-first card with an inline preview when
 * the content type is renderable in-browser:
 *   - image/*  → <img> with object-contain
 *   - application/pdf → <iframe>
 *   - text/* (small) → fetched + rendered as <pre>
 *   - everything else → a download CTA
 *
 * For private files, the URL is minted on demand via /url; public files use the
 * durable publicUrl shipped on the metadata DTO.
 */
export function FilePreviewDialog({ fileAssetId, initial, onClose, onDeleted }: Props) {
  const open = fileAssetId !== null;
  const { user } = useAuth();
  const [confirmingDelete, setConfirmingDelete] = useState(false);

  // Reset the inline confirm state every time the dialog closes so the next file
  // opens cleanly in its non-armed state.
  useEffect(() => {
    if (!open) setConfirmingDelete(false);
  }, [open]);

  const deleteMutation = useMutation({
    mutationFn: () => deleteFile(fileAssetId!),
    onSuccess: () => {
      toast.success("File deleted");
      onDeleted?.(fileAssetId!);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Delete failed", { description: detail });
      setConfirmingDelete(false);
    },
  });

  // Re-fetch metadata each open: the list may be stale w.r.t. status/visibility, and
  // the publicUrl/presigned URL must be current for inline rendering.
  const metaQuery = useQuery({
    queryKey: ["files", "meta", fileAssetId],
    queryFn: () => getFileMetadata(fileAssetId!),
    enabled: open,
    initialData: initial && initial.id === fileAssetId ? initial : undefined,
  });

  const visibilityMutation = useMutation({
    mutationFn: (next: typeof Visibility[keyof typeof Visibility]) =>
      changeFileVisibility(fileAssetId!, next),
    onSuccess: (dto) => {
      metaQuery.refetch();
      toast.success(
        dto.visibility === Visibility.Public
          ? "File is now public to your tenant"
          : "File is now private",
      );
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : (err as Error).message;
      toast.error("Visibility change failed", { description: detail });
    },
  });

  // Caller is allowed to mutate (delete, change visibility) only if they uploaded
  // the file. The server enforces the same rule via IFileAccessPolicy, but gating
  // the UI here avoids rendering buttons that would 403 on click.
  const isUploader =
    !!metaQuery.data &&
    !!user?.id &&
    metaQuery.data.createdByUserId === user.id;

  // Private files need a presigned GET to render — fetch lazily once we know visibility.
  // Ask for ?inline=true so the URL carries Content-Disposition: inline, letting the
  // browser PDF viewer / image renderer show the file in place instead of downloading.
  //
  // staleTime: 0 + gcTime: 0 force a fresh URL on every modal open. The presigned URL has
  // a TTL of its own (see Files:DownloadUrlTtlMinutes); reusing a cached URL from a previous
  // open across sessions risks serving an expired URL that the <img> fails on silently.
  const downloadQuery = useQuery({
    queryKey: ["files", "download", fileAssetId, "inline"],
    queryFn: () => getFileDownloadUrl(fileAssetId!, { inline: true }),
    enabled: open && metaQuery.data?.visibility === Visibility.Private,
    staleTime: 0,
    gcTime: 0,
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (o ? undefined : onClose())}>
      <DialogContent className="flex max-h-[90dvh] max-w-3xl flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 truncate">
            <MimeIcon contentType={metaQuery.data?.contentType ?? "application/octet-stream"} />
            <span className="truncate">{metaQuery.data?.originalFileName ?? "File"}</span>
          </DialogTitle>
          {metaQuery.data && (
            <DialogDescription>
              {metaQuery.data.contentType} · {formatBytes(metaQuery.data.sizeBytes)}
            </DialogDescription>
          )}
        </DialogHeader>

        <DialogBody className="min-h-0 flex-1 space-y-4 overflow-y-auto">
          {metaQuery.isError ? (
            <ErrorBand
              message={
                metaQuery.error instanceof ApiRequestError
                  ? (metaQuery.error.problem?.detail ?? metaQuery.error.message)
                  : "Couldn't load file metadata."
              }
            />
          ) : !metaQuery.data ? (
            <PreviewSkeleton />
          ) : (
            <>
              <Preview
                file={metaQuery.data}
                downloadUrl={downloadQuery.data?.url}
                onUrlError={() => void downloadQuery.refetch()}
              />
              <MetadataPanel
                file={metaQuery.data}
                isUploader={isUploader}
                onChangeVisibility={(next) => visibilityMutation.mutate(next)}
                visibilityPending={visibilityMutation.isPending}
              />
            </>
          )}
        </DialogBody>

        <DialogFooter className="flex-row sm:flex-row sm:justify-between">
          {/* Left cluster — destructive action with inline confirm. Only the
              uploader sees this; viewers of a shared file get no destructive
              affordance. The two-step "Delete → Confirm delete" keeps the
              modal flow on the same surface; opening a second dialog over a
              preview would feel layered for what's still a one-click action. */}
          {onDeleted && metaQuery.data && fileAssetId && isUploader ? (
            confirmingDelete ? (
              <div className="flex items-center gap-2">
                <span className="text-[12px] text-[var(--color-muted-foreground)]">
                  Delete this file?
                </span>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => setConfirmingDelete(false)}
                  disabled={deleteMutation.isPending}
                >
                  Cancel
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => deleteMutation.mutate()}
                  disabled={deleteMutation.isPending}
                >
                  {deleteMutation.isPending ? (
                    <Loader2 className="size-3.5 animate-spin" />
                  ) : (
                    <Trash2 className="size-3.5" />
                  )}
                  {deleteMutation.isPending ? "Deleting…" : "Confirm delete"}
                </Button>
              </div>
            ) : (
              <Button
                size="sm"
                variant="outline"
                onClick={() => setConfirmingDelete(true)}
                className="text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)] hover:text-[var(--color-destructive)]"
              >
                <Trash2 className="size-3.5" />
                Delete
              </Button>
            )
          ) : (
            <span aria-hidden />
          )}

          {/* Right cluster — primary actions */}
          <div className="flex items-center gap-2">
            {metaQuery.data && metaQuery.data.status === FileAssetStatus.Available && (
              <DownloadButton file={metaQuery.data} />
            )}
            <DialogClose asChild>
              <Button size="sm">Close</Button>
            </DialogClose>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Preview({
  file,
  downloadUrl,
  onUrlError,
}: {
  file: FileAssetDto;
  downloadUrl?: string;
  onUrlError: () => void;
}) {
  // For private files we need the presigned URL; for public we use the durable URL.
  const url = file.publicUrl ?? downloadUrl ?? null;
  const isAvailable = file.status === FileAssetStatus.Available;
  const [errored, setErrored] = useState(false);

  if (!isAvailable) {
    return (
      <div className="rounded-xl border border-border bg-[var(--color-muted)] px-4 py-8 text-center">
        <p className="text-sm text-[var(--color-muted-foreground)]">
          {file.status === FileAssetStatus.PendingUpload
            ? "Upload not yet finalized."
            : "This file is quarantined and cannot be previewed."}
        </p>
      </div>
    );
  }

  if (!url) {
    return <PreviewSkeleton />;
  }

  if (errored) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-[var(--color-muted)] px-4 py-10 text-center">
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Preview link expired or unreachable.
        </p>
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            setErrored(false);
            onUrlError();
          }}
        >
          Retry
        </Button>
      </div>
    );
  }

  if (file.contentType.startsWith("image/")) {
    return (
      <div className="grid min-h-[30vh] place-items-center overflow-hidden rounded-xl border border-border bg-[var(--color-muted)]">
        <img
          src={url}
          alt={file.originalFileName}
          className="max-h-[48vh] w-auto object-contain"
          onError={() => setErrored(true)}
        />
      </div>
    );
  }

  if (file.contentType === "application/pdf") {
    return (
      <iframe
        src={url}
        title={file.originalFileName}
        className="h-[60vh] w-full rounded-xl border border-border bg-[var(--color-muted)]"
      />
    );
  }

  if (file.contentType.startsWith("text/")) {
    return <TextPreview url={url} />;
  }

  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border border-border bg-[var(--color-muted)] px-4 py-10 text-center">
      <FileIcon className="h-8 w-8 text-[var(--color-muted-foreground)]" />
      <p className="text-sm text-[var(--color-muted-foreground)]">
        Preview not available for this file type.
      </p>
      <a href={url} target="_blank" rel="noopener noreferrer" download={file.originalFileName}>
        <Button size="sm" variant="outline">
          <ExternalLink className="h-3.5 w-3.5" />
          Open in new tab
        </Button>
      </a>
    </div>
  );
}

function TextPreview({ url }: { url: string }) {
  const [text, setText] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const t = await res.text();
        if (!cancelled) setText(t.slice(0, 64 * 1024)); // cap at 64 KiB to keep the modal snappy
      } catch (e) {
        if (!cancelled) setErr(e instanceof Error ? e.message : "Failed to fetch");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [url]);

  if (err) {
    return <ErrorBand message={`Couldn't fetch text body: ${err}`} />;
  }
  if (text === null) {
    return <PreviewSkeleton />;
  }
  return (
    <pre className="max-h-[55vh] overflow-auto rounded-xl border border-border bg-[var(--color-muted)] p-4 font-mono text-xs leading-relaxed text-[var(--color-foreground)]">
      {text}
    </pre>
  );
}

function MetadataPanel({
  file,
  isUploader,
  onChangeVisibility,
  visibilityPending,
}: {
  file: FileAssetDto;
  isUploader: boolean;
  onChangeVisibility: (next: typeof Visibility[keyof typeof Visibility]) => void;
  visibilityPending: boolean;
}) {
  // Uploader name resolved via the existing identity cache. For files older than
  // the createdByUserId rollout this comes back empty — fall back to a hyphen so
  // the row reads cleanly rather than as a broken loader.
  const uploader = useUserDisplay(file.createdByUserId || null);
  const uploaderLabel = file.createdByUserId
    ? uploader.loading
      ? "Loading…"
      : uploader.name
    : "—";

  const rows: Array<[string, React.ReactNode, string?]> = [
    ["File ID", <code className="font-mono text-[11px]">{file.id}</code>, file.id],
    ["Owner type", file.ownerType, file.ownerType],
    ["Uploaded by", uploaderLabel, uploaderLabel],
    ["Content type", file.contentType, file.contentType],
    ["Size", formatBytes(file.sizeBytes), undefined],
    ["Status", statusLabel(file.status), undefined],
    ["Created", new Date(file.createdAtUtc).toLocaleString(), undefined],
  ];

  const isPublic = file.visibility === Visibility.Public;

  return (
    <div className="space-y-3 rounded-xl border border-border bg-[var(--color-muted)] p-4">
      {/* Visibility row — switch when the caller is the uploader, static label
          otherwise. Public state gets a small "visible to the tenant" hint so
          the consequence is clear before flipping. */}
      <div className="flex items-start justify-between gap-3 border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pb-3">
        <div>
          <dt className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Visibility
          </dt>
          <dd
            className={cn(
              "mt-0.5 text-sm font-medium",
              isPublic ? "text-[var(--color-primary)]" : "text-[var(--color-foreground)]",
            )}
          >
            {isPublic ? "Public" : "Private"}
          </dd>
          <p className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]">
            {isPublic
              ? "Everyone in your tenant can find this file under Shared."
              : "Only you can preview or download this file."}
          </p>
        </div>
        {isUploader ? (
          <Switch
            checked={isPublic}
            onCheckedChange={(checked) =>
              onChangeVisibility(checked ? Visibility.Public : Visibility.Private)
            }
            disabled={visibilityPending}
            aria-label="Toggle public visibility"
          />
        ) : (
          <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
            Read-only
          </span>
        )}
      </div>

      <dl className="grid grid-cols-1 gap-x-6 gap-y-2 sm:grid-cols-2">
        {rows.map(([k, v, titleText]) => (
          <div key={k} className="flex items-baseline justify-between gap-3 sm:block">
            <dt className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              {k}
            </dt>
            <dd className="truncate text-sm text-[var(--color-foreground)]" title={titleText}>
              {v}
            </dd>
          </div>
        ))}
      </dl>
    </div>
  );
}

// Click-to-download. Mints a fresh attachment-disposition presigned URL each click for
// private files so we don't reuse the inline URL the iframe is consuming. For public
// files there's no inline/attachment distinction — both buttons use the same publicUrl.
function DownloadButton({ file }: { file: FileAssetDto }) {
  const [busy, setBusy] = useState(false);
  const handle = async () => {
    if (busy) return;
    setBusy(true);
    try {
      const url = file.visibility === Visibility.Public && file.publicUrl
        ? file.publicUrl
        : (await getFileDownloadUrl(file.id, { inline: false })).url;
      // Triggering navigation in a new tab; the response carries
      // Content-Disposition: attachment for private files, so the browser saves rather
      // than navigates. For public files we just open in a new tab.
      window.open(url, "_blank", "noopener,noreferrer");
    } finally {
      setBusy(false);
    }
  };
  return (
    <Button size="sm" variant="outline" onClick={handle} disabled={busy}>
      {busy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
      Download
    </Button>
  );
}

function statusLabel(status: FileAssetStatusValue): string {
  switch (status) {
    case FileAssetStatus.PendingUpload:
      return "Pending upload";
    case FileAssetStatus.Available:
      return "Available";
    case FileAssetStatus.Quarantined:
      return "Quarantined";
    default:
      return `Unknown (${status})`;
  }
}

function PreviewSkeleton() {
  return (
    <div className="grid h-[30vh] place-items-center rounded-xl border border-dashed border-border bg-[var(--color-muted)]">
      <Loader2 className="h-6 w-6 animate-spin text-[var(--color-muted-foreground)]" />
    </div>
  );
}

function MimeIcon({ contentType }: { contentType: string }) {
  const cls = cn("h-4 w-4 flex-shrink-0 text-[var(--color-muted-foreground)]");
  if (contentType.startsWith("image/")) return <FileImage className={cls} />;
  if (contentType.startsWith("application/zip") || contentType.includes("compressed")) {
    return <FileArchive className={cls} />;
  }
  if (contentType.startsWith("text/") || contentType.includes("pdf") || contentType.includes("officedocument")) {
    return <FileText className={cls} />;
  }
  return <FileIcon className={cls} />;
}
