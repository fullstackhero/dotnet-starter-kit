import * as React from "react";
import { Label } from "@/components/ui/label";

// ───────────────────────────────────────────────────────────────────────
//  SettingsField — lightweight label wrapper used inside SettingsSection
//  body grids. Renders an uppercase tracked label above its child control.
//  Use the existing `Field` component when you need aria-invalid wiring,
//  error messages, or hint text; use SettingsField when the section
//  already supplies context and you just need the label rhythm.
//
//  Usage:
//    <SettingsField id="first-name" label="First name">
//      <Input id="first-name" … />
//    </SettingsField>
// ───────────────────────────────────────────────────────────────────────

export function SettingsField({
  id,
  label,
  children,
}: {
  id: string;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <Label
        htmlFor={id}
        className="mb-1.5 block text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
      >
        {label}
      </Label>
      {children}
    </div>
  );
}
