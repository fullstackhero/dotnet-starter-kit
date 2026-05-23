import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// Trash is tabbed. Default tab = Products → GET /api/v1/catalog/products/trash.
// The trash row VM only reads { id, name, sku, deletedOnUtc, deletedBy } for
// products, so we send a lean product shape (the page never touches price etc.).
function trashedProduct(over: Record<string, unknown> = {}) {
  return {
    id: "p-1",
    sku: "SKU-001",
    name: "Cordless Drill",
    slug: "cordless-drill",
    brandId: "b-1",
    categoryId: "c-1",
    price: { amount: 99, currency: "USD" },
    stock: 0,
    isActive: false,
    images: [],
    createdAtUtc: "2026-05-01T00:00:00.000Z",
    deletedOnUtc: "2026-05-20T12:00:00.000Z",
    deletedBy: "11111111-2222-3333-4444-555555555555",
    ...over,
  };
}

function trashedBrand(over: Record<string, unknown> = {}) {
  return {
    id: "b-9",
    name: "Acme Tools",
    slug: "acme-tools",
    createdAtUtc: "2026-05-01T00:00:00.000Z",
    deletedOnUtc: "2026-05-19T09:00:00.000Z",
    deletedBy: "11111111-2222-3333-4444-555555555555",
    ...over,
  };
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("system/trash", () => {
  test("renders the 'Recycle bin' heading + a trashed product row (default tab)", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/catalog/products/trash**",
      paged([trashedProduct({ name: "Cordless Drill", sku: "SKU-001" })], { totalCount: 1 }),
    );

    await page.goto("/system/trash");

    await expect(
      page.getByRole("heading", { name: "Recycle bin", level: 1 }),
    ).toBeVisible();

    // Row title renders in the hidden mobile card AND the desktop row →
    // assert on the last (visible desktop) occurrence.
    await expect(page.getByText("Cordless Drill").last()).toBeVisible();
    await expect(page.getByText(/1 products in trash/i)).toBeVisible();
    // Each row has a Restore action.
    await expect(page.getByRole("button", { name: /restore/i }).last()).toBeVisible();
  });

  test("renders the empty state for the products tab", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/catalog/products/trash**",
      paged([], { totalCount: 0 }),
    );

    await page.goto("/system/trash");

    await expect(
      page.getByRole("heading", { name: /the products trash is empty/i }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /back to products/i })).toBeVisible();
  });

  test("switching to the Brands tab loads the brands trash endpoint", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/catalog/products/trash**",
      paged([trashedProduct()], { totalCount: 1 }),
    );
    await mockJsonResponse(
      page,
      "**/api/v1/catalog/brands/trash**",
      paged([trashedBrand({ name: "Acme Tools", slug: "acme-tools" })], { totalCount: 1 }),
    );

    await page.goto("/system/trash");
    await expect(
      page.getByRole("heading", { name: "Recycle bin", level: 1 }),
    ).toBeVisible();

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/catalog/brands/trash"),
      { timeout: 5_000 },
    );
    // Tab pills are buttons; scope to the nav landmark to avoid ambiguity.
    await page
      .getByRole("navigation", { name: /trash sections/i })
      .getByRole("button", { name: "Brands" })
      .click();
    await reqPromise;

    await expect(page.getByText("Acme Tools").last()).toBeVisible();
    await expect(page.getByText(/1 brands in trash/i)).toBeVisible();
  });
});
