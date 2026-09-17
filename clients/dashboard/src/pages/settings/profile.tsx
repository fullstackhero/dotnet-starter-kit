import { useEffect, useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Camera, Fingerprint, UserCircle2 } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { getMyProfileWithETag, setProfileImage, updateMyProfile } from "@/api/identity";
import type { UserDto } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ImageInput } from "@/components/file/image-input";
import { SettingsSection } from "@/pages/settings/settings-layout";

const PROFILE_KEY = ["identity", "me"] as const;

export function ProfileSettings() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const profileQuery = useQuery({
    queryKey: PROFILE_KEY,
    queryFn: getMyProfileWithETag,
  });

  const profile = profileQuery.data?.profile;
  const loading = profileQuery.isLoading;

  // The version this form is editing against, captured when the form is seeded. Deliberately a
  // ref and not `profileQuery.data`: a background refetch would otherwise move it to a version
  // the user never saw, and the save would carry an If-Match that matches whatever someone else
  // just wrote — silently overwriting it, which is exactly what the ETag exists to prevent. It
  // moves only on a deliberate step: a successful save, or the user being told about a conflict.
  const editingVersionRef = useRef<{ profile: UserDto; etag: string | null } | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");

  // Seed the form from the fetched profile exactly once. The JWT-derived
  // `user` provides an immediate non-empty paint; the authoritative profile
  // then seeds and locks. Seeding once means a later background refetch
  // can't clobber edits the user has already made.
  const seededRef = useRef(false);
  useEffect(() => {
    if (seededRef.current) return;
    if (profileQuery.data) {
      const { profile: seeded } = profileQuery.data;
      setFirstName(seeded.firstName ?? "");
      setLastName(seeded.lastName ?? "");
      setPhone(seeded.phoneNumber ?? "");
      editingVersionRef.current = profileQuery.data;
      seededRef.current = true;
    } else if (user && loading) {
      setFirstName(user.name?.split(" ")[0] ?? "");
      setLastName(user.name?.split(" ").slice(1).join(" ") ?? "");
    }
  }, [profileQuery.data, user, loading]);

  // Re-reads the profile and adopts it as the version the form edits against, so the next save
  // carries a tag the server will accept.
  const adoptCurrentVersion = async () => {
    const fresh = await queryClient.fetchQuery({
      queryKey: PROFILE_KEY,
      queryFn: getMyProfileWithETag,
      // Must reach the network. The client's default staleTime would hand back the cached copy,
      // and the cached copy carries the very tag the server just rejected.
      staleTime: 0,
    });
    editingVersionRef.current = fresh;
    return fresh;
  };

  const saveMutation = useMutation({
    mutationFn: updateMyProfile,
    onSuccess: () => {
      toast.success("Profile saved");
      // The save moved the profile on, so the tag the form holds is spent: adopt the new one or
      // a second save in the same sitting would 412 against the user's own write.
      void adoptCurrentVersion();
    },
    onError: async (err: unknown) => {
      // 412 means someone else wrote the profile after this form was seeded. Do NOT resend: the
      // only body available is the one typed against the old values, and pushing it through
      // against a fresh tag performs the overwrite the 412 just prevented. Keep the user's
      // edits on screen, adopt the current version, and let them decide whether to save again.
      if (err instanceof ApiRequestError && err.status === 412) {
        await adoptCurrentVersion();
        toast.warning("Profile changed elsewhere", {
          description:
            "Someone updated this profile while you were editing. Review your changes and save again to apply them.",
        });
        return;
      }

      const message =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : "Failed to save profile";
      toast.error("Save failed", { description: message });
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const editing = editingVersionRef.current;
    if (!editing) return;
    // Everything the save needs travels through mutate(), never through state the callbacks
    // close over: the values that go out must be the ones on screen when the button was pressed.
    saveMutation.mutate({
      profile: editing.profile,
      expectedETag: editing.etag,
      firstName: firstName.trim() || null,
      lastName: lastName.trim() || null,
      phoneNumber: phone.trim() || null,
    });
  };

  const onReset = () => {
    if (profile) {
      setFirstName(profile.firstName ?? "");
      setLastName(profile.lastName ?? "");
      setPhone(profile.phoneNumber ?? "");
    }
  };

  const saving = saveMutation.isPending;
  const dirty =
    (profile?.firstName ?? "") !== firstName ||
    (profile?.lastName ?? "") !== lastName ||
    (profile?.phoneNumber ?? "") !== phone;
  // A save carries the unedited fields and the version tag off the profile read, so until that
  // read lands there is nothing to save against. Disabled rather than silently doing nothing.
  const canSave = profileQuery.isSuccess;

  const imageMutation = useMutation({
    mutationFn: (url: string | null) => setProfileImage(url),
    onSuccess: () => {
      toast.success("Profile image updated");
      queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
    },
    onError: (e: unknown) => {
      const message =
        e instanceof ApiRequestError
          ? (e.problem?.detail ?? e.problem?.title ?? e.message)
          : "Failed to update profile image";
      toast.error(message);
    },
  });

  return (
    <form onSubmit={onSubmit} className="space-y-5 fsh-enter">
      {profileQuery.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[13px] text-[var(--color-destructive)]"
        >
          <span>
            Couldn't load your profile. Showing details from your session; saving is disabled
            until the profile loads, because a save has to carry the version it was read at.
          </span>
        </div>
      )}
      <SettingsSection
        title="Photo"
        icon={Camera}
        description="Shown in the topbar and on your activity. Square crops work best — JPG, PNG, or WebP."
      >
        <ImageInput
          value={profile?.imageUrl ?? ""}
          onChange={(next) => imageMutation.mutate(next.length > 0 ? next : null)}
          ownerType="User"
          ownerId={profile?.id ?? null}
          shape="circle"
        />
      </SettingsSection>

      <SettingsSection
        title="Identity"
        icon={UserCircle2}
        description="Your name and contact details, visible across the dashboard."
        footer={
          <div className="flex items-center justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={onReset}
              disabled={saving || !dirty || !canSave}
              size="sm"
            >
              Reset
            </Button>
            <Button type="submit" disabled={saving || !dirty || !canSave} size="sm">
              {saving ? "Saving…" : "Save changes"}
            </Button>
          </div>
        }
      >
        <div className="grid gap-5 sm:grid-cols-2">
          <Field id="first-name" label="First name">
            <Input
              id="first-name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              autoComplete="given-name"
              disabled={loading}
              className="h-10 text-[13px]"
            />
          </Field>
          <Field id="last-name" label="Last name">
            <Input
              id="last-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              autoComplete="family-name"
              disabled={loading}
              className="h-10 text-[13px]"
            />
          </Field>
          <Field id="email" label="Email">
            <Input
              id="email"
              type="email"
              value={profile?.email ?? user?.email ?? ""}
              readOnly
              disabled
              className="h-10 cursor-not-allowed bg-[var(--color-muted)] text-[13px]"
            />
            <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
              Contact your tenant admin to change your sign-in email.
            </p>
          </Field>
          <Field id="phone" label="Phone">
            <Input
              id="phone"
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              autoComplete="tel"
              placeholder="+1 (555) 123-4567"
              disabled={loading}
              className="h-10 text-[13px]"
            />
          </Field>
        </div>
      </SettingsSection>

      <SettingsSection
        title="Subject identifier"
        icon={Fingerprint}
        description="The unique ID this account uses inside the platform. Read-only."
      >
        <code className="block w-full overflow-x-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 font-mono text-xs">
          {profile?.id ?? user?.id ?? "—"}
        </code>
      </SettingsSection>
    </form>
  );
}

function Field({
  id,
  label,
  children,
}: {
  id: string;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <Label
        htmlFor={id}
        className="mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
      >
        {label}
      </Label>
      {children}
    </div>
  );
}
