import { LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/use-auth";
import { SseStatusBadge } from "@/components/sse/sse-status-badge";

export function Topbar() {
  const { user, logout } = useAuth();
  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-4 border-b border-[var(--color-border)] bg-[var(--color-background)] px-6">
      <div className="flex items-center gap-3">
        <SseStatusBadge />
        <span className="text-sm text-[var(--color-muted-foreground)]">
          Tenant: <span className="font-medium text-[var(--color-foreground)]">{user?.tenant ?? "—"}</span>
        </span>
      </div>
      <div className="flex items-center gap-3">
        <div className="text-right">
          <div className="text-sm font-medium">{user?.name ?? user?.email ?? "Unknown"}</div>
          {user?.email && user.name && (
            <div className="text-xs text-[var(--color-muted-foreground)]">{user.email}</div>
          )}
        </div>
        <Button variant="outline" size="sm" onClick={logout}>
          <LogOut className="mr-2 h-4 w-4" />
          Sign out
        </Button>
      </div>
    </header>
  );
}
