/// <reference types="@cloudflare/workers-types" />

// Workers entry: handles /api/views/* and delegates everything else
// to the static assets binding. Assets win over the worker by default
// for matching files, so /docs/**, /_astro/**, /index.html etc. are
// still served straight from R2-backed assets storage without the
// worker being invoked.

export interface Env {
  DB: D1Database;
  DEDUPE: KVNamespace;
  ASSETS: Fetcher;
  DEDUPE_SALT?: string;
}

const MAX_SLUG_LEN = 256;
const DEDUPE_TTL_SECONDS = 60 * 60; // 1 hour
const SLUG_PREFIX = '/docs/';

export default {
  async fetch(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === '/api/views' || url.pathname.startsWith('/api/views/')) {
      return handleViews(request, env, ctx);
    }

    return env.ASSETS.fetch(request);
  },
};

async function handleViews(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
  if (request.method === 'POST') {
    return handleIncrement(request, env, ctx);
  }
  if (request.method === 'GET') {
    return handleRead(request, env);
  }
  return json({ error: 'method-not-allowed' }, 405, { Allow: 'GET, POST' });
}

async function handleIncrement(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
  const slug = await extractSlug(request);
  if (!slug) return json({ error: 'invalid-slug' }, 400);

  const ip = request.headers.get('cf-connecting-ip') ?? '';
  const ua = request.headers.get('user-agent') ?? '';
  const seenKey = await dedupeKey(slug, ip, ua, env.DEDUPE_SALT ?? '');
  const seen = await env.DEDUPE.get(seenKey);

  if (!seen) {
    // Two-statement batch keeps both writes atomic from the caller's view
    // and avoids two RTTs to the D1 region. The UPSERT seeds count=1 for
    // brand-new slugs and increments existing ones.
    await env.DB.batch([
      env.DB
        .prepare(
          `INSERT INTO views (slug, count, updated_at)
           VALUES (?1, 1, unixepoch())
           ON CONFLICT(slug) DO UPDATE
             SET count = count + 1,
                 updated_at = unixepoch()`,
        )
        .bind(slug),
    ]);

    // Dedup write is fire-and-forget — failure here just means the next
    // beacon from the same IP increments again, which is fine.
    ctx.waitUntil(env.DEDUPE.put(seenKey, '1', { expirationTtl: DEDUPE_TTL_SECONDS }));
  }

  const row = await env.DB.prepare('SELECT count FROM views WHERE slug = ?1')
    .bind(slug)
    .first<{ count: number }>();

  return json({ views: row?.count ?? 0 }, 200, {
    // Beacons are uncacheable — every request must reach the worker.
    'Cache-Control': 'no-store',
  });
}

async function handleRead(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);
  const slug = normalizeSlug(url.searchParams.get('slug'));
  if (!slug) return json({ error: 'invalid-slug' }, 400);

  const row = await env.DB.prepare('SELECT count FROM views WHERE slug = ?1')
    .bind(slug)
    .first<{ count: number }>();

  return json({ views: row?.count ?? 0 }, 200, {
    // Edge cache 30s — read traffic can dogpile a popular page.
    'Cache-Control': 'public, max-age=30, s-maxage=30',
  });
}

async function extractSlug(request: Request): Promise<string | null> {
  // Accept slug from JSON body { slug } or ?slug=.
  if (request.headers.get('content-type')?.includes('application/json')) {
    try {
      const body = (await request.json()) as { slug?: unknown };
      return normalizeSlug(typeof body.slug === 'string' ? body.slug : null);
    } catch {
      return null;
    }
  }
  const url = new URL(request.url);
  return normalizeSlug(url.searchParams.get('slug'));
}

function normalizeSlug(raw: string | null): string | null {
  if (!raw) return null;
  // Strip query/hash if the client sent the whole href by mistake.
  const noQuery = raw.split('?')[0].split('#')[0];
  // Force leading slash, strip trailing slash (except root).
  const withSlash = noQuery.startsWith('/') ? noQuery : `/${noQuery}`;
  const trimmed = withSlash.length > 1 ? withSlash.replace(/\/+$/, '') : withSlash;
  // Whitelist: docs pages only. Keeps the table from filling with
  // arbitrary paths if a beacon ever fires from the marketing site.
  if (!trimmed.startsWith(SLUG_PREFIX) && trimmed !== '/docs') return null;
  if (trimmed.length > MAX_SLUG_LEN) return null;
  // Path-only — reject anything with control chars.
  if (/[\x00-\x1f\x7f]/.test(trimmed)) return null;
  return trimmed;
}

// Dedup identity = IP + User-Agent + slug. Folding in the UA means two
// devices behind one NAT'd public IP (phone + desktop on the same WiFi)
// hash to different keys and each count once, while reloads from the same
// device still dedupe within the TTL window.
async function dedupeKey(slug: string, ip: string, ua: string, salt: string): Promise<string> {
  const data = new TextEncoder().encode(`${salt}:${ip}:${ua}:${slug}`);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return `dedup:${toHex(new Uint8Array(hash))}`;
}

function toHex(bytes: Uint8Array): string {
  let out = '';
  for (let i = 0; i < bytes.length; i++) {
    out += bytes[i].toString(16).padStart(2, '0');
  }
  return out;
}

function json(body: unknown, status = 200, extra: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json; charset=utf-8', ...extra },
  });
}
