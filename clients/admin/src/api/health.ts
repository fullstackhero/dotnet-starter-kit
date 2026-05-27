import { env } from "@/env";

export type HealthStatus = "Healthy" | "Degraded" | "Unhealthy" | string;

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

/**
 * Health probes are anonymous — bypass the apiClient so we don't drag the
 * tenant header / auth token into a public endpoint, and so we can read
 * the body on a 503 (apiClient would throw before parsing).
 */
async function fetchHealth(path: string, timeoutMs = 8_000): Promise<HealthResult> {
  const url = `${env.apiBase}${path}`;
  const response = await fetch(url, {
    method: "GET",
    signal: AbortSignal.timeout(timeoutMs),
    headers: { Accept: "application/json" },
  });

  // /health/live returns 200; /health/ready returns 200 OR 503 with body.
  if (!response.ok && response.status !== 503) {
    return {
      status: "Unhealthy",
      results: [
        {
          name: "probe",
          status: "Unhealthy",
          description: `Probe failed: ${response.status} ${response.statusText}`,
          durationMs: 0,
        },
      ],
    };
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) {
    return {
      status: response.ok ? "Healthy" : "Unhealthy",
      results: [],
    };
  }

  return (await response.json()) as HealthResult;
}

export function getLiveness(): Promise<HealthResult> {
  return fetchHealth("/health/live");
}

export function getReadiness(): Promise<HealthResult> {
  return fetchHealth("/health/ready");
}
