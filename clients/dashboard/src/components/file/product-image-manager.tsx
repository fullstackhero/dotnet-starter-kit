import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Loader2, Star, StarOff, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import {
  addProductImage,
  removeProductImage,
  setProductThumbnail,
  type ProductImageDto,
} from "@/api/catalog";
import { getFileMetadata, Visibility } from "@/api/files";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useFileUpload } from "@/hooks/use-file-upload";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const IMAGE_EXTS = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
const MAX_BYTES = 10 * 1024 * 1024;

type Props = {
  productId: string;
  images: ProductImageDto[];
  /** Cache key to invalidate after mutations (typically the product detail query). */
  invalidateKey: readonly unknown[];
  className?: string;
};

/**
 * ProductImageManager — multi-image control for the product detail page.
 *
 *   - Uploads new images via the Files presigned-upload flow (ownerType=Product), then calls
 *     addProductImage on success to attach the durable publicUrl to the product.
 *   - Renders the existing images as a grid; each tile has Set-as-cover, Remove, and click-to-preview.
 *   - Clicking an image opens a fullscreen preview modal.
 */
export function ProductImageManager({ productId, images, invalidateKey, className }: Props) {
  const { t } = useTranslation("files");
  const queryClient = useQueryClient();
  const [previewId, setPreviewId] = useState<string | null>(null);
  const [pendingRemove, setPendingRemove] = useState<ProductImageDto | null>(null);

  const { upload, progress, isUploading, reset } = useFileUpload({
    ownerType: "Product",
    ownerId: productId,
    category: "Image",
    visibility: Visibility.Public,
    allowedExtensions: IMAGE_EXTS,
    maxBytes: MAX_BYTES,
  });

  const attachMutation = useMutation({
    mutationFn: (input: { fileAssetId: string; url: string }) =>
      addProductImage(productId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: invalidateKey });
    },
    onError: (e: unknown) => {
      toast.error(extract(e, t("pim.errAttach")));
    },
  });

  const thumbnailMutation = useMutation({
    mutationFn: (imageId: string) => setProductThumbnail(productId, imageId),
    onSuccess: () => {
      toast.success(t("pim.toastCoverUpdated"));
      void queryClient.invalidateQueries({ queryKey: invalidateKey });
    },
    onError: (e: unknown) => toast.error(extract(e, t("pim.errSetCover"))),
  });

  const removeMutation = useMutation({
    mutationFn: (imageId: string) => removeProductImage(productId, imageId),
    onSuccess: () => {
      toast.success(t("pim.toastImageRemoved"));
      void queryClient.invalidateQueries({ queryKey: invalidateKey });
      setPendingRemove(null);
    },
    onError: (e: unknown) => toast.error(extract(e, t("pim.errRemove"))),
  });

  const handlePick = () => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = "image/*";
    input.multiple = true;
    input.onchange = async () => {
      const files = Array.from(input.files ?? []);
      if (files.length === 0) return;
      for (const file of files) {
        try {
          const asset = await upload(file);
          const meta = await getFileMetadata(asset.id);
          if (!meta.publicUrl) {
            throw new Error(t("pim.noPublicUrl"));
          }
          await attachMutation.mutateAsync({ fileAssetId: asset.id, url: meta.publicUrl });
        } catch (e) {
          toast.error(extract(e, t("pim.errUploadNamed", { name: file.name })));
        }
      }
      reset();
    };
    input.click();
  };

  const previewImage = images.find((i) => i.id === previewId) ?? null;
  const sorted = [...images].sort((a, b) => a.sortOrder - b.sortOrder);

  return (
    <div className={cn("space-y-4", className)}>
      {/* Upload row */}
      <div className="flex items-center gap-3">
        <Button type="button" onClick={handlePick} disabled={isUploading || attachMutation.isPending}>
          {isUploading || attachMutation.isPending
            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
            : <Upload className="h-3.5 w-3.5" />}
          {t("pim.upload")}
        </Button>
        {progress && progress.status !== "done" && (
          <span className="text-[11.5px] tabular-nums text-[var(--color-muted-foreground)]">
            {progress.fileName} · {progress.percent}%
          </span>
        )}
        <span className="ml-auto text-[11.5px] text-[var(--color-muted-foreground)]">
          {t("pim.count", { count: sorted.length })}
        </span>
      </div>

      {/* Gallery grid */}
      {sorted.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-[var(--color-muted)] px-6 py-10 text-center text-sm text-[var(--color-muted-foreground)]">
          {t("pim.empty")}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {sorted.map((image) => (
            <ImageTile
              key={image.id}
              image={image}
              onPreview={() => setPreviewId(image.id)}
              onSetThumbnail={() => thumbnailMutation.mutate(image.id)}
              onRemove={() => setPendingRemove(image)}
              busy={thumbnailMutation.isPending || removeMutation.isPending}
            />
          ))}
        </div>
      )}

      <PreviewDialog image={previewImage} onClose={() => setPreviewId(null)} />
      <RemoveDialog
        image={pendingRemove}
        onCancel={() => setPendingRemove(null)}
        onConfirm={() => pendingRemove && removeMutation.mutate(pendingRemove.id)}
        busy={removeMutation.isPending}
      />
    </div>
  );
}

