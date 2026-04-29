import { useState } from "react";
import { LogOut, MonitorSmartphone, ShieldCheck, ShieldOff } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";

type Session = {
  id: string;
  device: string;
  location: string;
  startedAt: string;
  current: boolean;
};

const mockSessions: Session[] = [
  {
    id: "current",
    device: "Chrome 130 · Windows",
    location: "Bangalore, IN",
    startedAt: new Date().toISOString(),
    current: true,
  },
  {
    id: "old-1",
    device: "Safari 17 · macOS",
    location: "Bangalore, IN",
    startedAt: new Date(Date.now() - 1000 * 60 * 60 * 18).toISOString(),
    current: false,
  },
];

export function SecuritySettings() {
  const [twoFactor, setTwoFactor] = useState(false);

  return (
    <div className="space-y-6 fsh-enter">
      {/* Password */}
      <Card>
        <CardHeader>
          <CardTitle>Password</CardTitle>
          <CardDescription>
            Used to sign in to this tenant. Last changed —.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="text-sm text-[var(--color-muted-foreground)]">
            Choose a strong, unique password. We recommend a passphrase of 16+ characters.
          </div>
          <Button variant="outline" size="sm">Change password</Button>
        </CardContent>
      </Card>

      {/* Two-factor */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            Two-factor authentication
            {twoFactor ? (
              <Badge variant="success">enabled</Badge>
            ) : (
              <Badge variant="warning">disabled</Badge>
            )}
          </CardTitle>
          <CardDescription>
            Require a one-time code from an authenticator app on every sign-in.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-4 px-6 pb-5 pt-1">
          <div className="flex items-center gap-3">
            <span
              aria-hidden
              className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              {twoFactor ? <ShieldCheck className="h-4 w-4" /> : <ShieldOff className="h-4 w-4" />}
            </span>
            <div className="text-sm">
              {twoFactor ? "Authenticator-app codes required" : "Codes are not currently required"}
            </div>
          </div>
          <Switch
            checked={twoFactor}
            onCheckedChange={setTwoFactor}
            aria-label="Two-factor authentication"
          />
        </CardContent>
      </Card>

      {/* Active sessions */}
      <Card>
        <CardHeader>
          <CardTitle>Active sessions</CardTitle>
          <CardDescription>
            Browsers and devices currently signed in to your account.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <ul>
            {mockSessions.map((s) => (
              <li
                key={s.id}
                className="flex items-center justify-between gap-4 border-t border-[var(--color-border)] px-6 py-4 first:border-t-0"
              >
                <div className="flex items-center gap-3">
                  <span
                    aria-hidden
                    className="grid h-9 w-9 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
                  >
                    <MonitorSmartphone className="h-4 w-4" />
                  </span>
                  <div className="space-y-0.5">
                    <div className="flex items-center gap-2 text-sm font-medium tracking-tight">
                      {s.device}
                      {s.current && <Badge variant="brand">this device</Badge>}
                    </div>
                    <div className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                      {s.location} · started {new Date(s.startedAt).toLocaleString()}
                    </div>
                  </div>
                </div>
                {!s.current && (
                  <Button variant="ghost" size="sm">
                    <LogOut className="mr-1.5 h-3.5 w-3.5" />
                    Revoke
                  </Button>
                )}
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
