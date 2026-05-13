import { useQuery } from "@tanstack/react-query";
import { getUserById } from "@/api/identity";

export type UserDisplay = {
  /** Best display name: "First Last" → username → email → shortened GUID. */
  name: string;
  /** Username for tooltip / mention copy. */
  handle?: string;
  /** Profile image URL when available. */
  imageUrl?: string;
  /** True while we wait for the first response. */
  loading: boolean;
};

function shortenGuid(userId: string): string {
  return /^[0-9a-f-]{32,}$/i.test(userId) ? userId.slice(0, 8) : userId;
}

/**
 * Resolves a userId to a display tuple via GET /api/v1/identity/users/{id}.
 * Cached in the TanStack Query cache for 5 min (stale) / 30 min (gc), so
 * multiple message rows from the same author share one network round-trip
 * and switching channels is instant.
 *
 * Future: when an /identity/users/by-ids batch endpoint lands, swap this for
 * a registry-style provider that batches per-tick. For now the dedupe inside
 * react-query is good enough — a channel with 100 messages from 10 unique
 * users does 10 parallel GETs, not 100.
 */
export function useUserDisplay(userId: string | null | undefined): UserDisplay {
  const query = useQuery({
    queryKey: ["identity", "user", userId],
    queryFn: () => getUserById(userId!),
    enabled: !!userId,
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
    retry: 1,
  });

  if (!userId) {
    return { name: "(unknown)", loading: false };
  }

  const u = query.data;
  if (!u) {
    // Fallback to short GUID while we load; after error, also stick with it.
    return { name: shortenGuid(userId), loading: query.isPending };
  }

  const fullName = [u.firstName, u.lastName].filter(Boolean).join(" ").trim();
  return {
    name: fullName || u.userName || u.email || shortenGuid(userId),
    handle: u.userName ?? undefined,
    imageUrl: u.imageUrl ?? undefined,
    loading: false,
  };
}
