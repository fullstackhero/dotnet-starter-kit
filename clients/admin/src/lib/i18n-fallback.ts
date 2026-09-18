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
