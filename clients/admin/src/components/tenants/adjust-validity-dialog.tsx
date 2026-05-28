import { useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { CalendarCog } from "lucide-react";
import { toast } from "sonner";
import { adjustTenantValidity } from "@/api/tenants";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/list";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ApiRequestError } from "@/lib/api-client";

// A `type="date"` input yields a `YYYY-MM-DD` string. zod validates the shape
// and that it parses to a real calendar date.
const schema = z.object({
  validUpto: z
    .string()
    .min(1, "Pick a date.")
    .refine((v) => !Number.isNaN(new Date(v).getTime()), "Enter a valid date."),
});

type FormValues = z.infer<typeof schema>;

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

function formatDate(value?: string | null): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString();
}

/** `YYYY-MM-DD` (the native date input value) for an ISO/date string, for prefill. */
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "";
  return d.toISOString().slice(0, 10);
}

/**
 * Operator override that sets a tenant's ValidUpto directly with NO invoice —
 * a comp/correction, distinct from Renew (which issues a term invoice).
 * Backdating is permitted server-side. Root-operator only.
 */
export function AdjustValidityDialog({
  open,
  onOpenChange,
  tenantId,
  validUpto,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tenantId: string;
  validUpto?: string;
}) {
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { validUpto: "" },
  });

  // Prefill with the tenant's current validity each time the dialog opens.
  useEffect(() => {
    if (open) reset({ validUpto: toDateInputValue(validUpto) });
  }, [open, validUpto, reset]);

  const mutation = useMutation({
    // Pass the date via mutate(arg) — never close over form state at submit time.
    mutationFn: (value: string) => adjustTenantValidity(tenantId, new Date(value).toISOString()),
    onSuccess: (result) => {
      toast.success("Validity adjusted", {
        description: `Valid until ${formatDate(result.validUpto)}. No invoice was issued.`,
      });
      queryClient.invalidateQueries({ queryKey: ["tenant", tenantId] });
      queryClient.invalidateQueries({ queryKey: ["tenants"] });
      handleClose();
    },
    onError: (err) => toast.error("Adjust failed", { description: describe(err, "Could not adjust validity.") }),
  });

  function handleClose() {
    reset({ validUpto: "" });
    onOpenChange(false);
  }

  const onSubmit = handleSubmit((values) => mutation.mutate(values.validUpto));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) handleClose();
        else onOpenChange(true);
      }}
    >
      <DialogContent size="md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 shrink-0 place-items-center rounded-xl
                bg-[oklch(from_var(--color-primary)_l_c_h_/_0.12)] text-[var(--color-primary)]
                ring-1 ring-inset ring-[oklch(from_var(--color-primary)_l_c_h_/_0.18)]"
            >
              <CalendarCog className="h-[18px] w-[18px]" />
            </span>
            <DialogTitle className="text-[16px]">Adjust validity</DialogTitle>
          </div>
          <DialogDescription className="mt-1">
            Set this tenant's expiry date directly — an operator override with{" "}
            <strong className="text-[var(--color-foreground)]">no invoice</strong>. Use for comps or
            corrections; renewals that should bill belong in Renew. Currently valid until{" "}
            {formatDate(validUpto)}.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-4">
            <Field
              id="av-validUpto"
              label="Valid until"
              required
              hint="Backdating is allowed. No invoice is issued for an adjustment."
              error={errors.validUpto?.message}
            >
              <Input id="av-validUpto" type="date" {...register("validUpto")} />
            </Field>
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose} disabled={submitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Saving…" : "Adjust validity"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
