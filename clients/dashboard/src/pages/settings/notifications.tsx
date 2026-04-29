import { useState } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";

type Pref = {
  id: string;
  title: string;
  description: string;
  defaultOn: boolean;
};

const preferences: Pref[] = [
  {
    id: "login-from-new-device",
    title: "Sign-in from a new device",
    description: "Email me when a new browser or device signs in to my account.",
    defaultOn: true,
  },
  {
    id: "quota-threshold",
    title: "Quota threshold reached",
    description: "Alert me when any tracked resource crosses 80% of its plan limit.",
    defaultOn: true,
  },
  {
    id: "subscription-changes",
    title: "Subscription changes",
    description: "Notify me when my plan is upgraded, downgraded, or renewed.",
    defaultOn: true,
  },
  {
    id: "invoice-issued",
    title: "Invoice issued",
    description: "Send a copy of every invoice when it's generated.",
    defaultOn: false,
  },
  {
    id: "webhook-delivery-failures",
    title: "Webhook delivery failures",
    description: "Notify me when an outbound webhook fails after the final retry.",
    defaultOn: false,
  },
];

export function NotificationsSettings() {
  const [state, setState] = useState<Record<string, boolean>>(() =>
    Object.fromEntries(preferences.map((p) => [p.id, p.defaultOn])),
  );

  return (
    <div className="space-y-6 fsh-enter">
      <Card>
        <CardHeader>
          <CardTitle>Email notifications</CardTitle>
          <CardDescription>
            Pick which events should generate an email to{" "}
            <span className="font-mono text-[var(--color-foreground)]">
              your account address
            </span>
            .
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <ul>
            {preferences.map((p, idx) => (
              <li
                key={p.id}
                className="flex items-start justify-between gap-6 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
                style={{ animationDelay: `${50 * idx}ms` }}
              >
                <div className="min-w-0">
                  <div className="text-sm font-medium tracking-tight">{p.title}</div>
                  <p className="mt-0.5 text-xs leading-relaxed text-[var(--color-muted-foreground)]">
                    {p.description}
                  </p>
                </div>
                <Switch
                  checked={state[p.id] ?? false}
                  onCheckedChange={(checked) =>
                    setState((prev) => ({ ...prev, [p.id]: checked }))
                  }
                  aria-label={p.title}
                />
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
