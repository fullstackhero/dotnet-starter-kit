import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Webhook, X } from "lucide-react";
import { toast } from "sonner";
import {
  SUGGESTED_EVENT_TYPES,
  createWebhookSubscription,
} from "@/api/webhooks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

const schema = z.object({
  url: z
    .string()
    .trim()
    .url("Must be a valid http(s) URL.")
    .refine((u) => u.startsWith("https://") || u.startsWith("http://"), {
      message: "Use http:// or https://",
    }),
  secret: z
    .string()
    .trim()
    .max(256)
    .optional(),
});

type FormValues = z.infer<typeof schema>;

export function CreateWebhookDialog({
  open,
  onOpenChange,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (id: string) => void;
}) {
  const [events, setEvents] = useState<string[]>([]);
  const [draftEvent, setDraftEvent] = useState("");

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { url: "", secret: "" },
  });

  const reset_ = () => {
    reset();
    setEvents([]);
    setDraftEvent("");
  };

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      createWebhookSubscription({
        url: values.url,
        secret: values.secret,
        events,
      }),
    onSuccess: (id) => {
      toast.success("Subscription created", {
        description: "Use the Test button on the row to verify the endpoint accepts events.",
      });
      onCreated?.(id);
      reset_();
      onOpenChange(false);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : (err as Error).message;
      toast.error("Create failed", { description: detail });
    },
  });

  const onSubmit = handleSubmit((values) => {
    if (events.length === 0) {
      toast.warning("Pick at least one event", {
        description: "Subscriptions with no events would never fire.",
      });
      return;
    }
    mutation.mutate(values);
  });

  const addEvent = (raw: string) => {
    const name = raw.trim().toLowerCase();
    if (!name) return;
    if (events.includes(name)) return;
    setEvents((prev) => [...prev, name]);
    setDraftEvent("");
  };

  const removeEvent = (name: string) => {
    setEvents((prev) => prev.filter((e) => e !== name));
  };

  const submitting = isSubmitting || mutation.isPending;
  const suggestionsToShow = SUGGESTED_EVENT_TYPES.filter((s) => !events.includes(s));

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset_();
        onOpenChange(o);
      }}
    >
      <DialogContent size="lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <span
              aria-hidden
              className="grid h-7 w-7 place-items-center rounded-md bg-[var(--color-accent-signal)]/15 text-[var(--color-accent-signal)]"
            >
              <Webhook className="h-4 w-4" />
            </span>
            <DialogTitle>New webhook subscription</DialogTitle>
          </div>
          <DialogDescription>
            Your endpoint receives a JSON payload with the event details. We sign each request
            with HMAC-SHA256 in the <code className="code-chip">X-FSH-Signature</code> header
            using the secret below — store it on your side and verify before trusting the body.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit}>
          <DialogBody className="space-y-5">
            <Field id="webhook-url" label="Endpoint URL" required error={errors.url?.message}>
              <Input
                id="webhook-url"
                type="url"
                placeholder="https://api.acme.com/webhooks/fsh"
                autoComplete="off"
                className="font-mono"
                aria-invalid={errors.url ? true : undefined}
                {...register("url")}
              />
            </Field>

            <Field
              id="webhook-secret"
              label="Signing secret"
              hint="Optional but recommended. At least 32 random characters. Used to compute the HMAC."
              error={errors.secret?.message}
            >
              <Input
                id="webhook-secret"
                type="password"
                autoComplete="new-password"
                placeholder="Leave blank to skip signing"
                className="font-mono"
                {...register("secret")}
              />
            </Field>

            <div className="space-y-2">
              <div className="meta text-[var(--color-muted-foreground)]">
                Events ({events.length})
                <span className="text-[var(--color-destructive)]" aria-hidden> ·</span>
              </div>
              <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-[var(--color-input)] bg-transparent p-2 min-h-10">
                {events.map((e) => (
                  <span
                    key={e}
                    className="inline-flex items-center gap-1 rounded-md bg-[var(--color-accent-signal)]/15 px-2 py-0.5 font-mono text-[11px] text-[var(--color-foreground)]"
                  >
                    {e}
                    <button
                      type="button"
                      onClick={() => removeEvent(e)}
                      aria-label={`Remove ${e}`}
                      className="text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </span>
                ))}
                <input
                  value={draftEvent}
                  onChange={(e) => setDraftEvent(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === ",") {
                      e.preventDefault();
                      addEvent(draftEvent);
                    } else if (e.key === "Backspace" && draftEvent === "" && events.length > 0) {
                      setEvents((prev) => prev.slice(0, -1));
                    }
                  }}
                  placeholder={events.length === 0 ? "type an event name then Enter…" : "add another…"}
                  className="min-w-[10rem] flex-1 bg-transparent font-mono text-xs outline-none placeholder:text-[var(--color-muted-foreground)]/70"
                />
              </div>
              {suggestionsToShow.length > 0 && (
                <div className="flex flex-wrap items-center gap-1.5 pt-1">
                  <span className="meta text-[var(--color-muted-foreground)]">suggested</span>
                  {suggestionsToShow.map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => addEvent(s)}
                      className={cn(
                        "inline-flex items-center gap-1 rounded-md border border-[var(--color-border)] bg-transparent px-2 py-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)] transition-colors",
                        "hover:border-[var(--color-accent-signal)] hover:text-[var(--color-foreground)]",
                      )}
                    >
                      <Plus className="h-2.5 w-2.5" />
                      {s}
                    </button>
                  ))}
                </div>
              )}
            </div>
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={submitting}>
              Cancel
            </Button>
            <Button type="submit" variant="signal" disabled={submitting}>
              {submitting ? "Creating…" : "Create subscription"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