function ImageTile({
  image,
  onPreview,
  onSetThumbnail,
  onRemove,
  busy,
}: {
  image: ProductImageDto;
  onPreview: () => void;
  onSetThumbnail: () => void;
  onRemove: () => void;
  busy: boolean;
}) {
  const { t } = useTranslation("files");
  return (
    <div
      className={cn(
        "group relative overflow-hidden rounded-xl border bg-[var(--color-card)] shadow-xs transition-colors",
        image.isThumbnail
          ? "border-[var(--color-primary)] ring-1 ring-[var(--color-primary)]"
          : "border-border hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
      )}
    >
      <button
        type="button"
        onClick={onPreview}
        className="block aspect-square w-full cursor-pointer"
        aria-label={t("pim.previewImage")}
      >
        <img
          src={image.url}
          alt=""
          className="h-full w-full object-cover"
          loading="lazy"
        />
      </button>

      {image.isThumbnail && (
        <span className="absolute left-2 top-2 inline-flex items-center gap-1 rounded-full bg-[var(--color-primary)] px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-primary-foreground)]">
          <Star className="h-3 w-3 fill-current" />
          {t("pim.cover")}
        </span>
      )}

      <div className="absolute inset-x-0 bottom-0 flex items-center justify-end gap-1 bg-gradient-to-t from-[oklch(0_0_0/0.65)] to-transparent p-2 opacity-0 transition-opacity duration-[var(--duration-fast)] group-hover:opacity-100 focus-within:opacity-100">
        {!image.isThumbnail && (
          <Button
            type="button"
            size="icon"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              onSetThumbnail();
            }}
            disabled={busy}
            title={t("pim.setCover")}
            aria-label={t("pim.setCover")}
            className="bg-[var(--color-overlay)] text-[var(--color-overlay-foreground)] hover:bg-[oklch(0_0_0/0.65)]"
          >
            <StarOff className="h-4 w-4" />
          </Button>
        )}
        <Button
          type="button"
          size="icon"
          variant="ghost"
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          disabled={busy}
          title={t("pim.removeImage")}
          aria-label={t("pim.removeImage")}
          className="bg-[var(--color-overlay)] text-[var(--color-overlay-foreground)] hover:bg-[var(--color-destructive)]"
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

function PreviewDialog({ image, onClose }: { image: ProductImageDto | null; onClose: () => void }) {
  const { t } = useTranslation("files");
  return (
    <Dialog open={image !== null} onOpenChange={(o) => (o ? undefined : onClose())}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle className="truncate">{t("pim.previewTitle")}</DialogTitle>
          <DialogDescription>
            {image?.isThumbnail ? t("pim.currentCover") : t("pim.clickOutside")}
          </DialogDescription>
        </DialogHeader>
        {image && (
          <div className="grid place-items-center overflow-hidden rounded-xl border border-border bg-[var(--color-muted)]">
            <img
              src={image.url}
              alt=""
              className="max-h-[70vh] w-auto object-contain"
            />
          </div>
        )}
        <DialogFooter>
          <DialogClose asChild>
            <Button size="sm">{t("pim.close")}</Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function RemoveDialog({
  image,
  onCancel,
  onConfirm,
  busy,
}: {
  image: ProductImageDto | null;
  onCancel: () => void;
  onConfirm: () => void;
  busy: boolean;
}) {
  const { t } = useTranslation("files");
  return (
    <Dialog open={image !== null} onOpenChange={(o) => (o ? undefined : onCancel())}>
      <DialogContent>
        <DialogHeader>
          <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-destructive)]">
            {t("pim.removeImage")}
          </span>
          <DialogTitle>{t("pim.detachTitle")}</DialogTitle>
          <DialogDescription>
            {t("pim.detachBody")}{image?.isThumbnail
              ? t("pim.detachCoverNote")
              : ""}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={busy}>
              {t("pim.cancel")}
            </Button>
          </DialogClose>
          <Button variant="destructive" onClick={onConfirm} disabled={busy}>
            {busy ? t("pim.removing") : t("pim.remove")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function extract(e: unknown, fallback: string): string {
  if (e instanceof ApiRequestError) {
    return e.problem?.detail ?? e.problem?.title ?? e.message;
  }
  if (e instanceof Error) return e.message;
  return fallback;
}
