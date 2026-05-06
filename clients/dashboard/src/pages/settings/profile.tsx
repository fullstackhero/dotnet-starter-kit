import { useState, type FormEvent } from "react";
import { useAuth } from "@/auth/use-auth";
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

export function ProfileSettings() {
  const { user } = useAuth();
  const [firstName, setFirstName] = useState(user?.name?.split(" ")[0] ?? "");
  const [lastName, setLastName] = useState(user?.name?.split(" ").slice(1).join(" ") ?? "");
  const [phone, setPhone] = useState("");
  const [saving, setSaving] = useState(false);

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    // TODO: wire to /api/v1/identity/profile when the endpoint is available.
    await new Promise((r) => setTimeout(r, 800));
    setSaving(false);
  };

  return (
    <form onSubmit={onSubmit} className="space-y-6 fsh-enter">
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
              value={user?.email ?? ""}
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
            {user?.id ?? "—"}
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
