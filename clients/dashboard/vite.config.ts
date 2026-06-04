import { defineConfig, loadEnv, type Plugin } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import fs from "node:fs";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiBase = env.VITE_API_BASE_URL ?? "https://localhost:7030";

  // Dev only: serve the runtime config with apiBase pointed straight at the API. This makes
  // REST + the long-lived SSE / SignalR streams hit localhost:7030 directly instead of being
  // proxied through Vite. Otherwise those streams hold connections on localhost:5174 and, under
  // HTTP/1.1's ~6-per-host cap, intermittently starve lazy route-chunk loads ("page won't load").
  // The committed public/config.json keeps apiBase="" as the same-origin production default.
  const devDirectApiConfig: Plugin = {
    name: "fsh-dev-direct-api-config",
    apply: "serve",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = req.url ?? "";
        if (url !== "/config.json" && !url.startsWith("/config.json?")) {
          next();
          return;
        }
        let base: Record<string, unknown> = {};
        try {
          base = JSON.parse(
            fs.readFileSync(path.resolve(__dirname, "public/config.json"), "utf8"),
          ) as Record<string, unknown>;
        } catch {
          // Fall back to defaults if the file is missing/unreadable.
        }
        res.setHeader("Content-Type", "application/json");
        res.setHeader("Cache-Control", "no-store");
        res.end(JSON.stringify({ ...base, apiBase }));
      });
    },
  };

  return {
    plugins: [devDirectApiConfig, react(), tailwindcss()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      port: 5174,
      strictPort: true,
      proxy: {
        // ws: true forwards the WebSocket upgrade used by SignalR's hub
        // transport at /api/v1/realtime/hub. Without it the negotiate
        // succeeds over HTTP but the WS upgrade falls into Vite's own
        // dev server, so the chat status stalls on "CONNECTING" while
        // SignalR retries forever.
        "/api": { target: apiBase, changeOrigin: true, secure: false, ws: true },
        "/openapi": { target: apiBase, changeOrigin: true, secure: false },
        "/scalar": { target: apiBase, changeOrigin: true, secure: false },
        // Health probes live at the root (not under /api). Without this the
        // dashboard's /system/health page 404s in dev because Vite serves
        // the request itself instead of proxying to the API.
        "/health": { target: apiBase, changeOrigin: true, secure: false },
      },
    },
  };
});
