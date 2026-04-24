import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createTenant } from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiRequestError } from "@/lib/api-client";

const TENANT_ID_PATTERN = /^[a-z0-9][a-z0-9-]{1,62}[a-z0-9]$/;

export function CreateTenantPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [id, setId] = useState("");
  const [name, setName] = useState("");
  const [adminEmail, setAdminEmail] = useState("");
  const [issuer, setIssuer] = useState("");
  const [connectionString, setConnectionString] = useState("");

  const mutation = useMutation({
    mutationFn: createTenant,
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
          : err instanceof Error
            ? err.message
            : "Failed to create tenant";
      toast.error("Create failed", { description: detail });
    },
  });

  const idInvalid = id.length > 0 && !TENANT_ID_PATTERN.test(id);

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate({
      id: id.trim(),
      name: name.trim(),
      adminEmail: adminEmail.trim(),
      issuer: issuer.trim(),
      connectionString: connectionString.trim() ? connectionString.trim() : null,
    });
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">New tenant</h1>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Provision a new tenant and its seed admin user.
        </p>
      </div>

      <Card className="max-w-2xl">
        <form onSubmit={onSubmit}>
          <CardHeader>
            <CardTitle>Tenant details</CardTitle>
            <CardDescription>
              The tenant identifier is used as a subdomain-like slug and must be URL-safe.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <Field
              id="id"
              label="Identifier"
              hint="Lowercase letters, digits, and hyphens. 3–64 chars."
              value={id}
              onChange={setId}
              autoComplete="off"
              required
              error={idInvalid ? "Invalid format." : undefined}
              placeholder="acme-corp"
            />
            <Field id="name" label="Display name" value={name} onChange={setName} required placeholder="Acme Corp" />
            <Field
              id="adminEmail"
              label="Admin email"
              type="email"
              value={adminEmail}
              onChange={setAdminEmail}
              required
              placeholder="admin@acme.example"
            />
            <Field id="issuer" label="JWT issuer" value={issuer} onChange={setIssuer} required placeholder="acme-corp.issuer" />
            <Field
              id="connectionString"
              label="Connection string (optional)"
              hint="Leave blank to share the default database."
              value={connectionString}
              onChange={setConnectionString}
              placeholder=""
            />
          </CardContent>
          <CardFooter className="gap-2">
            <Button type="submit" disabled={mutation.isPending || idInvalid}>
              {mutation.isPending ? "Creating…" : "Create tenant"}
            </Button>
            <Button type="button" variant="outline" onClick={() => navigate("/tenants")} disabled={mutation.isPending}>
              Cancel
            </Button>
          </CardFooter>
        </form>
      </Card>
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
};

function Field({ id, label, value, onChange, type, required, hint, error, placeholder, autoComplete }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        type={type ?? "text"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        required={required}
        placeholder={placeholder}
        autoComplete={autoComplete}
        aria-invalid={error ? true : undefined}
      />
      {hint && <p className="text-xs text-[var(--color-muted-foreground)]">{hint}</p>}
      {error && <p className="text-xs text-[var(--color-destructive)]">{error}</p>}
    </div>
  );
}
