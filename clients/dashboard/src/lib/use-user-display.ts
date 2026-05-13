import { useQuery } from "@tanstack/react-query";
import { getUserById, searchUsers, type UserDto } from "@/api/identity";

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

export type UserByUsername = {
  resolved: UserDto | null;
  loading: boolean;
  error: boolean;
};

/**
 * Resolves an @username (case-insensitive) to a full UserDto via
 * /api/v1/identity/users/search. Used by the mention profile peek so
 * the popover can show the user's avatar / email and open a DM.
 *
 * Cached separately from useUserDisplay (which is keyed by userId).
 * Two consumers of the same @handle on the same page share one fetch.
 *
 * `enabled` is controlled by the caller — passing `false` keeps the
 * query dormant until the popover actually opens.
 */
export function useUserByUsername(
  username: string | null | undefined,
  enabled: boolean,
): UserByUsername {
  const normalized = (username ?? "").trim().toLowerCase();

  const query = useQuery({
    queryKey: ["identity", "by-username", normalized],
    queryFn: async () => {
      const page = await searchUsers({ search: normalized, pageSize: 5, isActive: true });
      const items: UserDto[] = page.items ?? [];
      return items.find((u) => (u.userName ?? "").toLowerCase() === normalized) ?? null;
    },
    enabled: enabled && normalized.length > 0,
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
    retry: 1,
  });

  return {
    resolved: query.data ?? null,
    loading: query.isPending && enabled && normalized.length > 0,
    error: query.isError,
  };
}
