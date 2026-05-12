import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAuth } from "@/auth/use-auth";
import { getMyProfile, setProfileImage } from "@/api/identity";
import { ApiRequestError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ImageInput } from "@/components/file/image-input";

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
  const [saving, setSaving] = useState(false);

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

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    // TODO: wire to /api/v1/identity/profile when the JSON endpoint accepts these fields.
    await new Promise((r) => setTimeout(r, 800));
    setSaving(false);
  };

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
    <form onSubmit={onSubmit} className="space-y-6 fsh-enter">
      <Card>
        <CardHeader>
          <CardTitle>Profile photo</CardTitle>
          <CardDescription>
            Shown in the topbar and on your activity. Square crops work best — JPG, PNG, or WebP.
          </CardDescription>
        </CardHeader>
        <CardContent className="px-6 pb-5 pt-1">
          <ImageInput
            value={profile?.imageUrl ?? ""}
            onChange={(next) => imageMutation.mutate(next.length > 0 ? next : null)}
            ownerType="User"
            ownerId={profile?.id ?? null}
            shape="circle"
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Public profile</CardTitle>
          <CardDescription>
            Your name and contact details, visible across the dashboard.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-5 px-6 pb-5 pt-1 sm:grid-cols-2">
          <Field id="first-name" label="First name">
            <Input
              id="first-name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              autoComplete="given-name"
            />
          </Field>
          <Field id="last-name" label="Last name">
            <Input
              id="last-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              autoComplete="family-name"
            />
          </Field>
          <Field id="email" label="Email">
            <Input
              id="email"
              type="email"
              value={profile?.email ?? user?.email ?? ""}
              readOnly
              disabled
              className="cursor-not-allowed"
            />
            <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
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
            />
          </Field>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Subject identifier</CardTitle>
          <CardDescription>
            The unique ID this account uses inside the platform. Read-only.
          </CardDescription>
        </CardHeader>
        <CardContent className="px-6 pb-5 pt-1">
          <code className="block w-full overflow-x-auto rounded-md border border-[var(--color-border)] bg-[var(--color-surface-2)] px-3 py-2 font-mono text-xs">
            {profile?.id ?? user?.id ?? "—"}
          </code>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" disabled={saving}>
          Reset
        </Button>
        <Button type="submit" disabled={saving}>
          {saving ? "Saving…" : "Save changes"}
        </Button>
      </div>
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
        className="mb-1.5 block text-[11px] font-medium uppercase tracking-[0.08em] text-[var(--color-muted-foreground)]"
      >
        {label}
      </Label>
      {children}
    </div>
  );
}
