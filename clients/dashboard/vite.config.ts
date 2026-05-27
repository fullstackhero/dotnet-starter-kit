import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiBase = env.VITE_API_BASE_URL ?? "https://localhost:7030";

  return {
    plugins: [react(), tailwindcss()],
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
