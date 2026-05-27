// Admin demo accounts — mirrors src/Host/FSH.Starter.Api/DevSeeding (keep in sync).
// Static — no API call — because the login page is unauthenticated and the
// API can't safely advertise credentials. Admin only surfaces the root /
// operator superadmin; tenant-level demo users live in the dashboard app.

export type DemoAccount = {
  email: string;
  password: string;
  tenant: string;
  /** Short display label */
  label: string;
  /** Initials rendered in the avatar */
  initials: string;
  /** One-line persona explainer */
  persona: string;
};

export const DEMO_PASSWORD = "Password123!";

/**
 * Admin exposes a single demo account: the root superadmin.
 * In the Aspire dev stack the demo seeder (`seed-demo`) aligns this
 * account's password to the shared DEMO_PASSWORD so it works out of
 * the box in local dev.
 */
export const ADMIN_DEMO_ACCOUNTS: DemoAccount[] = [
  {
    email: "superadmin@root.com",
    password: DEMO_PASSWORD,
    tenant: "root",
    label: "SuperAdmin",
    initials: "SA",
    persona: "Platform operator · cross-tenant control",
  },
];
