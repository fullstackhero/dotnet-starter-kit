import { Label } from "@/components/ui/label";

/**
 * Form field wrapper used across editor dialogs. Mono-caps tracked
 * label, optional `*` hint that's a primary-toned middle dot, and a
 * faint hint line below the control. The required indicator is
 * announced to screen readers via an sr-only "required" sibling so
 * the visual `·` glyph isn't relied on for semantics.
 */
export function Field({
  id,
  label,
  hint,
  required,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label
        htmlFor={id}
        className="flex items-center gap-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
      >
        {label}
        {required && (
          <>
            <span aria-hidden className="text-[var(--color-destructive)]">·</span>
            <span className="sr-only">required</span>
          </>
        )}
      </Label>
      {children}
      {hint && (
        <p className="text-[11.5px] leading-relaxed text-[var(--color-muted-foreground)]/85">
          {hint}
        </p>
      )}
    </div>
  );
}
