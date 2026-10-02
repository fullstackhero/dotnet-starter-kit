/**
 * What renders when a key is not in the catalog.
 *
 * Two kinds of caller end up here. One builds the key from a server value (`status.${x}`) and has
 * nothing better to show than the value itself, so it degrades to the last segment — the readable
 * name the UI showed before that surface was localized. The other passes an English `defaultValue`
 * and expects it back.
 *
 * i18next hands `parseMissingKeyHandler` both the key and the `defaultValue`, and whatever the
 * handler returns is what renders — a handler that only looks at the key silently replaces every
 * fallback in the app with a truncation of its own key ("Create users" becomes "Create").
 */
export function missingKeyFallback(key: string, defaultValue?: string | null): string {
  if (typeof defaultValue === "string") {
    return defaultValue;
  }

  const segment = key.split(/[.:]/).pop() ?? key;
  return segment.charAt(0).toUpperCase() + segment.slice(1);
}

/**
 * The language chain a missing key walks before `missingKeyFallback` runs.
 *
 * en-US is the only catalog guaranteed complete (a pt-BR translation may lag behind), so the chain
 * always ends there. A deployment defaulting to pt-BR keeps pt-BR first, so a browser locale the
 * app does not offer still lands on the deployment's language.
 */
export function fallbackChain(deploymentDefault: string, supported: readonly string[]): string | string[] {
  if (deploymentDefault !== "en-US" && supported.includes(deploymentDefault)) {
    return [deploymentDefault, "en-US"];
  }
  return "en-US";
}
