import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  ChevronDown,
  CircleCheck,
  Eye,
  EyeOff,
  Loader2,
  Lock,
  Pencil,
  Sparkles,
  Wand2,
} from "lucide-react";
import { toast } from "sonner";
import { createTenant } from "@/api/tenants";
import { getPlans, planTermPrice } from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Monogram } from "@/components/monogram";
import { Field, Select, type SelectOption } from "@/components/list";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

// ─── Schema (unchanged contract) ────────────────────────────────────────────

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
  // Optional: preselected to the default plan when plans load; if left empty the
  // server falls back to the configured trial plan.
  planKey: z.string().trim().optional(),
});

type FormValues = z.infer<typeof schema>;

function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

/** Derive a URL-safe slug from free text. Trailing/leading hyphens trimmed; capped to 64. */
function slugify(input: string): string {
  return input
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 64);
}

/** Strong random password from a copy-friendly charset (no ambiguous 0/O/1/l/I). */
function generatePassword(length = 16): string {
  const charset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*?";
  const bytes = new Uint32Array(length);
  crypto.getRandomValues(bytes);
  let out = "";
  for (let i = 0; i < length; i++) out += charset[bytes[i] % charset.length];
  return out;
}

// ─── Small inline adornment button (eye / generate) ─────────────────────────

function AdornButton({
  label,
  onClick,
  children,
}: {
  label: string;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className={cn(
        "grid size-7 shrink-0 place-items-center rounded-md outline-none transition-colors cursor-pointer",
        "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        "focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
        "[&_svg]:size-3.5",
      )}
    >
      {children}
    </button>
  );
}

// ─── Live preview rail ───────────────────────────────────────────────────────

