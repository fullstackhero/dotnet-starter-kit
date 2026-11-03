import { expect, test } from "@playwright/test";
import { installShellMocks } from "../helpers/shell-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

// Regression: the JWT payload is UTF-8. Decoding it with a bare atob() read the
// bytes as Latin-1, so a claim like "Comitê de Ética" rendered as
// "ComitÃª de Ã…tica" in the user menu and the overview greeting.

test("renders non-ASCII name claims from the access token intact", async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, firstName: "Comitê", lastName: "de Ética" });
  await installShellMocks(page);

  await page.goto("/");

  await expect(page.getByText("Comitê de Ética").first()).toBeVisible();
  await expect(page.getByText(/Ã/)).toHaveCount(0);
});
