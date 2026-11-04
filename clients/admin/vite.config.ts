import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiBase = env.VITE_API_BASE_URL ?? "http://localhost:5030";

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        "@": path.resolve(import.meta.dirname, "./src"),
      },
    },
    build: {
      // Left to its defaults, Rolldown splits the code the entry loads up front into ~45 tiny
      // chunks. Each compresses on its own, which costs ~15 KiB gzip on first load over a few
      // larger files. These groups put everything the entry statically imports (the `$initial`
      // tag) into one vendor chunk and one app chunk. Route pages stay lazy: they aren't
      // `$initial`, so automatic chunking still handles them. See #1462.
      rolldownOptions: {
        output: {
          codeSplitting: {
            groups: [
              { name: "vendor", test: /node_modules[\\/]/, tags: ["$initial"], priority: 2 },
              { name: "app", tags: ["$initial"], priority: 1 },
            ],
          },
        },
      },
      // The vendor chunk is ~560 KB minified (~175 KiB gzip) on purpose, see above.
      chunkSizeWarningLimit: 600,
    },
    server: {
      port: 5173,
      strictPort: true,
      proxy: {
        // ws: true forwards the WebSocket upgrade for SignalR's hub at /api/v1/realtime/hub
        // (config.json ships apiBase "", so the hub goes through this proxy). Without it the
        // negotiate succeeds over HTTP but the upgrade lands on Vite's own server and the
        // connection falls back to a slower transport.
        "/api": { target: apiBase, changeOrigin: true, secure: false, ws: true },
        "/health": { target: apiBase, changeOrigin: true, secure: false },
        "/openapi": { target: apiBase, changeOrigin: true, secure: false },
        "/scalar": { target: apiBase, changeOrigin: true, secure: false },
      },
    },
  };
});
