import { readFileSync } from "node:fs";
import path from "node:path";
import { expect, test } from "@playwright/test";
import { INVOICE_STATUS_KEYS, statusKey, TOPUP_STATUS_KEYS } from "../../src/lib/status-keys";

// Several labels build their catalog key from a value the API sends (`invoices.status.${status}`,
// the ticket status/priority maps in src/lib/ticket-enums.ts). Catalog parity cannot see those —
// both locales can be missing the same key and still match — so a value the backend emits and the
// catalog never learned about renders as the key itself.
//
// The member list is read from the backend enum rather than copied here, so adding a member to the
// C# enum without translating it fails this test instead of reaching a screen.

const repoRoot = path.resolve("../..");

function enumMembers(relativePath: string, enumName: string): string[] {
  const source = readFileSync(path.join(repoRoot, relativePath), "utf8");
  const declaration = new RegExp(`enum\\s+${enumName}\\s*\\{([^}]*)\\}`, "s").exec(source);
  expect(declaration, `${enumName} not found in ${relativePath}`).not.toBeNull();
  const members = [...declaration![1].matchAll(/^\s*([A-Z]\w*)\s*(?:=\s*-?\d+\s*)?,?\s*$/gm)].map(
    (m) => m[1],
  );
  expect(members.length, `${enumName} parsed as empty`).toBeGreaterThan(0);
  return members;
}

const readCatalog = (locale: string, ns: string): Record<string, string> =>
  JSON.parse(
    readFileSync(path.resolve("src/locales", locale, `${ns}.json`), "utf8"),
  ) as Record<string, string>;

/** Looks a member up in the app's map (src/lib/status-keys.ts); a member it lacks has no key. */
const mapped = (keys: Record<string, string>) => (member: string) =>
  Object.hasOwn(keys, member) ? keys[member] : `(no entry for ${member} in src/lib/status-keys.ts)`;

const CASES = [
  {
    label: "invoices",
    ns: "subscription",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "InvoiceStatus",
    key: mapped(INVOICE_STATUS_KEYS),
  },
  {
    label: "top-up requests",
    ns: "subscription",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "TopupRequestStatus",
    key: mapped(TOPUP_STATUS_KEYS),
  },
  {
    label: "ticket status",
    ns: "tickets",
    file: "src/Modules/Tickets/Modules.Tickets.Contracts/Dtos/TicketStatus.cs",
    enumName: "TicketStatus",
    key: (member: string) => `status.${member}`,
  },
  {
    label: "ticket priority",
    ns: "tickets",
    file: "src/Modules/Tickets/Modules.Tickets.Contracts/Dtos/TicketPriority.cs",
    enumName: "TicketPriority",
    key: (member: string) => `priority.${member}`,
  },
];

test("a status the server added after this build has no key, so the badge shows the raw value", () => {
  expect(statusKey(TOPUP_STATUS_KEYS, "Expired")).toBeUndefined();
  expect(statusKey(INVOICE_STATUS_KEYS, "constructor")).toBeUndefined();
  expect(statusKey(INVOICE_STATUS_KEYS, "Paid")).toBe("invoices.status.Paid");
});

test.describe("status keys built from backend enums", () => {
  for (const testCase of CASES) {
    test(`${testCase.label}: every member has a label in both locales`, () => {
      const members = enumMembers(testCase.file, testCase.enumName);
      const en = readCatalog("en-US", testCase.ns);
      const pt = readCatalog("pt-BR", testCase.ns);

      const missing = members
        .map((member) => testCase.key(member))
        .filter((key) => !(key in en) || !(key in pt));

      expect(missing).toEqual([]);
    });
  }
});
