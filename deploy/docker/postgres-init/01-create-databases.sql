-- Postgres init for fullstackhero
-- This file runs only on first boot (when /var/lib/postgresql/data is empty).
-- The `fsh` database itself is created by POSTGRES_DB= in the compose env;
-- we just add the extensions the framework relies on.

\connect fsh

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- pg_trgm is used by the full-text search GIN indexes (chat search).
CREATE EXTENSION IF NOT EXISTS pg_trgm;
