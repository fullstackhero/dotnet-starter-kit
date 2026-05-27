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
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";
import { issueSseToken } from "@/sse/sse-api";

export type SseStatus = "idle" | "connecting" | "connected" | "reconnecting" | "error";

export type SseEvent = {
  id: string;
  type: string;
  data: unknown;
  rawData: string;
  receivedAt: number;
};

// Two contexts — split so consumers that only watch status (the topbar
// SSE dot, the bell footer pill) don't re-render every time a new event
// lands. The previous single context's value identity changed on every
// event because `events` is a fresh array, which cascaded re-renders
// through the entire overview tree.
type SseStatusValue = {
  status: SseStatus;
  eventCount: number;
};

type SseEventsValue = {
  events: SseEvent[];
};

const SseStatusContext = createContext<SseStatusValue | null>(null);
const SseEventsContext = createContext<SseEventsValue | null>(null);

const MAX_EVENTS = 200;
const INITIAL_BACKOFF_MS = 1000;
const MAX_BACKOFF_MS = 30_000;

function randomId() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

function tryParseJson(raw: string): unknown {
  try {
    return JSON.parse(raw);
  } catch {
    return raw;
  }
}

async function* parseSseStream(
  reader: ReadableStreamDefaultReader<Uint8Array>,
): AsyncGenerator<{ type: string; data: string; id?: string }> {
  const decoder = new TextDecoder("utf-8");
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) return;
    buffer += decoder.decode(value, { stream: true });

    // SSE events are delimited by a blank line (\n\n).
    let delimiter: number;
    while ((delimiter = buffer.indexOf("\n\n")) !== -1) {
      const rawEvent = buffer.slice(0, delimiter);
      buffer = buffer.slice(delimiter + 2);

      // Skip comments (lines starting with ':') and empty blocks
      const lines = rawEvent.split("\n").filter((l) => l && !l.startsWith(":"));
      if (lines.length === 0) continue;

      let eventType = "message";
      let eventId: string | undefined;
      const dataLines: string[] = [];

      for (const line of lines) {
        const colonIdx = line.indexOf(":");
        const field = colonIdx === -1 ? line : line.slice(0, colonIdx);
        const value = colonIdx === -1 ? "" : line.slice(colonIdx + 1).replace(/^ /, "");
        if (field === "event") eventType = value;
        else if (field === "id") eventId = value;
        else if (field === "data") dataLines.push(value);
      }

      yield { type: eventType, data: dataLines.join("\n"), id: eventId };
    }
  }
}

export function SseProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SseStatus>("idle");
  const [events, setEvents] = useState<SseEvent[]>([]);
  const [eventCount, setEventCount] = useState(0);

  // We keep the running connection in refs so re-renders don't restart it.
  const abortRef = useRef<AbortController | null>(null);
  const stoppedRef = useRef(false);

  const appendEvent = useCallback((ev: { type: string; data: string; id?: string }) => {
    const entry: SseEvent = {
      id: ev.id ?? randomId(),
      type: ev.type,
      data: tryParseJson(ev.data),
      rawData: ev.data,
      receivedAt: Date.now(),
    };
    setEvents((prev) => {
      const next = [entry, ...prev];
      return next.length > MAX_EVENTS ? next.slice(0, MAX_EVENTS) : next;
    });
    setEventCount((c) => c + 1);
  }, []);

  useEffect(() => {
    stoppedRef.current = false;
    let backoff = INITIAL_BACKOFF_MS;

    const connect = async () => {
      // Token lifecycle: the SSE access token is short-lived and single-use — the server
      // only checks it at /sse/stream handshake time, NOT on every event. Once the stream
      // is open, its life depends on the transport (network / server), not the token.
      // When the server or network eventually closes the stream, control returns to this
      // loop and we call issueSseToken() again below, obtaining a fresh one. The user JWT
      // used by issueSseToken() refreshes itself on 401 via api-client. So token refresh
      // during long sessions is implicit in the reconnect path — no dedicated timer needed.
      while (!stoppedRef.current) {
        if (!tokenStore.getAccessToken()) {
          setStatus("idle");
          return;
        }

        setStatus((s) => (s === "idle" ? "connecting" : "reconnecting"));

        try {
          const { token } = await issueSseToken();
          const controller = new AbortController();
          abortRef.current = controller;

          const tenant = tokenStore.getTenant() ?? env.defaultTenant;
          const url = `${env.apiBase}/api/v1/sse/stream?token=${encodeURIComponent(token)}`;

          const response = await fetch(url, {
            method: "GET",
            headers: {
              Accept: "text/event-stream",
              ...(tenant ? { tenant } : {}),
            },
            signal: controller.signal,
          });

          if (!response.ok || !response.body) {
            throw new Error(`SSE stream refused: ${response.status}`);
          }

          setStatus("connected");
          backoff = INITIAL_BACKOFF_MS;

          const reader = response.body.getReader();
          for await (const ev of parseSseStream(reader)) {
            appendEvent(ev);
          }

          // Stream ended cleanly — loop will retry unless stopped.
        } catch (err) {
          if (stoppedRef.current) return;
          if (err instanceof DOMException && err.name === "AbortError") return;
          setStatus("error");
        }

        if (stoppedRef.current) return;
        setStatus("reconnecting");
        await new Promise((r) => setTimeout(r, backoff));
        backoff = Math.min(backoff * 2, MAX_BACKOFF_MS);
      }
    };

    void connect();

    return () => {
      stoppedRef.current = true;
      abortRef.current?.abort();
      setStatus("idle");
    };
  }, [appendEvent]);

  const statusValue = useMemo<SseStatusValue>(
    () => ({ status, eventCount }),
    [status, eventCount],
  );
  const eventsValue = useMemo<SseEventsValue>(() => ({ events }), [events]);

  return (
    <SseStatusContext.Provider value={statusValue}>
      <SseEventsContext.Provider value={eventsValue}>{children}</SseEventsContext.Provider>
    </SseStatusContext.Provider>
  );
}

/** Status + lifetime counter. Stable across event arrivals — use this in
 *  the topbar dot, status pills, badges. */
export function useSseStatus(): SseStatusValue {
  const ctx = useContext(SseStatusContext);
  if (!ctx) throw new Error("useSseStatus must be used within SseProvider");
  return ctx;
}

/** Full event stream. Mutates on every event — only mount in components
 *  that render the event list (overview live feed, activity page). */
export function useSseEvents(): SseEventsValue {
  const ctx = useContext(SseEventsContext);
  if (!ctx) throw new Error("useSseEvents must be used within SseProvider");
  return ctx;
}

/**
 * Backwards-compat composite hook. Returns status + events + counter
 * together — equivalent to the pre-split API. New consumers should
 * prefer `useSseStatus()` or `useSseEvents()` so they only subscribe to
 * the slice they actually need.
 */
export function useSse(): SseStatusValue & SseEventsValue {
  return { ...useSseStatus(), ...useSseEvents() };
}
