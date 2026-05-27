import { cloneElement, isValidElement, type ReactElement, type ReactNode } from "react";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/cn";

type FieldProps = {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string;
  required?: boolean;
  className?: string;
  children: ReactNode;
};

/**
 * Field — form-field shell used across editor forms. Mono-caps label
 * with optional required-dot, hint line, and destructive error line.
 * Always pass the control as children so the field can wrap any input
 * primitive (Input, Combobox, Textarea, etc.).
 */
export function Field({ id, label, hint, error, required, className, children }: FieldProps) {
  // Tie the control to its hint/error so AT announces them, and reflect the
  // error as aria-invalid — without forcing every caller to wire it by hand.
  // A caller-supplied value always wins (?? merge). Only a single element
  // child is augmented; anything else renders untouched.
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined;
  const control = isValidElement(children)
    ? cloneElement(children as ReactElement<Record<string, unknown>>, {
        "aria-describedby":
          (children.props as Record<string, unknown>)["aria-describedby"] ?? describedBy,
        "aria-invalid":
          (children.props as Record<string, unknown>)["aria-invalid"] ?? (error ? true : undefined),
      })
    : children;

  return (
    <div className={cn("space-y-1.5", className)}>
      <Label htmlFor={id} className="flex items-center gap-1.5">
        <span>{label}</span>
        {required && (
          <span className="text-[var(--color-destructive)]" aria-hidden>
            ·
          </span>
        )}
      </Label>
      {control}
      {error ? (
        <p
          id={`${id}-error`}
          className="text-[11.5px] leading-relaxed text-[var(--color-destructive)]"
          role="alert"
        >
          {error}
        </p>
      ) : (
        hint && (
          <p
            id={`${id}-hint`}
            className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]/85"
          >
            {hint}
          </p>
        )
      )}
    </div>
  );
}
