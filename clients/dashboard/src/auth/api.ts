import { ApiRequestError, apiFetch } from "@/lib/api-client";

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

export function issueToken(input: {
  email: string;
  password: string;
  tenant: string;
  /** Authenticator (TOTP) code — sent only on the second step of a 2FA sign-in. */
  twoFactorCode?: string;
}) {
  return apiFetch<TokenResponse>("/api/v1/identity/token/issue", {
    method: "POST",
    body: JSON.stringify({
      email: input.email,
      password: input.password,
      ...(input.twoFactorCode ? { twoFactorCode: input.twoFactorCode } : {}),
    }),
    // X-FSH-App tells the API this credential request originated from the
    // tenant dashboard. The server uses it to enforce the SuperAdmin / app
    // boundary — a root-tenant login submitted with X-FSH-App=dashboard is
    // rejected with 403 instead of receiving a usable token.
    headers: { tenant: input.tenant, "X-FSH-App": "dashboard" },
    skipAuth: true,
  });
}

// Stable ProblemDetails `code`s the token endpoint sets for the second factor
// (the MessageKey IdentityService throws with). The detail text is localized
// under the request culture, so branch on the code, never on the prose.
const TWO_FACTOR_REQUIRED = "Identity.TwoFactorRequired";
const TWO_FACTOR_INVALID = "Identity.TwoFactorInvalid";

function problemCode(error: unknown): string | undefined {
  if (!(error instanceof ApiRequestError) || error.status !== 401) return undefined;
  const code = error.problem?.code;
  return typeof code === "string" ? code : undefined;
}

/** The password was right, but the account has 2FA on and no code was sent. */
export function isTwoFactorRequiredError(error: unknown): boolean {
  return problemCode(error) === TWO_FACTOR_REQUIRED;
}

/** The authenticator code was wrong or expired. */
export function isTwoFactorInvalidError(error: unknown): boolean {
  return problemCode(error) === TWO_FACTOR_INVALID;
}
