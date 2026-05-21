import { Rows3, Rows4 } from "lucide-react";
import { cn } from "@/lib/cn";

export type Density = "cozy" | "compact";

/**
 * Segmented density toggle. Consumers persist the value to localStorage
 * themselves (key per page) and pass it back via `density` / `onChange`.
 */
export function DensityToggle({
  density,
  onChange,
}: {
  density: Density;
  onChange: (d: Density) => void;
}) {
  return (
    <div
      role="group"
      aria-label="Row density"
      className="inline-flex h-9 items-center rounded-full border border-[var(--color-border)] bg-[var(--color-card)] p-0.5"
    >
      <DensityButton
        active={density === "cozy"}
        onClick={() => onChange("cozy")}
        label="Cozy rows"
      >
        <Rows3 className="h-4 w-4" />
      </DensityButton>
      <DensityButton
        active={density === "compact"}
        onClick={() => onChange("compact")}
        label="Compact rows"
      >
        <Rows4 className="h-4 w-4" />
      </DensityButton>
    </div>
  );
}

function DensityButton({
  active,
  onClick,
  label,
  children,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      aria-label={label}
      title={label}
      onClick={onClick}
      className={cn(
        "grid h-8 w-9 cursor-pointer place-items-center rounded-full transition-colors duration-[var(--duration-fast)]",
        active
          ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
      )}
    >
      {children}
    </button>
  );
}

/**
 * Hook that wires a `localStorage`-backed density toggle to a key.
 * Page passes the key, gets back `[density, setDensity]`.
 */
import { useEffect, useState } from "react";

export function usePersistedDensity(storageKey: string): [Density, (d: Density) => void] {
  const [density, setDensity] = useState<Density>(() => {
    if (typeof window === "undefined") return "cozy";
    return (localStorage.getItem(storageKey) as Density | null) ?? "cozy";
  });
  useEffect(() => {
    try {
      localStorage.setItem(storageKey, density);
    } catch {
      /* storage unavailable */
    }
  }, [storageKey, density]);
  return [density, setDensity];
}
