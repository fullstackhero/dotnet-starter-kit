import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { KeyRound, User as UserIcon, Users } from "lucide-react";
import { toast } from "sonner";
import { registerUser } from "@/api/users";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/list";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogBody,
  DialogFooter,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";

// ─── Schema (identical to the old create page) ───────────────────────────────

const USERNAME_RE = /^[a-zA-Z][a-zA-Z0-9._-]{2,31}$/;

const schema = z
  .object({
    firstName: z.string().trim().min(1, "Required.").max(64),
    lastName: z.string().trim().min(1, "Required.").max(64),
    userName: z
      .string()
      .trim()
      .regex(USERNAME_RE, "3–32 chars. Letters, digits, dot, dash, underscore. Start with a letter."),
    email: z.string().trim().email("Enter a valid email."),
    phoneNumber: z.string().trim().max(32).optional(),
    password: z.string().min(8, "At least 8 characters."),
    confirmPassword: z.string().min(8),
  })
  .refine((d) => d.password === d.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords don't match.",
  });

type FormValues = z.infer<typeof schema>;

// ─── Section label ────────────────────────────────────────────────────────────

function SectionLabel({
  icon: Icon,
  title,
  description,
}: {
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean | "true" }>;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-2.5 pb-1">
      <span
        aria-hidden
        className="mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-md bg-[var(--color-accent)] text-[var(--color-muted-foreground)]"
      >
        <Icon className="h-3.5 w-3.5" aria-hidden />
      </span>
      <div className="min-w-0">
        <p className="text-[12.5px] font-semibold text-[var(--color-foreground)]">{title}</p>
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]">
          {description}
        </p>
      </div>
    </div>
  );
}

// ─── Dialog ───────────────────────────────────────────────────────────────────

export function CreateUserDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: "",
      lastName: "",
      userName: "",
      email: "",
      phoneNumber: "",
      password: "",
      confirmPassword: "",
    },
  });

  const mutation = useMutation({
    // Pass values via mutate(arg) — no closed-over state captured at submit time.
    mutationFn: (values: FormValues) =>
      registerUser({
        firstName: values.firstName,
        lastName: values.lastName,
        userName: values.userName,
        email: values.email,
        password: values.password,
        confirmPassword: values.confirmPassword,
        phoneNumber: values.phoneNumber?.trim() || undefined,
      }),
    onSuccess: (result) => {
      toast.success("User created", {
        description: result.message ?? "Confirmation email queued.",
      });
      queryClient.invalidateQueries({ queryKey: ["users"] });
      handleClose();
      navigate(result.userId ? `/users/${result.userId}` : "/users");
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Create failed", { description: detail });
    },
  });

  function handleClose() {
    reset();
    onOpenChange(false);
  }

  const onSubmit = handleSubmit((values) => mutation.mutate(values));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) handleClose();
        else onOpenChange(true);
      }}
    >
      <DialogContent size="lg">
        {/* ── Header ── */}
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="relative grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)]
                text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <Users className="h-[18px] w-[18px]" />
            </span>
            <div className="min-w-0">
              <DialogTitle className="text-[16px]">New account</DialogTitle>
            </div>
          </div>
          <DialogDescription className="mt-1">
            The new user is created in the current tenant and emailed a confirmation link. Roles can
            be assigned from the detail page after creation.
          </DialogDescription>
        </DialogHeader>

        {/* ── Form ── */}
        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-6">
            {/* ── Identity section ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={UserIcon}
                title="Identity"
                description="Personal details and the username they'll use to sign in."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field
                    id="cu-firstName"
                    label="First name"
                    required
                    error={errors.firstName?.message}
                  >
                    <Input
                      id="cu-firstName"
                      autoComplete="given-name"
                      aria-invalid={errors.firstName ? true : undefined}
                      {...register("firstName")}
                    />
                  </Field>
                  <Field
                    id="cu-lastName"
                    label="Last name"
                    required
                    error={errors.lastName?.message}
                  >
                    <Input
                      id="cu-lastName"
                      autoComplete="family-name"
                      aria-invalid={errors.lastName ? true : undefined}
                      {...register("lastName")}
                    />
                  </Field>
                </div>

                <Field
                  id="cu-userName"
                  label="Username"
                  required
                  hint="Letters, digits, dot, dash or underscore. 3–32 characters."
                  error={errors.userName?.message}
                >
                  <Input
                    id="cu-userName"
                    placeholder="m.chen"
                    autoComplete="off"
                    className="font-mono"
                    aria-invalid={errors.userName ? true : undefined}
                    {...register("userName")}
                  />
                </Field>

                <Field
                  id="cu-email"
                  label="Email"
                  required
                  error={errors.email?.message}
                >
                  <Input
                    id="cu-email"
                    type="email"
                    placeholder="user@example.com"
                    autoComplete="email"
                    className="font-mono"
                    aria-invalid={errors.email ? true : undefined}
                    {...register("email")}
                  />
                </Field>

                <Field
                  id="cu-phoneNumber"
                  label="Phone (optional)"
                  error={errors.phoneNumber?.message}
                >
                  <Input
                    id="cu-phoneNumber"
                    type="tel"
                    placeholder="+1 555 0100"
                    autoComplete="tel"
                    className="font-mono"
                    {...register("phoneNumber")}
                  />
                </Field>
              </div>
            </div>

            {/* ── Credentials section ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={KeyRound}
                title="Credentials"
                description="Initial password. The user is encouraged to change it on first sign-in."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  id="cu-password"
                  label="Password"
                  required
                  error={errors.password?.message}
                >
                  <Input
                    id="cu-password"
                    type="password"
                    autoComplete="new-password"
                    className="font-mono"
                    aria-invalid={errors.password ? true : undefined}
                    {...register("password")}
                  />
                </Field>
                <Field
                  id="cu-confirmPassword"
                  label="Confirm password"
                  required
                  error={errors.confirmPassword?.message}
                >
                  <Input
                    id="cu-confirmPassword"
                    type="password"
                    autoComplete="new-password"
                    className="font-mono"
                    aria-invalid={errors.confirmPassword ? true : undefined}
                    {...register("confirmPassword")}
                  />
                </Field>
              </div>
            </div>
          </DialogBody>

          {/* ── Footer ── */}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={submitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Creating…" : "Create account"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
