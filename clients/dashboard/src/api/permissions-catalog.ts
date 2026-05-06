// Mirror of FSH.Modules.Identity.Contracts.Authorization.IdentityPermissions.All
// Keep in sync when permissions are added/removed server-side.

export type PermissionDescriptor = {
  name: string; // Permissions.{Resource}.{Action}
  description: string;
  resource: string;
  action: string;
  isBasic?: boolean;
  isRoot?: boolean;
};

const def = (
  description: string,
  action: string,
  resource: string,
  flags: { isBasic?: boolean; isRoot?: boolean } = {},
): PermissionDescriptor => ({
  name: `Permissions.${resource}.${action}`,
  description,
  resource,
  action,
  isBasic: flags.isBasic,
  isRoot: flags.isRoot,
});

export const IDENTITY_PERMISSIONS: PermissionDescriptor[] = [
  // Users
  def("View Users", "View", "Users", { isBasic: true }),
  def("Search Users", "Search", "Users"),
  def("Create Users", "Create", "Users"),
  def("Update Users", "Update", "Users"),
  def("Delete Users", "Delete", "Users"),
  def("Export Users", "Export", "Users"),
  def("Manage User Roles", "ManageRoles", "Users"),
  def("Impersonate User", "Impersonate", "Users"),

  // UserRoles
  def("View User Roles", "View", "UserRoles", { isBasic: true }),
  def("Update User Roles", "Update", "UserRoles"),

  // Roles
  def("View Roles", "View", "Roles", { isBasic: true }),
  def("Create Roles", "Create", "Roles"),
  def("Update Roles", "Update", "Roles"),
  def("Delete Roles", "Delete", "Roles"),

  // RoleClaims
  def("View Role Claims", "View", "RoleClaims", { isBasic: true }),
  def("Update Role Claims", "Update", "RoleClaims"),

  // Sessions
  def("View My Sessions", "View", "Sessions", { isBasic: true }),
  def("Revoke My Sessions", "Revoke", "Sessions", { isBasic: true }),
  def("View All Sessions", "ViewAll", "Sessions"),
  def("Revoke Any Session", "RevokeAll", "Sessions"),

  // Groups
  def("View Groups", "View", "Groups", { isBasic: true }),
  def("Create Groups", "Create", "Groups"),
  def("Update Groups", "Update", "Groups"),
  def("Delete Groups", "Delete", "Groups"),
  def("Manage Group Members", "ManageMembers", "Groups"),
];

export type PermissionGroup = {
  resource: string;
  permissions: PermissionDescriptor[];
};

export function groupPermissions(perms: PermissionDescriptor[]): PermissionGroup[] {
  const map = new Map<string, PermissionDescriptor[]>();
  for (const p of perms) {
    const list = map.get(p.resource) ?? [];
    list.push(p);
    map.set(p.resource, list);
  }
  return Array.from(map.entries()).map(([resource, permissions]) => ({ resource, permissions }));
}

export const PERMISSION_GROUPS: PermissionGroup[] = groupPermissions(IDENTITY_PERMISSIONS);
