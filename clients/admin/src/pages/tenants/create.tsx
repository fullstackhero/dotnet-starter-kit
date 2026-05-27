import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft, Building2, Database, KeyRound, UserRound } from "lucide-react";
import { toast } from "sonner";
import { createTenant } from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  EntityPageHeader,
  Field,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";

const TENANT_ID_RE = /^[a-z0-9][a-z0-9-]{1,62}[a-z0-9]$/;

const schema = z.object({
  id: z
    .string()
    .trim()
    .regex(TENANT_ID_RE, "Lowercase letters, digits, hyphens. 3–64 chars. No leading/trailing hyphen."),
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

export function CreateTenantPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
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
        description: "Provisioning runs in the background. Track progress on the detail page.",
      });
      queryClient.invalidateQueries({ queryKey: ["tenants"] });
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

  const onSubmit = handleSubmit((values) => mutation.mutate(values));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <div className="space-y-8">
      <EntityPageHeader
        icon={Building2}
        title="New tenant"
        tone="info"
        description="Provision a new tenant and its seed admin user. The identifier is the URL-safe slug used in subdomain-like routing and JWT claims."
      >
        <Button variant="ghost" size="sm" onClick={() => navigate("/tenants")}>
          <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Registry
        </Button>
      </EntityPageHeader>

      <form onSubmit={onSubmit} className="max-w-2xl space-y-4">
        <SettingsSection
          title="Identity"
          icon={UserRound}
          description="How the tenant is named on the platform. The identifier is immutable."
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              id="id"
              label="Identifier"
              required
              hint="Lowercase letters, digits, and hyphens. 3–64 characters."
              error={errors.id?.message}
            >
              <Input
                id="id"
                autoComplete="off"
                placeholder="acme-corp"
                className="font-mono"
                aria-invalid={errors.id ? true : undefined}
                {...register("id")}
              />
            </Field>
            <Field id="name" label="Display name" required error={errors.name?.message}>
              <Input
                id="name"
                placeholder="Acme Corp"
                aria-invalid={errors.name ? true : undefined}
                {...register("name")}
              />
            </Field>
            <Field id="adminEmail" label="Admin email" required error={errors.adminEmail?.message}>
              <Input
                id="adminEmail"
                type="email"
                placeholder="admin@acme.example"
                className="font-mono"
                aria-invalid={errors.adminEmail ? true : undefined}
                {...register("adminEmail")}
              />
            </Field>
            <Field
              id="adminPassword"
              label="Initial admin password"
              required
              hint="The first admin will sign in with this. They can rotate it after first login."
              error={errors.adminPassword?.message}
            >
              <Input
                id="adminPassword"
                type="password"
                autoComplete="new-password"
                placeholder="Min 8 characters"
                aria-invalid={errors.adminPassword ? true : undefined}
                {...register("adminPassword")}
              />
            </Field>
          </div>
        </SettingsSection>

        <SettingsSection
          title="Security"
          icon={KeyRound}
          description="JWT issuer claim emitted by tokens for this tenant. Used to scope sessions."
        >
          <Field id="issuer" label="JWT issuer" required error={errors.issuer?.message}>
            <Input
              id="issuer"
              placeholder="acme-corp.issuer"
              className="font-mono"
              aria-invalid={errors.issuer ? true : undefined}
              {...register("issuer")}
            />
          </Field>
        </SettingsSection>

        <SettingsSection
          title="Database"
          icon={Database}
          description="Optional dedicated connection string. Leave blank to share the default database with row-level tenant scoping."
        >
          <Field
            id="connectionString"
            label="Connection string"
            hint="Optional. Leave blank to use the shared catalog."
            error={errors.connectionString?.message}
          >
            <Input
              id="connectionString"
              placeholder="Host=…;Database=…"
              className="font-mono"
              aria-invalid={errors.connectionString ? true : undefined}
              {...register("connectionString")}
            />
          </Field>
        </SettingsSection>

        <div className="flex flex-wrap items-center gap-2 pt-2">
          <Button type="submit" disabled={submitting}>
            {submitting ? "Provisioning…" : "Create tenant"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate("/tenants")}
            disabled={submitting}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
