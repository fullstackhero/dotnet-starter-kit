import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Camera, Fingerprint, UserCircle2 } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { getMyProfile, setProfileImage, updateMyProfile } from "@/api/identity";
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
    queryFn: getMyProfile,
  });

  const profile = profileQuery.data;
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");

  // Seed form state from the fetched profile (falls back to the JWT-derived
  // user while the query is in flight so the form isn't empty on first paint).
  useEffect(() => {
    if (profile) {
      setFirstName(profile.firstName ?? "");
      setLastName(profile.lastName ?? "");
      setPhone(profile.phoneNumber ?? "");
    } else if (user) {
      setFirstName(user.name?.split(" ")[0] ?? "");
      setLastName(user.name?.split(" ").slice(1).join(" ") ?? "");
    }
  }, [profile, user]);

  const saveMutation = useMutation({
    mutationFn: () =>
      updateMyProfile({
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        phoneNumber: phone.trim() || null,
      }),
    onSuccess: () => {
      toast.success("Profile saved");
      queryClient.invalidateQueries({ queryKey: PROFILE_KEY });
    },
    onError: (err: unknown) => {
      const message =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : "Failed to save profile";
      toast.error("Save failed", { description: message });
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    saveMutation.mutate();
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
              disabled={saving || !dirty}
              size="sm"
            >
              Reset
            </Button>
            <Button type="submit" disabled={saving || !dirty} size="sm">
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
              className="h-10 text-[13px]"
            />
          </Field>
          <Field id="last-name" label="Last name">
            <Input
              id="last-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              autoComplete="family-name"
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
