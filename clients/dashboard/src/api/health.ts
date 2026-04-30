import { env } from "@/env";

export type HealthStatus = "Healthy" | "Degraded" | "Unhealthy";

export type HealthEntry = {
  name: string;
  status: HealthStatus;
  description?: string | null;
  durationMs: number;
  details?: Record<string, unknown> | null;
};

export type HealthResult = {
  status: HealthStatus;
  results: HealthEntry[];
};

export type HealthSnapshot = HealthResult & {
  /** Wall-clock time the response landed in the browser. */
  fetchedAt: string;
  /** Round-trip from request start to response parse, in ms. */
  roundTripMs: number;
  /** HTTP status. 200 = healthy, 503 = degraded/unhealthy (still has payload). */
  httpStatus: number;
};

/**
 * Fetch the readiness report. Anonymous endpoint — bypasses apiFetch's
 * auth/refresh dance. We accept both 200 and 503 because the API now
 * returns the same JSON shape for both (so we can show *which* check
 * failed). Network errors propagate as exceptions.
 */
export async function getReadiness(signal?: AbortSignal): Promise<HealthSnapshot> {
  const t0 = performance.now();
  const url = `${env.apiBase}/health/ready`;
  const response = await fetch(url, {
    method: "GET",
    headers: { Accept: "application/json" },
    signal,
  });
  const roundTripMs = performance.now() - t0;

  if (response.status !== 200 && response.status !== 503) {
    throw new Error(`Health endpoint returned ${response.status}`);
  }

  const body = (await response.json()) as HealthResult;
  return {
    ...body,
    fetchedAt: new Date().toISOString(),
    roundTripMs,
    httpStatus: response.status,
  };
}
