import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Building2,
  Database,
  KeyRound,
  UserRound,
  Sparkles,
} from "lucide-react";
import { toast } from "sonner";
import { createTenant } from "@/api/tenants";
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

// ─── Schema (identical to the old page) ─────────────────────────────────────

const TENANT_ID_RE = /^[a-z0-9][a-z0-9-]{1,62}[a-z0-9]$/;

const schema = z.object({
  id: z
    .string()
    .trim()
    .regex(
      TENANT_ID_RE,
      "Lowercase letters, digits, hyphens. 3–64 chars. No leading/trailing hyphen.",
    ),
  name: z.string().trim().min(2, "At least 2 characters.").max(128),
  adminEmail: z.string().trim().email("Enter a valid email."),
  adminPassword: z
    .string()
    .min(8, "At least 8 characters.")
    .max(128, "Maximum 128 characters."),
  issuer: z.string().trim().min(2, "Required.").max(256),
  connectionString: z.string().trim().max(2048).optional(),
});

type FormValues = z.infer<typeof schema>;

// ─── Section header (inline, no card — we're inside the dialog already) ─────

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

// ─── Dialog ──────────────────────────────────────────────────────────────────

export function CreateTenantDialog({
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
      id: "",
      name: "",
      adminEmail: "",
      adminPassword: "",
      issuer: "",
      connectionString: "",
    },
  });

  const mutation = useMutation({
    // Pass values via mutate(arg) — no closed-over state captured at submit time.
    mutationFn: (values: FormValues) =>
      createTenant({
        id: values.id,
        name: values.name,
        adminEmail: values.adminEmail,
        adminPassword: values.adminPassword,
        issuer: values.issuer,
        connectionString: values.connectionString?.trim() ? values.connectionString : null,
      }),
    onSuccess: (result) => {
      toast.success(`Tenant ${result.id} created`, {
        description:
          "Provisioning runs in the background. Track progress on the detail page.",
      });
      queryClient.invalidateQueries({ queryKey: ["tenants"] });
      handleClose();
      navigate(`/tenants/${result.id}`);
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
            {/* Icon badge — geometric accent tile */}
            <span
              aria-hidden
              className="relative grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)]
                text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <Building2 className="h-[18px] w-[18px]" />
              {/* Subtle sparkle accent — top-right corner */}
              <Sparkles
                className="absolute right-0.5 top-0.5 h-2.5 w-2.5 opacity-40"
              />
            </span>
            <div className="min-w-0">
              <DialogTitle className="text-[16px]">New tenant</DialogTitle>
            </div>
          </div>
          <DialogDescription className="mt-1">
            Provision a new tenant and its seed admin user. The identifier is the
            URL-safe slug used in subdomain-like routing and JWT claims.
          </DialogDescription>
        </DialogHeader>

        {/* ── Form ── */}
        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-6">
            {/* ── Identity section ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={UserRound}
                title="Identity"
                description="How the tenant is named on the platform. The identifier is immutable."
              />
              {/* Ruled separator */}
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <div className="grid gap-4 sm:grid-cols-2">
                <Field
                  id="ct-id"
                  label="Identifier"
                  required
                  hint="Lowercase letters, digits, and hyphens. 3–64 characters."
                  error={errors.id?.message}
                >
                  <Input
                    id="ct-id"
                    autoComplete="off"
                    placeholder="acme-corp"
                    className="font-mono"
                    {...register("id")}
                  />
                </Field>

                <Field
                  id="ct-name"
                  label="Display name"
                  required
                  error={errors.name?.message}
                >
                  <Input
                    id="ct-name"
                    placeholder="Acme Corp"
                    {...register("name")}
                  />
                </Field>

                <Field
                  id="ct-adminEmail"
                  label="Admin email"
                  required
                  error={errors.adminEmail?.message}
                >
                  <Input
                    id="ct-adminEmail"
                    type="email"
                    placeholder="admin@acme.example"
                    className="font-mono"
                    {...register("adminEmail")}
                  />
                </Field>

                <Field
                  id="ct-adminPassword"
                  label="Initial admin password"
                  required
                  hint="The first admin signs in with this. They can rotate it after first login."
                  error={errors.adminPassword?.message}
                >
                  <Input
                    id="ct-adminPassword"
                    type="password"
                    autoComplete="new-password"
                    placeholder="Min 8 characters"
                    {...register("adminPassword")}
                  />
                </Field>
              </div>
            </div>

            {/* ── Security section ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={KeyRound}
                title="Security"
                description="JWT issuer claim emitted by tokens for this tenant. Used to scope sessions."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <Field
                id="ct-issuer"
                label="JWT issuer"
                required
                error={errors.issuer?.message}
              >
                <Input
                  id="ct-issuer"
                  placeholder="acme-corp.issuer"
                  className="font-mono"
                  {...register("issuer")}
                />
              </Field>
            </div>

            {/* ── Database section ── */}
            <div className="space-y-3">
              <SectionLabel
                icon={Database}
                title="Database"
                description="Optional dedicated connection string. Leave blank to share the default database."
              />
              <div className="h-px bg-[var(--color-border)] opacity-60" />
              <Field
                id="ct-connectionString"
                label="Connection string"
                hint="Optional. Leave blank to use the shared catalog."
                error={errors.connectionString?.message}
              >
                <Input
                  id="ct-connectionString"
                  placeholder="Host=…;Database=…"
                  className="font-mono"
                  {...register("connectionString")}
                />
              </Field>
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
              {submitting ? "Provisioning…" : "Create tenant"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
