// E2E coverage for the catalog pages: brands, categories, products (list),
// and product detail. All catalog API calls are mocked via page.route; the
// authed session is seeded into localStorage and the global shell calls are
// stubbed by installShellMocks. Browser: chromium only, run against the
// already-running Vite dev server.
//
// Gotcha note: the list pages render BOTH a hidden mobile card and a desktop
// row, so the same text matches twice. Row assertions therefore scope to a
// landmark/dialog or use .last() / { exact: true } to avoid strict-mode hits.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// ─── Fixtures ────────────────────────────────────────────────────────────

const BRAND_GLOBEX = {
  id: "00000000-0000-0000-0000-0000000b1111",
  name: "Globex Gadgets",
  slug: "globex-gadgets",
  description: "Maker of fine widgets.",
  logoUrl: null,
  createdAtUtc: "2026-05-01T10:00:00Z",
  updatedAtUtc: null,
  deletedOnUtc: null,
  deletedBy: null,
};

const CATEGORY_OUTDOOR = {
  id: "00000000-0000-0000-0000-0000000c2222",
  name: "Outdoor Gear",
  slug: "outdoor-gear",
  description: "Gear for the great outdoors.",
  parentCategoryId: null,
  createdAtUtc: "2026-05-02T10:00:00Z",
  updatedAtUtc: null,
  deletedOnUtc: null,
  deletedBy: null,
};

const PRODUCT_TENT = {
  id: "00000000-0000-0000-0000-0000000d3333",
  sku: "GLX-TENT-001",
  name: "Trailhead Tent",
  slug: "trailhead-tent",
  description: "A two-person backpacking tent.",
  brandId: BRAND_GLOBEX.id,
  categoryId: CATEGORY_OUTDOOR.id,
  price: { amount: 199.99, currency: "USD" },
  stock: 42,
  isActive: true,
  thumbnailUrl: null,
  images: [],
  createdAtUtc: "2026-05-03T10:00:00Z",
  updatedAtUtc: null,
  deletedOnUtc: null,
  deletedBy: null,
};

// ─── Shared beforeEach ──────────────────────────────────────────────────

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

// ─── Brands ──────────────────────────────────────────────────────────────

test.describe("catalog/brands", () => {
  test("renders the heading and a row built from mocked data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/catalog/brands**", paged([BRAND_GLOBEX]));

    await page.goto("/catalog/brands");

    await expect(page.getByRole("heading", { name: /brands/i })).toBeVisible();
    // Both the mobile card and desktop row print the name — scope to the
    // desktop list card so we assert on a single, deterministic match.
    await expect(page.getByText("Globex Gadgets").last()).toBeVisible();
    await expect(page.getByText("1 brand found")).toBeVisible();
  });

  test("shows the empty state when no brands match", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/catalog/brands**", paged([]));

    await page.goto("/catalog/brands");

    await expect(page.getByRole("heading", { name: /no brands yet/i })).toBeVisible();
    await expect(
      page.getByText(/add your first brand to start building the catalog/i),
    ).toBeVisible();
  });

  test("opens the New brand create dialog with its form fields", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/catalog/brands**", paged([BRAND_GLOBEX]));

    await page.goto("/catalog/brands");
    await page.getByRole("button", { name: /new brand/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /add a brand/i })).toBeVisible();
    // Required fields get an sr-only "required" suffix, so match by prefix.
    await expect(dialog.getByLabel(/^name/i)).toBeVisible();
    await expect(dialog.getByLabel(/logo url/i)).toBeVisible();
  });
});

// ─── Categories ────────────────────────────────────────────────────────

test.describe("catalog/categories", () => {
  // The category tree endpoint (categories/tree) also matches the broad
  // categories** glob, so register the search glob first then the more
  // specific tree mock LAST — Playwright matches the latest route first.
  async function mockCategories(
    page: import("@playwright/test").Page,
    items: typeof CATEGORY_OUTDOOR[],
  ) {
    await mockJsonResponse(page, "**/api/v1/catalog/categories**", paged(items, { pageSize: 50 }));
    await mockJsonResponse(page, "**/api/v1/catalog/categories/tree", items);
  }

  test("renders the heading and a row built from mocked data", async ({ page }) => {
    await mockCategories(page, [CATEGORY_OUTDOOR]);

    await page.goto("/catalog/categories");

    await expect(page.getByRole("heading", { name: /categories/i })).toBeVisible();
    await expect(page.getByText("Outdoor Gear").last()).toBeVisible();
    await expect(page.getByText("1 category found")).toBeVisible();
  });

  test("shows the empty state when no categories exist", async ({ page }) => {
    await mockCategories(page, []);

    await page.goto("/catalog/categories");

    await expect(page.getByRole("heading", { name: /no categories yet/i })).toBeVisible();
    await expect(
      page.getByText(/categories give your catalog its tree/i),
    ).toBeVisible();
  });

  test("opens the New category create dialog with its form fields", async ({ page }) => {
    await mockCategories(page, [CATEGORY_OUTDOOR]);

    await page.goto("/catalog/categories");
    await page.getByRole("button", { name: /new category/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /add a category/i })).toBeVisible();
    await expect(dialog.getByLabel(/^name/i)).toBeVisible();
    await expect(dialog.getByLabel(/description/i)).toBeVisible();
  });
});

