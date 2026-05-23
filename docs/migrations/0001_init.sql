-- Page view counters for /docs/* pages.
-- slug is the full pathname (e.g. "/docs/getting-started/installation"),
-- stored without trailing slash. updated_at is a unix epoch (seconds).
CREATE TABLE IF NOT EXISTS views (
  slug       TEXT PRIMARY KEY NOT NULL,
  count      INTEGER NOT NULL DEFAULT 0,
  updated_at INTEGER NOT NULL DEFAULT (unixepoch())
);

-- Supports "top pages" queries: SELECT slug, count FROM views ORDER BY count DESC LIMIT N
CREATE INDEX IF NOT EXISTS idx_views_count ON views(count DESC);
