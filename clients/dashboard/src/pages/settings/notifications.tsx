import { Bell } from "lucide-react";
import { Button } from "@/components/ui/button";
import { SettingsSection } from "@/pages/settings/settings-layout";

/**
 * Notification preferences — placeholder.
 *
 * In-app notifications + SignalR bell are wired up, but per-user
 * preference persistence isn't built yet. Honest placeholder until
 * `/api/v1/notifications/preferences` (or similar) ships.
 */
export function NotificationsSettings() {
  return (
    <div className="space-y-5 fsh-enter">
      <SettingsSection
        title="Notification preferences"
        icon={Bell}
        description="Choose which events you want delivered as email or in-app notifications."
      >
        <div className="flex flex-col items-center justify-center py-10 text-center">
          <div className="mb-4 grid size-14 place-items-center rounded-2xl bg-[var(--color-muted)]">
            <Bell className="size-6 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.4)]" />
          </div>
          <h2 className="mb-1.5 font-display text-[17px] font-semibold text-[var(--color-foreground)]">
            Per-event preferences aren't tunable yet.
          </h2>
          <p className="mb-6 max-w-[380px] text-[13px] text-[var(--color-muted-foreground)]">
            In-app notifications are already delivered to your bell — open it from
            the top bar to see them. Granular per-event email opt-ins are on the
            v1.1 roadmap. In the meantime, your tenant admin can disable email
            delivery globally.
          </p>
          <Button
            variant="outline"
            onClick={() => {
              const bell = document.querySelector<HTMLElement>(
                "[data-notification-bell]",
              );
              bell?.click();
            }}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            <Bell className="mr-1.5 size-4" />
            Open notifications bell
          </Button>
        </div>
      </SettingsSection>
    </div>
  );
}
