import type { Page, Route } from "@playwright/test";
import { mockJsonResponse } from "./api-mocks";

/**
 * A profile body that satisfies the topbar avatar + settings/profile read.
 * Matches the runtime UserDto shape the dashboard expects.
 */
export const DEFAULT_PROFILE = {
  id: "u-test-1",
  userName: "alice",
  email: "alice@acme.com",
  firstName: "Alice",
  lastName: "Nguyen",
  phoneNumber: "",
  isActive: true,
  emailConfirmed: true,
  twoFactorEnabled: false,
  imageUrl: null,
} as const;

/**
 * Mock every API call the authenticated AppShell fires on load so any
 * protected page can be visited in isolation without hanging on the
 * topbar's notification/chat badges or the SSE/realtime providers.
 *
 * ORDERING: Playwright matches the MOST RECENTLY registered route first.
 * We register broad globs first and the more-specific ones last. Callers
 * register their page-specific mocks AFTER calling this, so those win over
 * these defaults (e.g. a chat spec can return real channels).
 */
export async function installShellMocks(page: Page): Promise<void> {
  // Long-lived realtime transports — abort so they neither keep the network
  // busy nor spew reconnect noise. The shell simply shows an "offline" dot.
  await page.route("**/api/v1/sse/**", (r: Route) => r.abort());
  await page.route("**/negotiate**", (r: Route) => r.abort());
  await page.route("**/api/v1/realtime/**", (r: Route) => r.abort());

  // Notifications — register the list glob first, then the more specific
  // unread-count (so the count request resolves to a number, not []).
  await mockJsonResponse(page, "**/api/v1/notifications**", []);
  await mockJsonResponse(page, "**/api/v1/notifications/unread-count**", 0);

  // Topbar chat unread badge.
  await mockJsonResponse(page, "**/api/v1/chat/channels**", []);

  // Defensive: profile + permissions (harmless if a page re-reads them).
  await mockJsonResponse(page, "**/api/v1/identity/profile", DEFAULT_PROFILE);
  await mockJsonResponse(page, "**/api/v1/identity/permissions", []);

  // Tenant status drives the global expiry/grace banner mounted in the
  // AppShell. Default to a healthy, far-future tenant so the banner stays
  // hidden; specs that exercise the banner override this after the call.
  await mockJsonResponse(page, "**/api/v1/tenants/me/status**", {
    id: "acme",
    name: "Acme Corp",
    isActive: true,
    validUpto: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString(),
    hasConnectionString: false,
    adminEmail: "admin@acme.com",
    issuer: null,
    plan: "Scale",
    expiryState: "Active",
    graceEndsUtc: new Date(Date.now() + 372 * 24 * 60 * 60 * 1000).toISOString(),
  });
}

/** Build a Playwright-shaped paged response body. */
export function paged<T>(items: T[], overrides: Partial<{ pageNumber: number; pageSize: number; totalCount: number; totalPages: number }> = {}) {
  const pageSize = overrides.pageSize ?? 20;
  const totalCount = overrides.totalCount ?? items.length;
  return {
    items,
    pageNumber: overrides.pageNumber ?? 1,
    pageSize,
    totalCount,
    totalPages: overrides.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize)),
    hasPrevious: (overrides.pageNumber ?? 1) > 1,
    hasNext: (overrides.pageNumber ?? 1) < (overrides.totalPages ?? Math.max(1, Math.ceil(totalCount / pageSize))),
  };
}
