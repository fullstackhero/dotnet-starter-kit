/**
 * Permission required to view each Recycle bin tab. Each value mirrors the
 * permission its matching trash endpoint enforces server-side (the catalog
 * trash lists require the resource's `Restore` permission; Files trash requires
 * `Files.ViewTrash`). Mirrored here so the dashboard can hide tabs — and the
 * Trash nav entry itself — that a user can't access, instead of letting them
 * click into a guaranteed 403. The server keeps enforcing as defence-in-depth.
 *
 * Convention follows the server registries: `Permissions.{Resource}.{Action}`.
 * If a trash endpoint's permission changes, mirror it here.
 */
export const TRASH_TAB_PERMISSIONS = {
  products: "Permissions.Catalog.Products.Restore",
  brands: "Permissions.Catalog.Brands.Restore",
  categories: "Permissions.Catalog.Categories.Restore",
  tickets: "Permissions.Tickets.Restore",
  files: "Permissions.Files.ViewTrash",
} as const;

export type TrashTabKey = keyof typeof TRASH_TAB_PERMISSIONS;

/** Flat list of every trash permission — used to gate the Trash nav entry
 *  (visible if the user holds *any* of them). */
export const ALL_TRASH_PERMISSIONS: readonly string[] = Object.values(TRASH_TAB_PERMISSIONS);
