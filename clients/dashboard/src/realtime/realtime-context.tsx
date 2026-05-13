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
  /** Invoke a hub method (e.g. `Typing(channelId)`). Fire-and-forget. */
  invoke: (method: string, ...args: unknown[]) => Promise<void>;
};

const RealtimeContext = createContext<RealtimeContextValue | null>(null);

/**
 * Opens a single shared SignalR connection to /api/v1/realtime/hub authenticated via
 * ?access_token=. Reconnect with backoff [2s, 5s, 10s, 30s]; on a 401 we let the token-
 * refresh flow in apiFetch kick in next time the user touches the API and rebuild the
 * connection lazily. Components subscribe via the `on(event, handler)` hook.
 *
 * Transport is auto-selected by SignalR (WebSockets, then SSE, then long-polling) — we
 * don't pin one because dashboards behind corporate proxies often need the fallbacks.
 */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<RealtimeStatus>("idle");
  const connectionRef = useRef<HubConnection | null>(null);
  const listenersRef = useRef<Map<string, Set<Listener>>>(new Map());
  const stoppedRef = useRef(false);
  // Re-subscribed effect dep — bumped by tokenStore changes so that login,
  // refresh-token rotation, and impersonation switches all force a rebuild.
  const [tokenEpoch, setTokenEpoch] = useState(0);

  // Re-subscribe to tokenStore once on mount. Any change (set/clear/refresh
  // rotation) bumps the epoch which re-runs the connect effect below. Without
  // this the provider mounts once, snapshots whatever state existed at mount,
  // and never rebuilds — so a logout-then-login (or a token refresh) leaves
  // the hub running on a stale token (or worse, never connected at all if
  // mount happened before the token landed in storage).
  useEffect(() => tokenStore.subscribe(() => setTokenEpoch((n) => n + 1)), []);

  useEffect(() => {
    stoppedRef.current = false;
    const backoffs = [0, 2_000, 5_000, 10_000, 30_000];

    const buildConnection = () => {
      const url = `${env.apiBase || ""}/api/v1/realtime/hub`;
      return new HubConnectionBuilder()
        .withUrl(url, {
          // Returning null here tells SignalR's AccessTokenHttpClient to skip
          // adding the Authorization header rather than adding `Bearer ""` —
          // and gets a clear console warning when it happens so the failure
          // mode (provider running while signed out) is visible.
          accessTokenFactory: () => {
            const token = tokenStore.getAccessToken();
            if (!token) {
              console.warn(
                "[realtime] accessTokenFactory called with no stored token — SignalR negotiate will 401. " +
                  "This usually means the provider is alive while the user is signed out.",
              );
              return "";
            }
            return token;
          },
          transport:
            HttpTransportType.WebSockets |
            HttpTransportType.ServerSentEvents |
            HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect(backoffs.slice(1))
        .configureLogging(LogLevel.Warning)
        .build();
    };

    const wireListeners = (conn: HubConnection) => {
      // Single dispatch entry per event name — multiple subscribers route through
      // the Set we maintain ourselves. SignalR's `.on()` doesn't dedupe by name so
      // we keep a single sink and fan out client-side. This also lets new
      // components subscribe after the connection is up without re-binding the
      // hub method.
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
      // Pre-register every known server event so subscribers added before any
      // payload arrives don't miss the first message.
      for (const event of [
        "ChatMessageCreated",
        "ChatMessageEdited",
        "ChatMessageDeleted",
        "ChatChannelMemberAdded",
        "ChatChannelMemberRemoved",
        "ChatChannelAdded",
        "ChatChannelRemoved",
        "ChatChannelRead",
        "ChatReactionChanged",
        "ChatTypingStarted",
        "NotificationCreated",
      ]) {
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
        // SignalR's automatic-reconnect gives up after the backoff array runs out.
        // Wait a bit and rebuild — handles token rotation cleanly because the
        // accessTokenFactory will pick up the new token from the store.
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
    // tokenEpoch is the only intentional re-run trigger — see the subscribe
    // effect above. Re-running on epoch change tears down the old connection
    // and rebuilds with whatever the factory now returns from storage.
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

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    const conn = connectionRef.current;
    if (!conn || conn.state !== HubConnectionState.Connected) return;
    try {
      await conn.invoke(method, ...args);
    } catch {
      // Fire-and-forget — typing indicators and the like shouldn't disrupt UI on failure.
    }
  }, []);

  const value = useMemo<RealtimeContextValue>(
    () => ({ status, on, invoke }),
    [status, on, invoke],
  );

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
  // Keep the latest handler in a ref so subscribers don't re-bind on every render.
  useEffect(() => {
    handlerRef.current = handler;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [handler, ...deps]);

  useEffect(() => {
    const off = on<T>(event, (payload) => handlerRef.current(payload));
    return off;
  }, [event, on]);
}