function PreviewRail({
  name,
  slug,
  planLabel,
  email,
}: {
  name: string;
  slug: string;
  planLabel: string | null;
  email: string;
}) {
  const displayName = name.trim() || "New tenant";
  const displaySlug = slug || "tenant-id";

  return (
    <aside
      className={cn(
        "relative flex items-center gap-4 overflow-hidden p-5 sm:flex-col sm:items-start sm:gap-0 sm:p-6",
        "border-b border-[var(--color-border)] sm:border-b-0 sm:border-r",
        "bg-gradient-to-b from-[oklch(from_var(--color-primary)_l_c_h_/_0.06)] to-[var(--color-muted)]/40",
      )}
    >
      {/* Soft radial glow behind the monogram for depth */}
      <div
        aria-hidden
        className="pointer-events-none absolute -left-8 -top-10 h-40 w-40 rounded-full opacity-60 blur-2xl
          bg-[radial-gradient(circle,oklch(from_var(--color-primary)_l_c_h_/_0.18),transparent_70%)]"
      />

      <div className="relative shrink-0">
        <Monogram seed={slug || name || "new"} fallback={displayName} size="lg" />
      </div>

      <div className="relative min-w-0 sm:mt-4">
        <p className="truncate font-display text-[15px] font-semibold leading-tight text-[var(--color-foreground)]">
          {displayName}
        </p>
        <p className="mt-1 truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
          {displaySlug}
        </p>

        {email.trim() && (
          <p className="mt-0.5 hidden truncate font-mono text-[11px] text-[var(--color-muted-foreground)]/80 sm:block">
            {email.trim()}
          </p>
        )}

        {/* Live facts — hidden on mobile to keep the banner compact */}
        <dl className="mt-4 hidden space-y-2 sm:block">
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="size-1.5 rounded-full bg-[var(--color-primary)] ring-2 ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            />
            <span className="text-[12px] text-[var(--color-muted-foreground)]">
              {planLabel ?? "Default plan"}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="size-1.5 rounded-full bg-[var(--color-success)] ring-2 ring-[oklch(from_var(--color-success)_l_c_h_/_0.18)]"
            />
            <span className="text-[12px] text-[var(--color-muted-foreground)]">
              Active on creation
            </span>
          </div>
        </dl>
      </div>

      <p className="relative mt-auto hidden pt-6 text-[11px] leading-relaxed text-[var(--color-muted-foreground)]/75 sm:block">
        Provisioning runs in the background. You can track progress on the tenant&apos;s detail page.
      </p>
    </aside>
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

  // UI-only state, reset on close.
  const [idMode, setIdMode] = useState<"auto" | "manual">("auto");
  const [issuerDirty, setIssuerDirty] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [advancedOpen, setAdvancedOpen] = useState(false);

  const plansQuery = useQuery({
    queryKey: ["billing", "plans", "active"],
    queryFn: () => getPlans(false),
    enabled: open,
  });

  const planOptions: SelectOption[] = (plansQuery.data ?? []).map((p) => ({
    value: p.key,
    label: p.name,
    hint: `${p.interval} · ${formatMoney(planTermPrice(p), p.currency)}`,
  }));
  // Prefer the conventional trial plan, else the first active plan.
  const defaultPlanKey =
    plansQuery.data?.find((p) => p.key === "free")?.key ?? plansQuery.data?.[0]?.key ?? "";

  const {
    register,
    handleSubmit,
    control,
    reset,
    setValue,
    getValues,
    watch,
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
      planKey: "",
    },
  });

  // Live values for the preview rail + smart-field wiring.
  const name = watch("name");
  const idValue = watch("id");
  const adminEmail = watch("adminEmail");
  const planKey = watch("planKey");

  const idValid = TENANT_ID_RE.test((idValue ?? "").trim());
  const idTouched = (idValue ?? "").trim().length > 0;

  const selectedPlan = plansQuery.data?.find((p) => p.key === planKey);
  const planLabel = selectedPlan
    ? `${selectedPlan.name} · ${formatMoney(planTermPrice(selectedPlan), selectedPlan.currency)}`
    : null;

  // Preselect the default/trial plan once plans load (without clobbering a choice).
  useEffect(() => {
    if (defaultPlanKey && !getValues("planKey")) {
      setValue("planKey", defaultPlanKey);
    }
  }, [defaultPlanKey, getValues, setValue]);

  // Auto-derive the identifier slug from the display name until the operator
  // unlocks the field for manual editing.
  useEffect(() => {
    if (idMode !== "auto") return;
    setValue("id", slugify(name ?? ""), { shouldValidate: false });
  }, [name, idMode, setValue]);

  // JWT issuer mirrors the identifier until the operator edits it by hand.
  useEffect(() => {
    if (issuerDirty) return;
    setValue("issuer", (idValue ?? "").trim(), { shouldValidate: false });
  }, [idValue, issuerDirty, setValue]);

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
        planKey: values.planKey?.trim() ? values.planKey : null,
      }),
    onSuccess: (result) => {
      toast.success(`Tenant ${result.id} created`, {
        description:
          "Provisioning runs in the background. Track progress on the detail page.",
      });
      // Fire-and-forget refresh — don't block navigation on the list refetch.
      void queryClient.invalidateQueries({ queryKey: ["tenants"] });
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
    setIdMode("auto");
    setIssuerDirty(false);
    setShowPassword(false);
    setAdvancedOpen(false);
    onOpenChange(false);
  }

  function unlockIdentifier() {
    setIdMode("manual");
    requestAnimationFrame(() => document.getElementById("ct-id")?.focus());
  }

  function relockIdentifier() {
    setIdMode("auto");
    setValue("id", slugify(getValues("name") ?? ""), { shouldValidate: false });
  }

  function fillGeneratedPassword() {
    setValue("adminPassword", generatePassword(), { shouldValidate: true });
    setShowPassword(true);
  }

  const onSubmit = handleSubmit((values) => mutation.mutate(values));
  const submitting = isSubmitting || mutation.isPending;

  const issuerField = register("issuer");

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) handleClose();
        else onOpenChange(true);
      }}
    >
      <DialogContent size="lg" className="overflow-hidden p-0 sm:max-w-3xl">
        <form onSubmit={onSubmit} className="grid sm:grid-cols-[13.5rem_1fr]">
          {/* ── Live preview rail ── */}
          <PreviewRail
            name={name ?? ""}
            slug={idValue ?? ""}
            planLabel={planLabel}
            email={adminEmail ?? ""}
          />

          {/* ── Form pane ── */}
          <div className="min-w-0">
            {/* Header (leave room for the close affordance, top-right) */}
            <div className="flex flex-col gap-1 px-6 pb-2 pt-6 pr-12">
              <div className="flex items-center gap-2">
                <DialogTitle className="text-[16px]">New tenant</DialogTitle>
                <Sparkles className="size-3.5 text-[var(--color-primary)] opacity-70" aria-hidden />
              </div>
              <DialogDescription>
                Provision a tenant and its seed admin. The identifier is the URL-safe slug used in
                routing and JWT claims.
              </DialogDescription>
            </div>

            {/* Fields */}
            <div className="space-y-4 px-6 py-4">
              <Field id="ct-name" label="Display name" required error={errors.name?.message}>
                <Input
                  id="ct-name"
                  autoComplete="off"
                  placeholder="Acme Corp"
                  {...register("name")}
                />
              </Field>

              {/* Identifier — auto-derived, unlock to edit */}
              <Field
                id="ct-id"
                label="Identifier"
                required
                hint={
                  idTouched && idValid ? (
                    <span className="inline-flex items-center gap-1 text-[var(--color-success)]">
                      <CircleCheck className="size-3.5" aria-hidden />
                      Valid format — availability is confirmed when you create.
                    </span>
                  ) : (
                    "Lowercase letters, digits, and hyphens. 3–64 characters."
                  )
                }
                error={errors.id?.message}
              >
                <div className="relative">
                  <Input
                    id="ct-id"
                    autoComplete="off"
                    placeholder="acme-corp"
                    readOnly={idMode === "auto"}
                    className={cn(
                      "pr-[4.75rem] font-mono",
                      idMode === "auto" && "text-[var(--color-muted-foreground)]",
                    )}
                    {...register("id")}
                  />
                  <div className="absolute inset-y-0 right-1.5 flex items-center gap-1">
                    {idMode === "auto" ? (
                      <button
                        type="button"
                        onClick={unlockIdentifier}
                        className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-[11px] font-medium
                          text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)]
                          hover:text-[var(--color-foreground)] cursor-pointer outline-none
                          focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
                      >
                        <Pencil className="size-3" aria-hidden /> Edit
                      </button>
                    ) : (
                      <button
                        type="button"
                        onClick={relockIdentifier}
                        className="inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-[11px] font-medium
                          text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)]
                          hover:text-[var(--color-foreground)] cursor-pointer outline-none
                          focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
                      >
                        <Lock className="size-3" aria-hidden /> Auto
                      </button>
                    )}
                  </div>
                </div>
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
                  autoComplete="off"
                  placeholder="admin@acme.example"
                  className="font-mono"
                  {...register("adminEmail")}
                />
              </Field>

              {/* Password — generate + show/hide */}
              <Field
                id="ct-adminPassword"
                label="Initial admin password"
                required
                hint="The first admin signs in with this and can rotate it after first login."
                error={errors.adminPassword?.message}
              >
                <div className="relative">
                  <Input
                    id="ct-adminPassword"
                    type={showPassword ? "text" : "password"}
                    autoComplete="new-password"
                    placeholder="Min 8 characters"
                    className="pr-16 font-mono"
                    {...register("adminPassword")}
                  />
                  <div className="absolute inset-y-0 right-1.5 flex items-center gap-0.5">
                    <AdornButton label="Generate strong password" onClick={fillGeneratedPassword}>
                      <Wand2 aria-hidden />
                    </AdornButton>
                    <AdornButton
                      label={showPassword ? "Hide password" : "Show password"}
                      onClick={() => setShowPassword((s) => !s)}
                    >
                      {showPassword ? <EyeOff aria-hidden /> : <Eye aria-hidden />}
                    </AdornButton>
                  </div>
                </div>
              </Field>

              {/* Plan */}
              <Field
                id="ct-plan"
                label="Billing plan"
                hint={
                  plansQuery.isError
                    ? "Could not load plans — the tenant will fall back to the default plan."
                    : "Sets the first invoice and how long the tenant stays valid. Defaults to the trial plan."
                }
                error={errors.planKey?.message}
              >
                <Controller
                  control={control}
                  name="planKey"
                  render={({ field }) => (
                    <Select
                      id="ct-plan"
                      value={field.value ?? ""}
                      onValueChange={field.onChange}
                      options={planOptions}
                      emptyLabel={
                        plansQuery.isLoading
                          ? "Loading plans…"
                          : planOptions.length === 0
                            ? "No active plans"
                            : undefined
                      }
                      disabled={plansQuery.isLoading || planOptions.length === 0}
                    />
                  )}
                />
              </Field>

              {/* Advanced disclosure — issuer + dedicated database */}
              <div className="rounded-lg border border-[var(--color-border)]">
                <button
                  type="button"
                  onClick={() => setAdvancedOpen((o) => !o)}
                  aria-expanded={advancedOpen}
                  className="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2.5 text-left outline-none
                    transition-colors hover:bg-[var(--color-accent)] cursor-pointer
                    focus-visible:ring-2 focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
                >
                  <span className="text-[12.5px] font-medium text-[var(--color-foreground)]">
                    Advanced
                    <span className="ml-1.5 font-normal text-[var(--color-muted-foreground)]">
                      issuer, dedicated database
                    </span>
                  </span>
                  <ChevronDown
                    aria-hidden
                    className={cn(
                      "size-4 shrink-0 text-[var(--color-muted-foreground)] transition-transform duration-[var(--duration-fast)]",
                      advancedOpen && "rotate-180",
                    )}
                  />
                </button>

                {advancedOpen && (
                  <div className="space-y-4 border-t border-[var(--color-border)] px-3 py-3.5">
                    <Field
                      id="ct-issuer"
                      label="JWT issuer"
                      required
                      hint="Mirrors the identifier by default. Issued in tokens to scope sessions."
                      error={errors.issuer?.message}
                    >
                      <Input
                        id="ct-issuer"
                        placeholder="acme-corp"
                        className="font-mono"
                        {...issuerField}
                        onChange={(e) => {
                          setIssuerDirty(true);
                          void issuerField.onChange(e);
                        }}
                      />
                    </Field>

                    <Field
                      id="ct-connectionString"
                      label="Connection string"
                      hint="Optional. Leave blank to use the shared catalog database."
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
                )}
              </div>
            </div>

            {/* Footer */}
            <DialogFooter className="px-6">
              <Button type="button" variant="outline" onClick={handleClose} disabled={submitting}>
                Cancel
              </Button>
              <Button type="submit" disabled={submitting} className="min-w-[8.5rem]">
                {submitting ? (
                  <>
                    <Loader2 className="size-4 animate-spin" aria-hidden />
                    <span>Provisioning…</span>
                  </>
                ) : (
                  "Create tenant"
                )}
              </Button>
            </DialogFooter>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
