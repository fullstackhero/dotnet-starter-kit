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
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";

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

    const buildConnection = () => {
      const url = `${env.apiBase || ""}/api/v1/realtime/hub`;
      return new HubConnectionBuilder()
        .withUrl(url, {
          accessTokenFactory: () => tokenStore.getAccessToken() ?? "",
          transport:
            HttpTransportType.WebSockets |
            HttpTransportType.ServerSentEvents |
            HttpTransportType.LongPolling,
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
        .configureLogging(LogLevel.Warning)
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
      const conn = buildConnection();
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
      if (conn && conn.state !== HubConnectionState.Disconnected) {
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
