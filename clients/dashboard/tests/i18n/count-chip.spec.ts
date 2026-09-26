import { readFileSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { DEFAULT_PROFILE, installShellMocks, paged } from "../helpers/shell-mocks";

// The count chip in EntityPageHeader translates the `unit` prop itself
// (`t("unit." + unit, { count })`). A caller that passes an already translated
// word builds a key no catalog has, and the chip renders the raw key. en-US
// hides it because the English translation equals the token; pt-BR does not.

const TICKET = {
  id: "00000000-0000-0000-0000-0000000000a1",
  number: "TK-1",
  title: "Login button broken on mobile",
  description: null,
  status: "Open",
  priority: "High",
  reporterUserId: "00000000-0000-0000-0000-000000000111",
  assignedToUserId: null,
  resolutionNote: null,
  createdAtUtc: "2026-05-10T10:00:00Z",
  updatedAtUtc: null,
  resolvedAtUtc: null,
  closedAtUtc: null,
  commentCount: 0,
  deletedOnUtc: null,
  deletedBy: null,
};

test.describe("count chip — pt-BR", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    // Keep the topbar's profile-locale sync from switching back to the default.
    await mockJsonResponse(page, "**/api/v1/identity/profile", {
      ...DEFAULT_PROFILE,
      locale: "pt-BR",
    });
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));
  });

  test("renders the plural form, not the raw key", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tickets**", paged([TICKET], { totalCount: 3 }));

    await page.goto("/tickets?culture=pt-BR");

    await expect(page.getByText("3 chamados", { exact: true })).toBeVisible();
    // The failure mode is a literal `unit.<translated word>` on screen.
    await expect(page.getByText(/^unit\./)).toHaveCount(0);
  });

  test("renders the singular form for a count of one", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tickets**", paged([TICKET], { totalCount: 1 }));

    await page.goto("/tickets?culture=pt-BR");

    await expect(page.getByText("1 chamado", { exact: true })).toBeVisible();
  });
});

// The union type on `unit` stops a translated word from reaching the chip, but
// nothing stops a new member being added to it without the matching plural
// keys. This closes that half.
test("every unit token in use has both plural forms in both catalogs", () => {
  const srcDir = fileURLToPath(new URL("../../src", import.meta.url));

  const walk = (dir: string): string[] =>
    readdirSync(dir, { withFileTypes: true }).flatMap((e) =>
      e.isDirectory() ? walk(`${dir}/${e.name}`) : [`${dir}/${e.name}`],
    );

  const tokens = new Set<string>(["item"]); // the prop's own default
  for (const file of walk(srcDir).filter((f) => f.endsWith(".tsx"))) {
    for (const m of readFileSync(file, "utf8").matchAll(/\bunit="([a-zA-Z]+)"/g)) {
      tokens.add(m[1]);
    }
  }
  expect(tokens.size).toBeGreaterThan(1);

  for (const locale of ["en-US", "pt-BR"]) {
    const common: Record<string, string> = JSON.parse(
      readFileSync(
        fileURLToPath(new URL(`../../src/locales/${locale}/common.json`, import.meta.url)),
        "utf8",
      ),
    );
    const missing = [...tokens]
      .flatMap((t) => [`unit.${t}_one`, `unit.${t}_other`])
      .filter((k) => !(k in common));

    expect(missing, `${locale}/common.json`).toEqual([]);
  }
});
