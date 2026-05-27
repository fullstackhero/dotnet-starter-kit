import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ArrowLeft, KeyRound, User as UserIcon, Users } from "lucide-react";
import { toast } from "sonner";
import { registerUser } from "@/api/users";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  EntityPageHeader,
  Field,
  SettingsSection,
} from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";

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

export function CreateUserPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
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
    mutationFn: registerUser,
    onSuccess: (result) => {
      toast.success("User created", {
        description: result.message ?? "Confirmation email queued.",
      });
      queryClient.invalidateQueries({ queryKey: ["users"] });
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

  const onSubmit = handleSubmit((values) =>
    mutation.mutate({
      firstName: values.firstName,
      lastName: values.lastName,
      userName: values.userName,
      email: values.email,
      password: values.password,
      confirmPassword: values.confirmPassword,
      phoneNumber: values.phoneNumber?.trim() || undefined,
    }),
  );

  const submitting = isSubmitting || mutation.isPending;

  return (
    <div className="space-y-6">
      <EntityPageHeader
        icon={Users}
        title="New account"
        description="The new user is created in the current tenant and emailed a confirmation link. Roles can be assigned from the detail page after creation."
      >
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate("/users")}
          className="h-9 gap-1.5 rounded-lg px-3 text-[13px]"
        >
          <ArrowLeft className="size-3.5" /> Directory
        </Button>
      </EntityPageHeader>

      <form onSubmit={onSubmit}>
        <div className="max-w-2xl space-y-4">
          <SettingsSection
            title="Identity"
            icon={UserIcon}
            description="Personal details and the username they'll use to sign in."
          >
            <div className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <Field id="firstName" label="First name" required error={errors.firstName?.message}>
                  <Input
                    id="firstName"
                    autoComplete="given-name"
                    aria-invalid={errors.firstName ? true : undefined}
                    {...register("firstName")}
                  />
                </Field>
                <Field id="lastName" label="Last name" required error={errors.lastName?.message}>
                  <Input
                    id="lastName"
                    autoComplete="family-name"
                    aria-invalid={errors.lastName ? true : undefined}
                    {...register("lastName")}
                  />
                </Field>
              </div>

              <Field
                id="userName"
                label="Username"
                required
                hint="Letters, digits, dot, dash or underscore. 3–32 characters."
                error={errors.userName?.message}
              >
                <Input
                  id="userName"
                  placeholder="m.chen"
                  autoComplete="off"
                  className="font-mono"
                  aria-invalid={errors.userName ? true : undefined}
                  {...register("userName")}
                />
              </Field>

              <Field id="email" label="Email" required error={errors.email?.message}>
                <Input
                  id="email"
                  type="email"
                  placeholder="user@example.com"
                  autoComplete="email"
                  className="font-mono"
                  aria-invalid={errors.email ? true : undefined}
                  {...register("email")}
                />
              </Field>

              <Field id="phoneNumber" label="Phone (optional)" error={errors.phoneNumber?.message}>
                <Input
                  id="phoneNumber"
                  type="tel"
                  placeholder="+1 555 0100"
                  autoComplete="tel"
                  className="font-mono"
                  {...register("phoneNumber")}
                />
              </Field>
            </div>
          </SettingsSection>

          <SettingsSection
            title="Credentials"
            icon={KeyRound}
            description="Initial password. The user is encouraged to change it on first sign-in."
            footer={
              <div className="flex items-center gap-2">
                <Button
                  type="submit"
                  disabled={submitting}
                  className="h-9 rounded-lg px-4 text-[13px]"
                >
                  {submitting ? "Creating…" : "Create account"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate("/users")}
                  disabled={submitting}
                  className="h-9 rounded-lg px-4 text-[13px]"
                >
                  Cancel
                </Button>
              </div>
            }
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <Field id="password" label="Password" required error={errors.password?.message}>
                <Input
                  id="password"
                  type="password"
                  autoComplete="new-password"
                  className="font-mono"
                  aria-invalid={errors.password ? true : undefined}
                  {...register("password")}
                />
              </Field>
              <Field
                id="confirmPassword"
                label="Confirm password"
                required
                error={errors.confirmPassword?.message}
              >
                <Input
                  id="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  className="font-mono"
                  aria-invalid={errors.confirmPassword ? true : undefined}
                  {...register("confirmPassword")}
                />
              </Field>
            </div>
          </SettingsSection>
        </div>
      </form>
    </div>
  );
}
