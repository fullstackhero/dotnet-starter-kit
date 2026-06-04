import { useEffect, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Image as ImageIcon, Loader2, Upload, X, Link as LinkIcon } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";
import { useFileUpload, formatBytes } from "@/hooks/use-file-upload";
import { getFileMetadata, Visibility } from "@/api/files";
import { ApiRequestError } from "@/lib/api-client";

type Props = {
  /** Current image URL (or empty). The component is fully controlled. */
  value: string;
  onChange: (next: string) => void;
  /**
   * Owner binding for the upload. The Files module's per-OwnerType IFileAccessPolicy
   * decides who can attach what. For product images, ownerType="Product" + the product id.
   */
  ownerType: string;
  ownerId?: string | null;
  /** Allowed extensions (lower-case w/ leading dot). Server enforces too. */
  allowedExtensions?: string[];
  maxBytes?: number;
  /** Visual treatment for the preview tile — "square" for products, "circle" for avatars. */
  shape?: "square" | "circle";
  className?: string;
};

const IMAGE_EXTS = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

/**
 * ImageInput — composite control that lets a user either upload a new image
 * (presigned PUT to S3/MinIO) OR paste an external URL. After a successful
 * upload the component fetches the FileAsset metadata to retrieve the durable
 * `publicUrl` and forwards it through `onChange`.
 */
export function ImageInput({
  value,
  onChange,
  ownerType,
  ownerId,
  allowedExtensions = IMAGE_EXTS,
  maxBytes = 10 * 1024 * 1024,
  shape = "square",
  className,
}: Props) {
  const [mode, setMode] = useState<"upload" | "url">("upload");
  const { upload, progress, isUploading, reset } = useFileUpload({
    ownerType,
    ownerId,
    category: "Image",
    visibility: Visibility.Public, // public so we get a durable URL we can persist on the entity
    allowedExtensions,
    maxBytes,
  });

  // After upload+finalize, fetch metadata so we get the durable publicUrl.
  const resolveUrl = useMutation({
    mutationFn: async (fileAssetId: string) => {
      const dto = await getFileMetadata(fileAssetId);
      if (!dto.publicUrl) {
        throw new Error("Server returned no publicUrl for this file.");
      }
      return dto.publicUrl;
    },
  });

  const handlePick = () => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = "image/*";
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file) return;
      try {
        const asset = await upload(file);
        const url = await resolveUrl.mutateAsync(asset.id);
        onChange(url);
        toast.success("Image uploaded");
        // Clear progress so the dropzone re-arms for another upload.
        setTimeout(reset, 1500);
      } catch (e) {
        const message =
          e instanceof ApiRequestError
            ? (e.problem?.detail ?? e.problem?.title ?? e.message)
            : e instanceof Error
              ? e.message
              : "Upload failed";
        toast.error(message);
      }
    };
    input.click();
  };

  const hasImage = value.length > 0;
  const isWorking = isUploading || resolveUrl.isPending;
  const tileClass = shape === "circle" ? "rounded-full" : "rounded-xl";

  // Show the placeholder (not a broken-image icon) when the current URL fails to
  // load — e.g. a seeded default-avatar URL that 404s. Reset on every URL change.
  const [imgFailed, setImgFailed] = useState(false);
  useEffect(() => setImgFailed(false), [value]);
  const showImage = hasImage && !imgFailed;

  return (
    <div className={cn("space-y-3", className)}>
      {/* Mode toggle */}
      <div className="flex gap-1">
        <ModeChip active={mode === "upload"} onClick={() => setMode("upload")} icon={<Upload className="h-3.5 w-3.5" />}>
          Upload
        </ModeChip>
        <ModeChip active={mode === "url"} onClick={() => setMode("url")} icon={<LinkIcon className="h-3.5 w-3.5" />}>
          Paste URL
        </ModeChip>
      </div>

      {/* Preview + controls row */}
      <div className="flex items-start gap-4">
        <div
          className={cn(
            "relative grid place-items-center overflow-hidden bg-[var(--color-muted)] ring-1 ring-inset ring-border",
            shape === "circle" ? "h-20 w-20 rounded-full" : "h-24 w-24 rounded-xl",
          )}
        >
          {showImage ? (
            <img
              src={value}
              alt=""
              onError={() => setImgFailed(true)}
              className={cn("h-full w-full object-cover", tileClass)}
            />
          ) : isWorking ? (
            <Loader2 className="h-5 w-5 animate-spin text-[var(--color-primary)]" />
          ) : (
            <ImageIcon className="h-5 w-5 text-[var(--color-muted-foreground)]" />
          )}
        </div>

        <div className="flex-1 space-y-2">
          {mode === "upload" ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button type="button" size="sm" onClick={handlePick} disabled={isWorking}>
                {isWorking
                  ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  : <Upload className="h-3.5 w-3.5" />}
                {showImage ? "Replace image" : "Choose image"}
              </Button>
              {showImage && !isWorking && (
                <Button type="button" size="sm" variant="outline" onClick={() => onChange("")}>
                  <X className="h-3.5 w-3.5" />
                  Remove
                </Button>
              )}
              {isUploading && progress && (
                <span className="text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                  {progress.percent}% · {formatBytes(progress.loaded)} / {formatBytes(progress.totalBytes)}
                </span>
              )}
            </div>
          ) : (
            <Input
              type="url"
              value={value}
              onChange={(e) => onChange(e.target.value)}
              placeholder="https://…"
              maxLength={512}
            />
          )}

          <p className="text-xs text-[var(--color-muted-foreground)]">
            {mode === "upload"
              ? `JPG/PNG/WebP/GIF · up to ${formatBytes(maxBytes)}`
              : "Direct link to an image you host elsewhere."}
          </p>
        </div>
      </div>
    </div>
  );
}

function ModeChip({
  active,
  onClick,
  icon,
  children,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex h-7 cursor-pointer items-center gap-1.5 rounded-full px-2.5 text-xs font-medium transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)]",
      )}
    >
      {icon}
      {children}
    </button>
  );
}
