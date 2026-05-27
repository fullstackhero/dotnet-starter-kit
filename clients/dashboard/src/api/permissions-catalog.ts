// Permission editor helpers. The catalog itself is fetched from
// GET /api/v1/identity/permissions/catalog (see `getPermissionsCatalog` in
// `./identity.ts`) so every module's permissions land in the editor
// automatically — there's no static mirror to keep in sync any more.

import type { PermissionCatalogEntryDto } from "@/api/identity";

/**
 * Editor-facing alias. The DTO already carries everything the role-detail
 * editor needs; we keep `PermissionDescriptor` as the in-app type name so
 * existing call sites read naturally ("a descriptor for one permission").
 */
export type PermissionDescriptor = PermissionCatalogEntryDto;

export type PermissionGroup = {
  resource: string;
  permissions: PermissionDescriptor[];
};

/**
 * Buckets a flat catalog into groups keyed by resource, preserving the
 * order the API returned (which the server emits in registration order:
 * Identity first, then each module in startup order, then SystemPermissions
 * at the end). That keeps related actions next to each other in the UI.
 */
export function groupPermissions(perms: PermissionDescriptor[]): PermissionGroup[] {
  const order: string[] = [];
  const map = new Map<string, PermissionDescriptor[]>();
  for (const p of perms) {
    const bucket = map.get(p.resource);
    if (bucket) {
      bucket.push(p);
    } else {
      map.set(p.resource, [p]);
      order.push(p.resource);
    }
  }
  return order.map((resource) => ({ resource, permissions: map.get(resource)! }));
}
