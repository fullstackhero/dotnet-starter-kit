# Worklog

Running operations journal for this project's real deployments and infra work.

## Structure

- `YYYY-MM-DD-<topic>.md` — one markdown log per working session: what was done,
  every command that mattered, every error hit with its fix, risks identified,
  and the state the system was left in.
- `index.html` — self-contained visual dashboard (open in any browser, no
  server needed): architecture diagram, deployment timeline, charts, risk
  register, and quick-reference commands. Updated alongside the session logs.

## Conventions (for humans and AI sessions alike)

1. **Every working session appends here.** New session → new
   `YYYY-MM-DD-<topic>.md` + update `index.html` (session timeline, risk
   register, current-state numbers) before finishing.
2. **Never store secrets.** No passwords, keys, or tokens — only *where* they
   live and *how* to look them up (e.g. `grep POSTGRES_PASSWORD deploy/docker/.env`
   on the VPS).
3. **Errors are the most valuable entries.** Record the exact error text, the
   root cause, and the fix — future deployments grep this folder first.
4. **Keep `index.html` self-contained** — inline CSS/JS/SVG only, no CDN links.

## Related docs

- `deploy/docker/DEPLOY-VPS.md` — the reusable step-by-step deployment runbook
  (distilled from these logs; update it when a session discovers something
  every future deploy needs).
