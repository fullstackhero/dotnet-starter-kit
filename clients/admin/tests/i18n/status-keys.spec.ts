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
