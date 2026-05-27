import { ExternalLink, KeyRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { SettingsSection } from "@/pages/settings/settings-layout";

/**
 * API keys settings — placeholder.
 *
 * The backend feature isn't built yet. We keep the route alive so
 * existing nav-links don't 404, but render an honest "coming soon"
 * state until `/api/v1/identity/api-keys` ships.
 */
export function ApiKeysSettings() {
  return (
    <div className="space-y-5 fsh-enter">
      <SettingsSection
        title="API keys"
        icon={KeyRound}
        description="Long-lived credentials used by services to call the FSH API. Treat them like passwords."
      >
        <div className="flex flex-col items-center justify-center py-10 text-center">
          <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
            <KeyRound className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)]" />
          </div>
          <h3 className="mb-1.5 font-display text-[17px] font-semibold text-[var(--color-foreground)]">
            API keys aren't available yet.
          </h3>
          <p className="mb-6 max-w-[380px] text-[13px] text-[var(--color-muted-foreground)]">
            Personal access tokens and service-to-service API keys are on the
            v1.1 roadmap. For now, your access flows through the user-bound JWT
            issued at sign-in. Track progress on the public roadmap or watch the
            next release notes.
          </p>
          <Button
            variant="outline"
            onClick={() => {
              window.open(
                "https://github.com/fullstackhero/dotnet-starter-kit",
                "_blank",
                "noopener,noreferrer",
              );
            }}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            <ExternalLink className="mr-1.5 size-4" />
            View roadmap
          </Button>
        </div>
      </SettingsSection>
    </div>
  );
}
