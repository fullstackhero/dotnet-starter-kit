import { KeyRound } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { EmptyState } from "@/components/list";

/**
 * API keys settings — placeholder.
 *
 * The backend feature isn't built yet (no `/api/v1/identity/api-keys`
 * endpoints exist). We keep the route alive so existing nav-links don't
 * 404, but render an honest "coming soon" state instead of mock toggles.
 * Wire this up once the server side ships.
 */
export function ApiKeysSettings() {
  return (
    <div className="space-y-6 fsh-enter">
      <Card>
        <CardHeader>
          <CardTitle>API keys</CardTitle>
          <CardDescription>
            Long-lived credentials used by your services to call the FSH API.
            Treat them like passwords.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <EmptyState
            eyebrow="Coming soon"
            headline="API keys aren't available yet."
            body={
              <>
                Personal access tokens and service-to-service API keys are on the v1.1
                roadmap. For now, your access flows through the user-bound JWT issued
                at sign-in. Track progress on the public roadmap or watch the next
                release notes.
              </>
            }
            icon={<KeyRound className="h-6 w-6 text-[var(--color-primary)]" />}
            primaryAction={{
              label: "View roadmap",
              onClick: () => {
                window.open(
                  "https://github.com/fullstackhero/dotnet-starter-kit",
                  "_blank",
                  "noopener,noreferrer",
                );
              },
            }}
          />
        </CardContent>
      </Card>
    </div>
  );
}
