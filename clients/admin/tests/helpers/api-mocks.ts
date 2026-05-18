import type { Page, Route } from "@playwright/test";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

export async function mockJsonResponse<T>(
  page: Page,
  urlGlob: string,
  body: T,
  options: { method?: string; status?: number } = {},
) {
  await page.route(urlGlob, async (route: Route) => {
    if (options.method && route.request().method() !== options.method) {
      await route.fallback();
      return;
    }
    await route.fulfill({
      status: options.status ?? 200,
      headers: JSON_HEADERS,
      body: typeof body === "string" ? body : JSON.stringify(body),
    });
  });
}

export async function mockProblemDetails(
  page: Page,
  urlGlob: string,
  status: number,
  problem: { title?: string; detail?: string; errors?: Record<string, string[]> },
) {
  await page.route(urlGlob, async (route: Route) => {
    await route.fulfill({
      status,
      headers: { "Content-Type": "application/problem+json" },
      body: JSON.stringify({
        type: `https://httpstatuses.io/${status}`,
        title: problem.title ?? "Error",
        status,
        detail: problem.detail,
        errors: problem.errors,
      }),
    });
  });
}
