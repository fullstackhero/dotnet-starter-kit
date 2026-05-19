/**
 * Build-time GitHub star fetcher with a hard-coded fallback.
 *
 * Astro evaluates component frontmatter at build time on Node, so a top-level
 * `await getStars()` in any .astro file resolves once per build and the
 * resolved value is baked into the static HTML. The result is memoized at
 * module scope so all callers in a single build share one network round-trip.
 *
 * The fallback ensures pages still build when:
 *   - The build environment is offline.
 *   - GitHub returns a 403/429 (unauthenticated rate limit is 60/hour/IP).
 *
 * Bump FALLBACK in this file every few weeks so cached/offline builds don't
 * lag too far behind reality.
 */

const REPO = 'fullstackhero/dotnet-starter-kit';
const FALLBACK_COUNT = 6500;

let cached: Stars | null = null;

export type Stars = {
  count: number;
  /** Compact human-readable form: 950 → "950", 1200 → "1.2k", 10500 → "10.5k". */
  formatted: string;
};

export async function getStars(): Promise<Stars> {
  if (cached) return cached;
  try {
    const res = await fetch(`https://api.github.com/repos/${REPO}`, {
      headers: {
        Accept: 'application/vnd.github+json',
        'User-Agent': 'fullstackhero-docs',
      },
    });
    if (!res.ok) throw new Error(`GitHub API responded ${res.status}`);
    const data = (await res.json()) as { stargazers_count?: number };
    const count = data.stargazers_count ?? FALLBACK_COUNT;
    cached = { count, formatted: format(count) };
  } catch {
    cached = { count: FALLBACK_COUNT, formatted: format(FALLBACK_COUNT) };
  }
  return cached;
}

function format(count: number): string {
  if (count >= 10000) return `${Math.floor(count / 1000)}k`;
  if (count >= 1000) {
    const k = count / 1000;
    // 1.0k → 1k; 1.5k stays 1.5k
    return k % 1 === 0 ? `${k}k` : `${k.toFixed(1)}k`;
  }
  return String(count);
}
