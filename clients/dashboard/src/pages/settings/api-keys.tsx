import { KeyRound, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { EmptyState } from "@/components/list";

export function ApiKeysSettings() {
  return (
    <div className="space-y-6 fsh-enter">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-4">
            <div>
              <CardTitle>API keys</CardTitle>
              <CardDescription>
                Long-lived credentials used by your services to call the FSH API.
                Treat them like passwords.
              </CardDescription>
            </div>
            <Button size="sm">
              <Plus className="mr-1.5 h-3.5 w-3.5" />
              Generate key
            </Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <EmptyState
            eyebrow="No keys yet"
            headline="Your tenant hasn't issued any API keys."
            body="Generate a key, store it somewhere safe, and use it as a Bearer token against the API. Keys are only shown once at creation — copy them at that moment."
            icon={<KeyRound className="h-6 w-6 text-[var(--color-primary)]" />}
            primaryAction={{
              label: "Generate first key",
              onClick: () => {},
              icon: <Plus className="h-3.5 w-3.5" />,
            }}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Usage best practices</CardTitle>
          <CardDescription>
            A short checklist for keeping API keys safe.
          </CardDescription>
        </CardHeader>
        <CardContent className="px-6 pb-5 pt-1">
          <ul className="space-y-2 text-sm">
            <Tip>
              Rotate keys at least every 90 days, or immediately when a teammate
              leaves the project.
            </Tip>
            <Tip>
              Scope keys narrowly — generate a separate key per service rather
              than reusing one across many.
            </Tip>
            <Tip>
              Never commit keys to source control. Use your platform's secret
              store (Azure Key Vault, AWS Secrets Manager, Doppler).
            </Tip>
            <Tip>
              Revoke immediately if a key is exposed; the audit log records the
              last-used timestamp for every key.
            </Tip>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}

function Tip({ children }: { children: React.ReactNode }) {
  return (
    <li className="flex items-start gap-2.5">
      <span
        aria-hidden
        className="mt-0.5 grid h-4 w-4 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
      >
        <svg viewBox="0 0 12 12" className="h-2.5 w-2.5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M2.5 6.5l2.5 2.5L9.5 4" />
        </svg>
      </span>
      <span className="leading-relaxed">{children}</span>
    </li>
  );
}
