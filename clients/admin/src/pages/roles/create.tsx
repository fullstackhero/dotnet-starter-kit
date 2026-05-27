import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft } from "lucide-react";
import { toast } from "sonner";
import { upsertRole, type RoleDto } from "@/api/roles";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  PageHeader,
  Field,
  FormShell,
  FormSection,
  FormActions,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "At least 2 characters.")
    .max(64, "Keep under 64 characters."),
  description: z.string().trim().max(256, "Keep under 256 characters.").optional(),
});

type FormValues = z.infer<typeof schema>;

export function CreateRolePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "" },
  });

  const mutation = useMutation<RoleDto, Error, FormValues>({
    mutationFn: (values) =>
      upsertRole({
        id: "",
        name: values.name,
        description: values.description?.trim() ? values.description : null,
      }),
    onSuccess: (result) => {
      toast.success(`Role ${result.name} created`);
      queryClient.invalidateQueries({ queryKey: ["roles"] });
      navigate(`/roles/${result.id}`);
    },
    onError: (err) => {
      const detail =
        err instanceof ApiRequestError
          ? err.problem?.detail ?? err.problem?.title ?? err.message
          : err.message;
      toast.error("Create failed", { description: detail });
    },
  });

  const onSubmit = handleSubmit((values) => mutation.mutate(values));
  const submitting = isSubmitting || mutation.isPending;

  return (
    <div className="space-y-8">
      <PageHeader
        crumbs={[{ label: "\\ Roles" }, { label: "New", muted: true }]}
        trailing="Role · Draft"
        title="New role"
        description="Create a role, then grant it permissions on its detail page."
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate("/roles")}>
            <ArrowLeft className="mr-1 h-3.5 w-3.5" /> Registry
          </Button>
        }
      />

      <form onSubmit={onSubmit}>
        <FormShell maxWidth="md">
          <FormSection
            title="Identity"
            description={
              <>
                The role name is what shows up in user role assignments. Choose
                something descriptive — &ldquo;Support agent&rdquo; reads better
                than &ldquo;Tier-2&rdquo;.
              </>
            }
          >
            <Field id="name" label="Name" required error={errors.name?.message}>
              <Input
                id="name"
                placeholder="Support agent"
                autoComplete="off"
                aria-invalid={errors.name ? true : undefined}
                {...register("name")}
              />
            </Field>
            <Field
              id="description"
              label="Description"
              hint="Optional. Plain English explaining what this role is for."
              error={errors.description?.message}
            >
              <Input
                id="description"
                placeholder="Inbound support · read-only on billing"
                aria-invalid={errors.description ? true : undefined}
                {...register("description")}
              />
            </Field>
          </FormSection>

          <FormActions>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Saving…" : "Create role"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate("/roles")}
              disabled={submitting}
            >
              Cancel
            </Button>
          </FormActions>
        </FormShell>
      </form>
    </div>
  );
}
