import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";
import { decodeJwt } from "@/auth/jwt";

export type ApiError = {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  // Dev-only extension surfaced on 401 by ConfigureJwtBearerOptions.
  reason?: string;
  // Allow any other ProblemDetails extensions through.
  [key: string]: unknown;
};

export class ApiRequestError extends Error {
  readonly status: number;
  readonly problem?: ApiError;

  constructor(status: number, message: string, problem?: ApiError) {
    super(message);
    this.status = status;
    this.problem = problem;
  }
}

/**
 * True when an error is the API's "tenant has been deactivated" 403. The
 * deactivated-tenant guard (MultitenancyModule) rejects *every* request once a
 * tenant is switched off, so this can surface from any query/mutation while a
 * user is mid-session. There is no machine-readable code on the ProblemDetails,
 * so we match the guard's detail text. A global query/mutation error hook uses
 * this to route the user to the dedicated `/tenant-deactivated` page rather than
 * leaving the dead 403 banner stuck under a half-loaded surface.
 */
export function isTenantDeactivatedError(error: unknown): boolean {
  if (!(error instanceof ApiRequestError) || error.status !== 403) return false;
  const detail = error.problem?.detail ?? error.message ?? "";
  return detail.toLowerCase().includes("tenant has been deactivated");
}

/**
 * True when an error is a 401 fired against an *impersonation* session — i.e.
 * the operator's grant was revoked (via /impersonation/revoke) or the
 * short-lived impersonation token expired. Both surface as a 401 from the
 * server's OnTokenValidated hook (ConfigureJwtBearerOptions).
 *
 * Detection is intentionally message-agnostic: in Production the 401 body is
 * opaque (the "Impersonation grant revoked or ended" reason is dev-only), so we
 * key off the durable shape instead — a 401 while the *currently installed*
 * access token carries the `act_sub` (impersonation) claim. Impersonation
 * sessions never hold a refresh token (token-store drops the refresh slot on
 * beginImpersonation), so such a 401 always propagates here rather than being
 * silently refreshed-and-retried by apiFetch. A global query/mutation error
 * hook (query-client.ts) uses this to route to the /impersonation-ended
 * terminal page instead of leaving a dead error banner under a half-loaded
 * dashboard — mirrors isTenantDeactivatedError.
 */
export function isImpersonationRevokedError(error: unknown): boolean {
  if (!(error instanceof ApiRequestError) || error.status !== 401) return false;
  const claims = decodeJwt(tokenStore.getAccessToken());
  return claims?.act_sub != null;
}

type RequestInitEx = RequestInit & {
  skipAuth?: boolean;
  /**
   * Per-request timeout in milliseconds. Browser fetch has no default timeout
   * so a permanently stalled request would hang React Query's promise forever.
   * Defaults to 30s; callers wiring long-poll surfaces (search streams, file
   * uploads) should override this explicitly.
   */
  timeoutMs?: number;
};

const DEFAULT_TIMEOUT_MS = 30_000;

function withTimeout(
  init: RequestInit,
  timeoutMs: number,
  externalSignal?: AbortSignal | null,
): { init: RequestInit; cleanup: () => void } {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(new DOMException("Request timed out", "TimeoutError")), timeoutMs);
  const onExternalAbort = () => controller.abort(externalSignal?.reason);
  if (externalSignal) {
    if (externalSignal.aborted) {
      controller.abort(externalSignal.reason);
    } else {
      externalSignal.addEventListener("abort", onExternalAbort, { once: true });
    }
  }
  return {
    init: { ...init, signal: controller.signal },
    cleanup: () => {
      window.clearTimeout(timeoutId);
      externalSignal?.removeEventListener("abort", onExternalAbort);
    },
  };
}

let refreshPromise: Promise<void> | null = null;

export async function refreshAccessToken() {
  const refreshToken = tokenStore.getRefreshToken();
  const accessToken = tokenStore.getAccessToken();
  if (!refreshToken || !accessToken) {
    throw new ApiRequestError(401, "No refresh token");
  }

  // Server's RefreshTokenCommand requires both `token` (the existing, possibly expired
  // access token, used to cross-check the subject) and `refreshToken`. Sending only one
  // of them fails FluentValidation and surfaces as 500.
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  const response = await fetch(`${env.apiBase}/api/v1/identity/token/refresh`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(tenant ? { tenant } : {}),
    },
    body: JSON.stringify({ token: accessToken, refreshToken }),
    // A stalled refresh would otherwise hang forever and block every queued
    // 401-retry awaiting the shared refreshPromise.
    signal: AbortSignal.timeout(DEFAULT_TIMEOUT_MS),
  });

  if (!response.ok) {
    tokenStore.clear();
    throw new ApiRequestError(response.status, "Refresh failed");
  }

  // Server's RefreshTokenCommandResponse returns `{ token, refreshToken, refreshTokenExpiryTime }` —
  // note the rotated access token is on `token`, not `accessToken`.
  const tokens = (await response.json()) as {
    token: string;
    refreshToken: string;
  };
  tokenStore.setTokens(tokens.token, tokens.refreshToken);
}

async function parseError(response: Response): Promise<ApiError | undefined> {
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) {
    return undefined;
  }
  try {
    return (await response.json()) as ApiError;
  } catch {
    return undefined;
  }
}

export async function apiFetch<T = unknown>(
  path: string,
  init: RequestInitEx = {},
): Promise<T> {
  const { skipAuth, headers, timeoutMs = DEFAULT_TIMEOUT_MS, signal, ...rest } = init;

  const mergedHeaders = new Headers(headers);
  if (!mergedHeaders.has("Content-Type") && rest.body && typeof rest.body === "string") {
    mergedHeaders.set("Content-Type", "application/json");
  }

  if (!skipAuth) {
    const accessToken = tokenStore.getAccessToken();
    if (accessToken) {
      mergedHeaders.set("Authorization", `Bearer ${accessToken}`);
    } else {
      // We're not anonymous (skipAuth=false) but the token is gone — likely a
      // manual localStorage clear that AuthContext missed. Clear remaining
      // session state and surface a clean 401 so the UI flips to /login
      // instead of repeatedly firing tokenless requests.
      tokenStore.clear();
      throw new ApiRequestError(401, "Not signed in", {
        status: 401,
        title: "Unauthorized",
        detail: "Your session is no longer available. Please sign in again.",
      });
    }
  }

  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant && !mergedHeaders.has("tenant")) {
    mergedHeaders.set("tenant", tenant);
  }

  const url = path.startsWith("http") ? path : `${env.apiBase}${path}`;
  const initialTimer = withTimeout({ ...rest, headers: mergedHeaders }, timeoutMs, signal);
  let response: Response;
  try {
    response = await fetch(url, initialTimer.init);
  } finally {
    initialTimer.cleanup();
  }

  if (response.status === 401 && !skipAuth && tokenStore.getRefreshToken()) {
    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null;
    });

    try {
      await refreshPromise;
    } catch (e) {
      throw e instanceof ApiRequestError
        ? e
        : new ApiRequestError(401, "Session expired");
    }

    const retryHeaders = new Headers(mergedHeaders);
    retryHeaders.set("Authorization", `Bearer ${tokenStore.getAccessToken() ?? ""}`);
    const retryTimer = withTimeout({ ...rest, headers: retryHeaders }, timeoutMs, signal);
    try {
      response = await fetch(url, retryTimer.init);
    } finally {
      retryTimer.cleanup();
    }
  }

  if (!response.ok) {
    const problem = await parseError(response);
    throw new ApiRequestError(
      response.status,
      problem?.title ?? problem?.detail ?? response.statusText,
      problem,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
