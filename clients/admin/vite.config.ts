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
