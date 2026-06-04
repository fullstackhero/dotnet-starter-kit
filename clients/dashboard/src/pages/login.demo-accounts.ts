// Mirrors src/Host/FSH.Starter.Api/DevSeeding/DevDataSeeder.cs (keep in sync).
// Static — no API call — because the login page is unauthenticated and the
// API can't safely advertise demo credentials anyway. The shape is hand-
// curated; if you add a demo account on the backend, add it here too.

export type DemoTier = "tenant-admin" | "manager" | "support" | "basic";

export type DemoAccount = {
  email: string;
  password: string;
  tenant: string;
  tenantLabel: string;
  firstName: string;
  lastName: string;
  /** Pre-baked role for the chip in the row. */
  tier: DemoTier;
  /** One-line persona explainer shown under the name. */
  persona: string;
};

export const DEMO_PASSWORD = "Password123!";

// In the Aspire dev stack the demo seeder (`seed-demo`) runs after `apply --seed`
// and realigns every tenant admin — including root's admin@root.com — to the
// shared demo password, so in dev the root account signs in with the same
// Password123! as the Acme/Globex demo users. (A production deploy that skips
// seed-demo keeps whatever Seed:DefaultAdminPassword is configured.)
export const ROOT_PASSWORD = DEMO_PASSWORD;

const acme = (
  email: string,
  firstName: string,
  lastName: string,
  tier: DemoTier,
  persona: string,
): DemoAccount => ({
  email,
  password: DEMO_PASSWORD,
  tenant: "acme",
  tenantLabel: "Acme Corp",
  firstName,
  lastName,
  tier,
  persona,
});

const globex = (
  email: string,
  firstName: string,
  lastName: string,
  tier: DemoTier,
  persona: string,
): DemoAccount => ({
  email,
  password: DEMO_PASSWORD,
  tenant: "globex",
  tenantLabel: "Globex",
  firstName,
  lastName,
  tier,
  persona,
});

/**
 * Group accounts by tenant for the panel renderer. Order matters — Root
 * sits at the top because it's the platform owner / super-tenant. Acme
 * follows since it's the populated demo where most flows make sense.
 */
export const DEMO_ACCOUNT_GROUPS: Array<{
  tenant: string;
  tenantLabel: string;
  blurb: string;
  accounts: DemoAccount[];
}> = [
  {
    tenant: "root",
    tenantLabel: "Root",
    blurb: "platform owner · super-tenant",
    accounts: [
      {
        email: "admin@root.com",
        password: ROOT_PASSWORD,
        tenant: "root",
        tenantLabel: "Root",
        firstName: "Root",
        lastName: "Admin",
        tier: "tenant-admin",
        persona: "Platform owner — manages tenants, all permissions",
      },
    ],
  },
  {
    tenant: "acme",
    tenantLabel: "Acme Corp",
    blurb: "operations · populated catalog",
    accounts: [
      acme("admin@acme.com", "Acme", "Admin", "tenant-admin", "Tenant administrator — full access"),
      acme("manager@acme.com", "Maya", "Lin", "manager", "Catalog + tickets, read-only users"),
      acme("support@acme.com", "Sam", "Rivera", "support", "Tickets only — support agent"),
      acme("alice@acme.com", "Alice", "Nguyen", "basic", "Default member"),
      acme("bob@acme.com", "Bob", "Patel", "basic", "Default member"),
    ],
  },
  {
    tenant: "globex",
    tenantLabel: "Globex",
    blurb: "onboarding · sparse data",
    accounts: [
      globex("admin@globex.com", "Globex", "Admin", "tenant-admin", "Tenant administrator — full access"),
      globex("dave@globex.com", "Dave", "Hartwell", "basic", "Default member"),
    ],
  },
];

export const TIER_LABEL: Record<DemoTier, string> = {
  "tenant-admin": "Tenant Admin",
  manager: "Manager",
  support: "Support",
  basic: "Basic",
};
