import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import { getMyProfile, setProfileImage } from "@/api/users";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  ErrorBand,
  Field,
  FormShell,
  FormSection,
  LoadingRow,
} from "@/components/list";
import { Monogram } from "@/components/monogram";
import { ApiRequestError } from "@/lib/api-client";

/**
 * ProfileSettings — read-only view of identity fields (server doesn't expose
 * an /update-me endpoint for these yet) plus avatar upload via the existing
 * /profile/image flow. Username, email, and name are intentionally not
 * editable from here — they require admin involvement, which is correct for
 * a multi-tenant operator console.
 */
export function ProfileSettings() {
  const queryClient = useQueryClient();
  const profile = useQuery({ queryKey: ["identity", "profile"], queryFn: getMyProfile });

  if (profile.isLoading) return <LoadingRow label="Loading profile" />;
  if (profile.isError) {
    return (
      <ErrorBand
        message={
          profile.error instanceof ApiRequestError
            ? profile.error.problem?.detail ?? profile.error.message
            : "Failed to load profile."
        }
      />
    );
  }

  const user = profile.data!;
  const displayName =
    [user.firstName, user.lastName].filter(Boolean).join(" ").trim() ||
    user.userName ||
    user.email ||
    "Account";

  return (
    <FormShell>
      <FormSection
        title="Avatar"
        description="A square image, at least 96×96. PNG or JPG, under 2 MB."
      >
        <AvatarEditor
          name={displayName}
          userId={user.id ?? "x"}
          imageUrl={user.imageUrl ?? null}
          onUpdated={() => queryClient.invalidateQueries({ queryKey: ["identity", "profile"] })}
        />
      </FormSection>

      <FormSection
        title="Identity"
        description="Your account details. These are managed by an administrator — contact one if changes are needed."
      >
        <Field id="profile-username" label="Username">
          <Input id="profile-username" value={user.userName ?? ""} readOnly className="font-mono bg-[var(--color-surface-2)]" />
        </Field>
        <Field id="profile-display" label="Display name">
          <Input id="profile-display" value={displayName} readOnly className="bg-[var(--color-surface-2)]" />
        </Field>
        <Field id="profile-email" label="Email" hint={user.emailConfirmed ? "Verified" : "Not yet verified"}>
          <Input id="profile-email" value={user.email ?? ""} readOnly className="font-mono bg-[var(--color-surface-2)]" />
        </Field>
        <Field id="profile-phone" label="Phone">
          <Input id="profile-phone" value={user.phoneNumber ?? "—"} readOnly className="font-mono bg-[var(--color-surface-2)]" />
        </Field>
        <div className="flex flex-wrap items-center gap-2 pt-1">
          <Badge variant={user.isActive ? "success" : "muted"} className="font-mono uppercase tracking-[0.14em]">
            {user.isActive ? "Active" : "Disabled"}
          </Badge>
          <Badge variant={user.emailConfirmed ? "info" : "warning"} className="font-mono uppercase tracking-[0.14em]">
            {user.emailConfirmed ? "Email confirmed" : "Email pending"}
          </Badge>
          <Badge variant={user.twoFactorEnabled ? "success" : "outline"} className="font-mono uppercase tracking-[0.14em]">
            {user.twoFactorEnabled ? "2FA enabled" : "2FA off"}
          </Badge>
        </div>
      </FormSection>
    </FormShell>
  );
}

// ─── Avatar editor ──────────────────────────────────────────────────────

const MAX_IMAGE_BYTES = 2 * 1024 * 1024;
const ACCEPTED_TYPES = ["image/png", "image/jpeg", "image/webp"];

function AvatarEditor({
  name,
  userId,
  imageUrl,
  onUpdated,
}: {
  name: string;
  userId: string;
  imageUrl: string | null;
  onUpdated: () => void;
}) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);

  useEffect(() => {
    // The data: URL preview is short-lived; revoke the previous one when
    // a new file is picked or the component unmounts.
    return () => {
      if (preview && preview.startsWith("blob:")) URL.revokeObjectURL(preview);
    };
  }, [preview]);

  const mutation = useMutation({
    mutationFn: async (file: File | null) => {
      if (file === null) {
        await setProfileImage(null);
        return null;
      }
      // The server's /profile/image endpoint takes a durable URL, not raw
      // bytes — the Files module's presigned upload returns one. Here we
      // skip that round-trip for v1 and inline a data: URL, which keeps the
      // demo working without a storage bucket configured. Production
      // deployments should switch to presigned uploads.
      const dataUrl = await fileToDataUrl(file);
      await setProfileImage(dataUrl);
      return dataUrl;
    },
    onSuccess: (newUrl) => {
      toast.success(newUrl === null ? "Avatar removed" : "Avatar updated");
      setPreview(null);
      onUpdated();
    },
    onError: (err: unknown) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Update failed", { description: detail });
    },
  });

  const onPick = (file: File | null) => {
    if (!file) return;
    if (!ACCEPTED_TYPES.includes(file.type)) {
      toast.error("Unsupported format", { description: "Use PNG, JPG, or WebP." });
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      toast.error("Too large", { description: "Keep avatars under 2 MB." });
      return;
    }
    if (preview && preview.startsWith("blob:")) URL.revokeObjectURL(preview);
    setPreview(URL.createObjectURL(file));
    mutation.mutate(file);
  };

  const currentSrc = preview ?? imageUrl ?? null;

  return (
    <div className="flex flex-wrap items-center gap-5">
      <div className="grid h-20 w-20 place-items-center overflow-hidden rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)]">
        {currentSrc ? (
          // Avatar preview — object-cover so non-square images crop centrally.
          <img src={currentSrc} alt="Avatar" className="h-full w-full object-cover" />
        ) : (
          <Monogram seed={userId} fallback={name} size="lg" />
        )}
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <input
          ref={fileRef}
          type="file"
          accept={ACCEPTED_TYPES.join(",")}
          className="sr-only"
          onChange={(e) => onPick(e.target.files?.[0] ?? null)}
        />
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={mutation.isPending}
          onClick={() => fileRef.current?.click()}
        >
          {mutation.isPending ? (
            <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
          ) : (
            <Upload className="mr-1.5 h-3.5 w-3.5" />
          )}
          Upload new
        </Button>
        {imageUrl && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            disabled={mutation.isPending}
            onClick={() => mutation.mutate(null)}
            className="text-[var(--color-destructive)] hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.08)]"
          >
            <Trash2 className="mr-1.5 h-3.5 w-3.5" /> Remove
          </Button>
        )}
      </div>
    </div>
  );
}

function fileToDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error ?? new Error("read failed"));
    reader.readAsDataURL(file);
  });
}
