import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import { registerUser, type RegisterUserInput } from "@/api/users";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SectionRule } from "@/components/section-rule";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const USERNAME_RE = /^[a-zA-Z][a-zA-Z0-9._-]{2,31}$/;

export function CreateUserPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [form, setForm] = useState<RegisterUserInput>({
    firstName: "",
    lastName: "",
    email: "",
    userName: "",
    password: "",
    confirmPassword: "",
    phoneNumber: "",
  });

  const usernameInvalid = form.userName.length > 0 && !USERNAME_RE.test(form.userName);
  const passwordsMismatch =
    form.confirmPassword.length > 0 && form.password !== form.confirmPassword;

  const mutation = useMutation({
    mutationFn: (input: RegisterUserInput) => registerUser(input),
    onSuccess: (result) => {
      toast.success("User created", {
        description: result.message ?? "Confirmation email queued.",
      });
      queryClient.invalidateQueries({ queryKey: ["users"] });
      if (result.userId) {
        navigate(`/users/${result.userId}`);
      } else {
        navigate("/users");
      }
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err instanceof Error
            ? err.message
            : "Failed to create user";
      toast.error("Create failed", { description: detail });
    },
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (usernameInvalid || passwordsMismatch) return;
    mutation.mutate({
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      userName: form.userName.trim(),
      password: form.password,
      confirmPassword: form.confirmPassword,
      phoneNumber: form.phoneNumber?.trim() || undefined,
    });
  };

  const set = <K extends keyof RegisterUserInput>(k: K) => (v: RegisterUserInput[K]) =>
    setForm((f) => ({ ...f, [k]: v }));

  return (
    <div className="space-y-8">
      <SectionRule
        crumbs={[{ label: "\\ Users" }, { label: "New", muted: true }]}
        trailing="Account · Draft"
      />

      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate("/users")}
          className="-ml-2 mb-2 font-mono text-[0.6875rem] uppercase tracking-[0.18em]"
        >
          <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Directory
        </Button>
        <h1 className="font-display text-4xl font-semibold tracking-tight md:text-5xl">
          New account
        </h1>
        <p className="mt-2 max-w-xl text-sm text-[var(--color-muted-foreground)]">
          The new user is created in the current tenant and emailed a confirmation link.
          Roles can be assigned from the detail page after creation.
        </p>
      </div>

      <form onSubmit={onSubmit} className="grid gap-10 md:grid-cols-[20rem_1fr]">
        <aside className="space-y-2 border-t border-[var(--color-foreground)] pt-3">
          <p className="font-mono text-[0.6875rem] uppercase tracking-[0.22em] text-[var(--color-foreground)]">
            \\ Identity
          </p>
          <p className="text-sm text-[var(--color-muted-foreground)]">
            Personal details and the username they'll use to sign in.
          </p>
        </aside>

        <div className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-2">
            <Field
              id="firstName"
              label="First name"
              value={form.firstName}
              onChange={set("firstName")}
              autoComplete="given-name"
              required
            />
            <Field
              id="lastName"
              label="Last name"
              value={form.lastName}
              onChange={set("lastName")}
              autoComplete="family-name"
              required
            />
          </div>

          <Field
            id="userName"
            label="Username"
            hint="Letters, digits, dot, dash or underscore. 3–32 characters."
            value={form.userName}
            onChange={set("userName")}
            autoComplete="off"
            required
            mono
            error={usernameInvalid ? "Invalid format." : undefined}
            placeholder="m.chen"
          />

          <Field
            id="email"
            label="Email"
            type="email"
            value={form.email}
            onChange={set("email")}
            autoComplete="email"
            required
            mono
            placeholder="user@example.com"
          />

          <Field
            id="phoneNumber"
            label="Phone (optional)"
            type="tel"
            value={form.phoneNumber ?? ""}
            onChange={set("phoneNumber")}
            autoComplete="tel"
            mono
            placeholder="+1 555 0100"
          />
        </div>

        <aside className="space-y-2 border-t border-[var(--color-foreground)] pt-3">
          <p className="font-mono text-[0.6875rem] uppercase tracking-[0.22em] text-[var(--color-foreground)]">
            \\ Credentials
          </p>
          <p className="text-sm text-[var(--color-muted-foreground)]">
            Initial password. The user is encouraged to change it on first sign-in.
          </p>
        </aside>

        <div className="space-y-5">
          <Field
            id="password"
            label="Password"
            type="password"
            value={form.password}
            onChange={set("password")}
            autoComplete="new-password"
            required
            mono
          />
          <Field
            id="confirmPassword"
            label="Confirm password"
            type="password"
            value={form.confirmPassword}
            onChange={set("confirmPassword")}
            autoComplete="new-password"
            required
            mono
            error={passwordsMismatch ? "Passwords don't match." : undefined}
          />
        </div>

        <div className="md:col-start-2">
          <div className="flex items-center gap-2 border-t border-[var(--color-border)] pt-5">
            <Button
              type="submit"
              disabled={
                mutation.isPending ||
                usernameInvalid ||
                passwordsMismatch ||
                !form.firstName.trim() ||
                !form.lastName.trim() ||
                !form.email.trim() ||
                !form.userName.trim() ||
                !form.password ||
                !form.confirmPassword
              }
            >
              {mutation.isPending ? "Creating…" : "Create account"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate("/users")}
              disabled={mutation.isPending}
            >
              Cancel
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}

type FieldProps = {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
  hint?: string;
  error?: string;
  placeholder?: string;
  autoComplete?: string;
  mono?: boolean;
};

function Field({ id, label, value, onChange, type, required, hint, error, placeholder, autoComplete, mono }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <Label
        htmlFor={id}
        className="font-mono text-[0.6875rem] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]"
      >
        {label}
      </Label>
      <Input
        id={id}
        type={type ?? "text"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        placeholder={placeholder}
        autoComplete={autoComplete}
        aria-invalid={error ? true : undefined}
        className={cn(mono && "font-mono")}
      />
      {hint && <p className="text-xs text-[var(--color-muted-foreground)]">{hint}</p>}
      {error && <p className="text-xs text-[var(--color-destructive)]">{error}</p>}
    </div>
  );
}
