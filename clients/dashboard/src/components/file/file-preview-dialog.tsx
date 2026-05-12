import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Download,
  ExternalLink,
  FileArchive,
  File as FileIcon,
  FileImage,
  FileText,
  Loader2,
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
import {
  FileAssetStatus,
  Visibility,
  getFileDownloadUrl,
  getFileMetadata,
  type FileAssetDto,
} from "@/api/files";
import { ApiRequestError } from "@/lib/api-client";
import { formatBytes } from "@/hooks/use-file-upload";
import { cn } from "@/lib/cn";

type Props = {
  fileAssetId: string | null;
  /** Seed the dialog with what the list already knows so we paint immediately while the
   *  fresh metadata refetch is in flight. */
  initial?: FileAssetDto;
  onClose: () => void;
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
export function FilePreviewDialog({ fileAssetId, initial, onClose }: Props) {
  const open = fileAssetId !== null;

  // Re-fetch metadata each open: the list may be stale w.r.t. status/visibility, and
  // the publicUrl/presigned URL must be current for inline rendering.
  const metaQuery = useQuery({
    queryKey: ["files", "meta", fileAssetId],
    queryFn: () => getFileMetadata(fileAssetId!),
    enabled: open,
    initialData: initial && initial.id === fileAssetId ? initial : undefined,
  });

  // Private files need a presigned GET to render — fetch lazily once we know visibility.
  const downloadQuery = useQuery({
    queryKey: ["files", "download", fileAssetId],
    queryFn: () => getFileDownloadUrl(fileAssetId!),
    enabled: open && metaQuery.data?.visibility === Visibility.Private,
    staleTime: 60 * 1000, // 1 min — short, since presigned URLs have a TTL of their own
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (o ? undefined : onClose())}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 truncate">
            <MimeIcon contentType={metaQuery.data?.contentType ?? "application/octet-stream"} />
            <span className="truncate">{metaQuery.data?.originalFileName ?? "File"}</span>
          </DialogTitle>
          <DialogDescription>
            {metaQuery.data ? `${metaQuery.data.contentType} · ${formatBytes(metaQuery.data.sizeBytes)}` : " "}
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
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
              <Preview file={metaQuery.data} downloadUrl={downloadQuery.data?.url} />
              <MetadataPanel file={metaQuery.data} />
            </>
          )}
        </DialogBody>

        <DialogFooter>
          {metaQuery.data && (metaQuery.data.publicUrl || downloadQuery.data?.url) && (
            <a
              href={metaQuery.data.publicUrl ?? downloadQuery.data?.url ?? "#"}
              target="_blank"
              rel="noopener noreferrer"
              download={metaQuery.data.originalFileName}
              className="inline-flex"
            >
              <Button size="sm" variant="outline">
                <Download className="h-3.5 w-3.5" />
                Download
              </Button>
            </a>
          )}
          <DialogClose asChild>
            <Button size="sm">Close</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Preview({ file, downloadUrl }: { file: FileAssetDto; downloadUrl?: string }) {
  // For private files we need the presigned URL; for public we use the durable URL.
  const url = file.publicUrl ?? downloadUrl ?? null;
  const isAvailable = file.status === FileAssetStatus.Available;

  if (!isAvailable) {
    return (
      <div className="rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-4 py-8 text-center">
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

  if (file.contentType.startsWith("image/")) {
    return (
      <div className="grid place-items-center overflow-hidden rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-1)]">
        <img
          src={url}
          alt={file.originalFileName}
          className="max-h-[60vh] w-auto object-contain"
        />
      </div>
    );
  }

  if (file.contentType === "application/pdf") {
    return (
      <iframe
        src={url}
        title={file.originalFileName}
        className="h-[60vh] w-full rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-1)]"
      />
    );
  }

  if (file.contentType.startsWith("text/")) {
    return <TextPreview url={url} />;
  }

  return (
    <div className="flex flex-col items-center gap-3 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] px-4 py-10 text-center">
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
    <pre className="max-h-[55vh] overflow-auto rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-1)] p-4 font-mono text-xs leading-relaxed text-[var(--color-foreground)]">
      {text}
    </pre>
  );
}

function MetadataPanel({ file }: { file: FileAssetDto }) {
  const rows: Array<[string, React.ReactNode]> = [
    ["File ID", <code className="font-mono text-[11px]">{file.id}</code>],
    ["Owner type", file.ownerType],
    ["Owner ID", file.ownerId ?? "—"],
    ["Content type", file.contentType],
    ["Size", formatBytes(file.sizeBytes)],
    ["Visibility", file.visibility === Visibility.Public ? "Public" : "Private"],
    ["Status", statusLabel(file.status)],
    ["Created", new Date(file.createdAtUtc).toLocaleString()],
  ];
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-surface-2)] p-4 sm:grid-cols-2">
      {rows.map(([k, v]) => (
        <div key={k} className="flex items-baseline justify-between gap-3 sm:block">
          <dt className="font-mono text-[10.5px] uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
            {k}
          </dt>
          <dd className="truncate text-sm text-[var(--color-foreground)]" title={typeof v === "string" ? v : undefined}>
            {v}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function statusLabel(status: number): string {
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
    <div className="grid h-[40vh] place-items-center rounded-xl border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-2)]">
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