// ─── Products (list) ──────────────────────────────────────────────────

test.describe("catalog/products", () => {
  // The products page fires three reads on load: the product search plus
  // brands?pageSize=200 and categories?pageSize=200 for the filter dropdowns.
  // Mock all three or the page hangs.
  async function mockProductsPage(
    page: import("@playwright/test").Page,
    products: typeof PRODUCT_TENT[],
  ) {
    await mockJsonResponse(page, "**/api/v1/catalog/brands**", paged([BRAND_GLOBEX], { pageSize: 200 }));
    await mockJsonResponse(page, "**/api/v1/catalog/categories**", paged([CATEGORY_OUTDOOR], { pageSize: 200 }));
    await mockJsonResponse(page, "**/api/v1/catalog/products**", paged(products, { pageSize: 25 }));
  }

  test("renders the heading and a product row from mocked data", async ({ page }) => {
    await mockProductsPage(page, [PRODUCT_TENT]);

    await page.goto("/catalog/products");

    await expect(page.getByRole("heading", { name: /products/i })).toBeVisible();
    await expect(page.getByText("Trailhead Tent").last()).toBeVisible();
    await expect(page.getByText("GLX-TENT-001").last()).toBeVisible();
    await expect(page.getByText("1 product found")).toBeVisible();
  });

  test("shows the empty state when no products match", async ({ page }) => {
    await mockProductsPage(page, []);

    await page.goto("/catalog/products");

    await expect(page.getByRole("heading", { name: /no products yet/i })).toBeVisible();
    await expect(
      page.getByText(/add your first product to start selling/i),
    ).toBeVisible();
  });

  test("opens the New product create dialog with its form fields", async ({ page }) => {
    await mockProductsPage(page, [PRODUCT_TENT]);

    await page.goto("/catalog/products");
    await page.getByRole("button", { name: /new product/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /add a product/i })).toBeVisible();
    await expect(dialog.getByLabel(/^name/i)).toBeVisible();
    await expect(dialog.getByLabel(/^sku/i)).toBeVisible();
  });
});

// ─── Product detail ──────────────────────────────────────────────────

test.describe("catalog/products/:productId", () => {
  // Detail fires getProductById, then the brand + category follow-up gets.
  // Register the broad globs first, the by-id product mock LAST so it wins
  // over the (unused-here) products list glob.
  async function mockProductDetail(page: import("@playwright/test").Page) {
    await mockJsonResponse(page, `**/api/v1/catalog/brands/${BRAND_GLOBEX.id}`, BRAND_GLOBEX);
    await mockJsonResponse(page, `**/api/v1/catalog/categories/${CATEGORY_OUTDOOR.id}`, CATEGORY_OUTDOOR);
    await mockJsonResponse(page, `**/api/v1/catalog/products/${PRODUCT_TENT.id}`, PRODUCT_TENT);
  }

  test("renders the product name, SKU, and a back link", async ({ page }) => {
    await mockProductDetail(page);

    await page.goto(`/catalog/products/${PRODUCT_TENT.id}`);

    await expect(page.getByRole("heading", { name: /trailhead tent/i })).toBeVisible();
    await expect(page.getByText("GLX-TENT-001").first()).toBeVisible();
    await expect(
      page.getByRole("link", { name: /back to products/i }),
    ).toBeVisible();
  });

  test("renders brand and category resolved from the follow-up gets", async ({ page }) => {
    await mockProductDetail(page);

    await page.goto(`/catalog/products/${PRODUCT_TENT.id}`);

    await expect(page.getByRole("heading", { name: /trailhead tent/i })).toBeVisible();
    await expect(page.getByRole("link", { name: "Globex Gadgets" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Outdoor Gear" })).toBeVisible();
  });

  test("shows the not-found panel when the product 404s", async ({ page }) => {
    // A null/empty product body resolves the query without data → NotFound.
    await mockJsonResponse(page, `**/api/v1/catalog/products/${PRODUCT_TENT.id}`, null);

    await page.goto(`/catalog/products/${PRODUCT_TENT.id}`);

    await expect(
      page.getByRole("heading", { name: /product not found/i }),
    ).toBeVisible();
  });
});
