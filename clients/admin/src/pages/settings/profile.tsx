import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Fingerprint, ShieldCheck, UserRound } from "lucide-react";
import { toast } from "sonner";
import { getMyProfile, setProfileImage } from "@/api/users";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, LoadingRow, SettingsSection, SettingsField } from "@/components/list";
import { ImageInput } from "@/components/file/image-input";
import { ApiRequestError } from "@/lib/api-client";

/**
 * ProfileSettings — read-only view of identity fields (server doesn't expose
 * an /update-me endpoint for these yet) plus avatar upload via the presigned
 * ImageInput flow. Username, email, and name are intentionally not editable
 * from here — they require admin involvement, which is correct for a
 * multi-tenant operator console.
 *
 * Avatar fix: uses ImageInput + presigned upload (durable URL via Files module)
 * instead of the old base64 data: URL approach that hit the 2048-char limit.
 */
export function ProfileSettings() {
  const queryClient = useQueryClient();
  const profile = useQuery({ queryKey: ["identity", "profile"], queryFn: getMyProfile });

  const imageMutation = useMutation({
    mutationFn: (url: string | null) => setProfileImage(url),
    onSuccess: () => {
      toast.success("Profile image updated");
      void queryClient.invalidateQueries({ queryKey: ["identity", "profile"] });
    },
    onError: (err: unknown) => {
      const message =
        err instanceof ApiRequestError
          ? (err.problem?.detail ?? err.problem?.title ?? err.message)
          : "Failed to update profile image";
      toast.error(message);
    },
  });

  if (profile.isLoading) return <LoadingRow label="Loading profile" />;
  if (profile.isError) {
    return (
      <ErrorBand
        message={
          profile.error instanceof ApiRequestError
            ? (profile.error.problem?.detail ?? profile.error.message)
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
    <div className="space-y-5 fsh-enter">
      {/* Avatar — presigned upload via ImageInput, no base64 data: URLs */}
      <SettingsSection
        title="Avatar"
        icon={UserRound}
        description="Shown in the topbar and on your activity. Square crops work best — JPG, PNG, or WebP."
      >
        <ImageInput
          value={user.imageUrl ?? ""}
          onChange={(next) => imageMutation.mutate(next.length > 0 ? next : null)}
          ownerType="User"
          ownerId={user.id ?? null}
          shape="circle"
        />
      </SettingsSection>

      {/* Identity — read-only; admin must update these server-side */}
      <SettingsSection
        title="Identity"
        icon={Fingerprint}
        description="Your account details. These are managed by an administrator — contact one if changes are needed."
      >
        <div className="grid gap-5 sm:grid-cols-2">
          <SettingsField id="profile-username" label="Username">
            <Input
              id="profile-username"
              value={user.userName ?? ""}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
          <SettingsField id="profile-display" label="Display name">
            <Input
              id="profile-display"
              value={displayName}
              readOnly
              className="bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
          <SettingsField id="profile-email" label="Email">
            <Input
              id="profile-email"
              type="email"
              value={user.email ?? ""}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
            {user.emailConfirmed !== undefined && (
              <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
                {user.emailConfirmed ? "Address verified" : "Not yet verified"}
              </p>
            )}
          </SettingsField>
          <SettingsField id="profile-phone" label="Phone">
            <Input
              id="profile-phone"
              value={user.phoneNumber ?? "—"}
              readOnly
              className="font-mono bg-[var(--color-muted)] cursor-not-allowed"
            />
          </SettingsField>
        </div>
      </SettingsSection>

      {/* Status badges */}
      <SettingsSection
        title="Account status"
        icon={ShieldCheck}
        description="Runtime flags on this account. Contact an operator to change them."
      >
        <div className="flex flex-wrap items-center gap-2">
          <Badge
            variant={user.isActive ? "success" : "muted"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.isActive ? "Active" : "Disabled"}
          </Badge>
          <Badge
            variant={user.emailConfirmed ? "info" : "warning"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.emailConfirmed ? "Email confirmed" : "Email pending"}
          </Badge>
          <Badge
            variant={user.twoFactorEnabled ? "success" : "outline"}
            className="font-mono uppercase tracking-[0.14em]"
          >
            {user.twoFactorEnabled ? "2FA enabled" : "2FA off"}
          </Badge>
        </div>
      </SettingsSection>
    </div>
  );
}

