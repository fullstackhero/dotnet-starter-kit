import { Bell } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { EmptyState } from "@/components/list";

/**
 * Notification preferences — placeholder.
 *
 * The notifications module exists (in-app notifications + SignalR bell)
 * but there's no per-user preference endpoint yet — no way to persist
 * "email me on X but not Y". Rather than ship a panel of toggles that
 * don't persist, we render an honest placeholder. Wire up the toggles
 * once `/api/v1/notifications/preferences` (or similar) ships.
 */
export function NotificationsSettings() {
  return (
    <div className="space-y-6 fsh-enter">
      <Card>
        <CardHeader>
          <CardTitle>Notification preferences</CardTitle>
          <CardDescription>
            Choose which events you want delivered as email or in-app notifications.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <EmptyState
            eyebrow="Coming soon"
            headline="Per-event preferences aren't tunable yet."
            body={
              <>
                In-app notifications are already delivered to your bell — open it from
                the top bar to see them. Granular per-event email opt-ins are on the
                v1.1 roadmap. In the meantime, your tenant admin can disable email
                delivery globally.
              </>
            }
            icon={<Bell className="h-6 w-6 text-[var(--color-primary)]" />}
            primaryAction={{
              label: "Open notifications bell",
              onClick: () => {
                const bell = document.querySelector<HTMLElement>(
                  "[data-notification-bell]",
                );
                bell?.click();
              },
            }}
          />
        </CardContent>
      </Card>
    </div>
  );
}
