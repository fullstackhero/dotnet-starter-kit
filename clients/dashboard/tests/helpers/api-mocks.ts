import type { Page, Route } from "@playwright/test";

/**
 * Shared API mocking helpers. Tests use page.route() to intercept fetch
 * calls to the dashboard's API and respond deterministically — no real
 * backend required, no DB seeding, no flake.
 *
 * Convention: pass the URL pattern as a substring (Playwright accepts
 * string|RegExp|Predicate); we use simple URL.includes() matching via
 * a glob pattern so tests stay readable.
 */

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

/**
 * Respond to any matching request with 200 OK + JSON body.
 *
 * @example
 *   await mockJsonResponse(page, "**\/api/v1/identity/forgot-password", "");
 */
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

/**
 * Respond with an RFC 7807 ProblemDetails-shaped error matching the
 * backend's exception handler output. The dashboard's apiFetch parses
 * this into ApiRequestError.problem.
 */
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

/**
 * Capture the request body sent to a given URL so tests can assert what
 * the UI actually posted. Returns a getter that resolves once the call
 * has been made.
 *
 * @example
 *   const sentBody = captureRequest(page, "**\/forgot-password");
 *   await page.getByRole("button", { name: /send/i }).click();
 *   expect(await sentBody.value()).toMatchObject({ email: "...@..." });
 */
export function captureRequest(page: Page, urlGlob: string) {
  let resolved: { body: unknown; headers: Record<string, string> } | null = null;
  const waiters: Array<(v: { body: unknown; headers: Record<string, string> }) => void> = [];

  void page.route(urlGlob, async (route: Route) => {
    const req = route.request();
    const raw = req.postData();
    const body: unknown = raw ? JSON.parse(raw) : undefined;
    const headers = req.headers();
    resolved = { body, headers };
    for (const w of waiters) w(resolved);
    waiters.length = 0;
    await route.fulfill({ status: 200, headers: JSON_HEADERS, body: '""' });
  });

  return {
    async value(timeoutMs = 5_000) {
      if (resolved) return resolved;
      return await new Promise<{ body: unknown; headers: Record<string, string> }>(
        (resolve, reject) => {
          const t = setTimeout(
            () => reject(new Error(`captureRequest timed out waiting for ${urlGlob}`)),
            timeoutMs,
          );
          waiters.push((v) => {
            clearTimeout(t);
            resolve(v);
          });
        },
      );
    },
  };
}
