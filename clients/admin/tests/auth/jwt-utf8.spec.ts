import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS } from "../helpers/shell-mocks";

// Regression: the JWT payload is UTF-8. Decoding it with a bare atob() read the
// bytes as Latin-1, so a claim like "Comitê de Ética" rendered as "ComitÃª".

test("renders non-ASCII name claims from the access token intact", async ({ page }) => {
  await seedAuthedSession(page, {
    ...TEST_USER,
    firstName: "Comitê",
    lastName: "de Ética",
    permissions: [...ADMIN_PERMS],
  });
  await installAdminShellMocks(page);

  await page.goto("/");

  await expect(page.getByText(/Comitê/).first()).toBeVisible();
  await expect(page.getByText(/Ã/)).toHaveCount(0);
});
