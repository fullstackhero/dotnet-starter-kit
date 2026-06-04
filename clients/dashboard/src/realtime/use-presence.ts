import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getPresence } from "@/api/chat";
import { useRealtimeEvent } from "@/realtime/realtime-context";

/**
 * Resolves a user id to a live online/offline boolean.
 *
 * Mechanics:
 *  1. On mount, a useQuery polls /realtime/presence?userIds={id} every 60s
 *     so a tab opened in the background eventually catches up even if the
 *     SignalR connection dropped while it was hidden. TanStack dedupes
 *     across multiple mounts of the same userId.
 *  2. The hook subscribes to PresenceChanged on the realtime hub and
 *     patches the cache whenever a transition for this userId arrives,
 *     so the dot flips instantly in the typical case.
 *
 * Returns `false` when the userId is unknown / unresolved.
 */
export function usePresence(userId: string | null | undefined): boolean {
  const queryClient = useQueryClient();
  const key = ["presence", userId ?? ""] as const;

  const query = useQuery({
    queryKey: key,
    queryFn: async () => {
      const rows = await getPresence([userId!]);
      return rows.find((r) => r.userId === userId)?.online ?? false;
    },
    enabled: !!userId,
    staleTime: 30_000,
    refetchInterval: 60_000,
    refetchIntervalInBackground: false,
  });

  useRealtimeEvent<{ userId: string; online: boolean }>(
    "PresenceChanged",
    (payload) => {
      if (!userId || payload.userId !== userId) return;
      queryClient.setQueryData<boolean>(key, payload.online);
    },
    [userId, queryClient],
  );

  return userId ? (query.data ?? false) : false;
}
