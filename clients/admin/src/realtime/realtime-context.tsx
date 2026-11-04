import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import type { HubConnection } from "@microsoft/signalr";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";

// SignalR is about 25 KB gzip. It's imported on first connect, so the sign-in page and the
// initial bundle don't carry it. Same approach as the dashboard's provider.
type SignalRModule = typeof import("@microsoft/signalr");
let signalRPromise: Promise<SignalRModule> | null = null;
function loadSignalR(): Promise<SignalRModule> {
  if (!signalRPromise) signalRPromise = import("@microsoft/signalr");
  return signalRPromise;
}

export type RealtimeStatus = "idle" | "connecting" | "connected" | "reconnecting" | "error";

type Listener = (payload: unknown) => void;

type RealtimeContextValue = {
  status: RealtimeStatus;
  /** Wire a handler for a server event. Returns an unsubscribe function. */
  on: <T = unknown>(event: string, handler: (payload: T) => void) => () => void;
};

const RealtimeContext = createContext<RealtimeContextValue | null>(null);

/**
 * RealtimeProvider — single shared SignalR connection to /api/v1/realtime/hub
 * authenticated via ?access_token=. Reconnect with backoff [2s, 5s, 10s, 30s].
 * Admin only wires NotificationCreated today; the dashboard's variant subscribes
 * to chat events too. Re-runs on tokenStore changes so login/refresh/impersonation
 * cleanly tear down and rebuild.
 */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<RealtimeStatus>("idle");
  const connectionRef = useRef<HubConnection | null>(null);
  const listenersRef = useRef<Map<string, Set<Listener>>>(new Map());
  const stoppedRef = useRef(false);
  const [tokenEpoch, setTokenEpoch] = useState(0);

  useEffect(() => tokenStore.subscribe(() => setTokenEpoch((n) => n + 1)), []);

  useEffect(() => {
    stoppedRef.current = false;
    const backoffs = [0, 2_000, 5_000, 10_000, 30_000];

    const buildConnection = async () => {
      const sr = await loadSignalR();
      const url = `${env.apiBase || ""}/api/v1/realtime/hub`;
      return new sr.HubConnectionBuilder()
        .withUrl(url, {
          accessTokenFactory: () => tokenStore.getAccessToken() ?? "",
          transport:
            sr.HttpTransportType.WebSockets |
            sr.HttpTransportType.ServerSentEvents |
            sr.HttpTransportType.LongPolling,
        })
        // SignalR's automatic reconnect re-negotiates internally, bypassing connect()'s
        // token guard. When the user is signed out (token cleared/expired) every reconnect
        // fires an anonymous negotiate the API 401s and logs — so stop retrying the moment
        // there's no token. onclose then rebuilds lazily once a token lands back in storage.
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (ctx) =>
            tokenStore.getAccessToken()
              ? (backoffs.slice(1)[ctx.previousRetryCount] ?? null)
              : null,
        })
        .configureLogging(sr.LogLevel.Warning)
        .build();
    };

    const wireListeners = (conn: HubConnection) => {
      const events = new Set<string>();
      const sink = (name: string) => (payload: unknown) => {
        const list = listenersRef.current.get(name);
        if (list) for (const fn of list) fn(payload);
      };
      const wire = (name: string) => {
        if (events.has(name)) return;
        events.add(name);
        conn.on(name, sink(name));
      };
      // Pre-register every event admin cares about so late subscribers don't
      // miss the first payload.
      for (const event of ["NotificationCreated"]) {
        wire(event);
      }
    };

    const connect = async () => {
      if (stoppedRef.current) return;
      if (!tokenStore.getAccessToken()) {
        setStatus("idle");
        return;
      }
      setStatus("connecting");
      const conn = await buildConnection();
      // Torn down while the SignalR chunk was loading: don't open a socket we'd have to stop.
      if (stoppedRef.current) return;
      connectionRef.current = conn;
      wireListeners(conn);

      conn.onreconnecting(() => setStatus("reconnecting"));
      conn.onreconnected(() => setStatus("connected"));
      conn.onclose(async () => {
        if (stoppedRef.current) return;
        setStatus("error");
        await new Promise((r) => setTimeout(r, 5_000));
        if (!stoppedRef.current) void connect();
      });

      try {
        await conn.start();
        setStatus("connected");
      } catch {
        setStatus("error");
        await new Promise((r) => setTimeout(r, 5_000));
        if (!stoppedRef.current) void connect();
      }
    };

    void connect();

    return () => {
      stoppedRef.current = true;
      const conn = connectionRef.current;
      connectionRef.current = null;
      // HubConnectionState is a string enum; comparing the literal avoids importing the
      // lazy-loaded module here.
      if (conn && (conn.state as string) !== "Disconnected") {
        void conn.stop();
      }
      setStatus("idle");
    };
  }, [tokenEpoch]);

  const on = useCallback(<T,>(event: string, handler: (payload: T) => void) => {
    let bucket = listenersRef.current.get(event);
    if (!bucket) {
      bucket = new Set();
      listenersRef.current.set(event, bucket);
    }
    const cast = handler as unknown as Listener;
    bucket.add(cast);
    return () => {
      bucket?.delete(cast);
    };
  }, []);

  const value = useMemo<RealtimeContextValue>(() => ({ status, on }), [status, on]);

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime() {
  const ctx = useContext(RealtimeContext);
  if (!ctx) throw new Error("useRealtime must be used within RealtimeProvider");
  return ctx;
}

/**
 * Typed subscription hook. The handler is automatically re-bound when it changes
 * via a ref so callers don't have to memoize.
 */
export function useRealtimeEvent<T = unknown>(
  event: string,
  handler: (payload: T) => void,
  deps: ReadonlyArray<unknown> = [],
) {
  const { on } = useRealtime();
  const handlerRef = useRef(handler);
  useEffect(() => {
    handlerRef.current = handler;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [handler, ...deps]);

  useEffect(() => {
    const off = on<T>(event, (payload) => handlerRef.current(payload));
    return off;
  }, [event, on]);
}
