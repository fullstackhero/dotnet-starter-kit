import { readFileSync } from "node:fs";
import path from "node:path";
import { expect, test } from "@playwright/test";

// Several labels build their catalog key from a value the API sends: `status.${status}`. Catalog
// parity cannot see those — both locales can be missing the same key and still match — so a status
// the backend emits and the catalog never learned about renders as the key itself. That is exactly
// what shipped for top-ups: approving one moves it to `Invoiced`, and the badge read
// "status.invoiced".
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
  JSON.parse(readFileSync(path.resolve("src/locales", locale, `${ns}.json`), "utf8"));

/** The transformation the call sites apply: first letter lowered, rest kept. */
const camel = (member: string) => `status.${member.charAt(0).toLowerCase()}${member.slice(1)}`;

/** The impersonation list lowercases the whole value instead. */
const lower = (member: string) => `status.${member.toLowerCase()}`;

const CASES = [
  {
    label: "top-up requests",
    ns: "billing",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "TopupRequestStatus",
    key: camel,
  },
  {
    label: "invoices",
    ns: "billing",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "InvoiceStatus",
    key: camel,
  },
  {
    label: "impersonation grants",
    ns: "impersonation",
    file: "src/Modules/Identity/Modules.Identity.Contracts/v1/Impersonation/ImpersonationGrantDto.cs",
    enumName: "ImpersonationGrantStatus",
    key: lower,
  },
];

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

// The permission matrix is the other place keys are built at run time, and the largest: eight
// groups and thirty-plus entries, rendered on the role detail screen. The English text stays in
// permissions.ts as the fallback, so a missing key degrades rather than showing "perm.entry.x" —
// but degrading means an English row in a Portuguese table, which is what this catches.
test.describe("permission catalog keys", () => {
  test("every group and entry has a label in both locales", () => {
    const source = readFileSync(path.resolve("src/lib/permissions.ts"), "utf8");
    const groups = [...source.matchAll(/key: "([^"]+)",/g)].map((m) => m[1]);
    // The catalog holds the permission VALUE at run time ("Permissions.Users.Create"), not the
    // identifier it is written with, so resolve the constants the same way the app does.
    const values = new Map<string, string>();
    for (const constant of source.matchAll(
      /export const (\w+) = Object\.freeze\(\{([\s\S]*?)\r?\n\} as const\);/g,
    )) {
      for (const group of constant[2].matchAll(/(\w+): \{([^}]*)\}/g)) {
        for (const entry of group[2].matchAll(/(\w+): "([^"]+)"/g)) {
          values.set(`${constant[1]}.${group[1]}.${entry[1]}`, entry[2]);
        }
      }
      for (const entry of constant[2].matchAll(/^ {2}(\w+): "([^"]+)",/gm)) {
        values.set(`${constant[1]}.${entry[1]}`, entry[2]);
      }
    }
    const entries = [...source.matchAll(/name: ([A-Za-z.]+),\s*description:/g)].map((m) => {
      const value = values.get(m[1]);
      expect(value, `${m[1]} does not resolve to a permission string`).toBeDefined();
      return value!;
    });
    expect(groups.length).toBeGreaterThan(0);
    expect(entries.length).toBeGreaterThan(0);

    const en = readCatalog("en-US", "roles");
    const pt = readCatalog("pt-BR", "roles");
    const expected = [
      ...groups.flatMap((key) => [`perm.group.${key}`, `perm.blurb.${key}`]),
      ...entries.map((name) => `perm.entry.${name}`),
    ];

    expect(expected.filter((key) => !(key in en) || !(key in pt))).toEqual([]);
  });
});
