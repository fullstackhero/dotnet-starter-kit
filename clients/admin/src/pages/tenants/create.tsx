/**
 * /tenants/new — redirects to the tenant list.
 *
 * Tenant creation is now a dialog launched from the list page "New tenant"
 * button. This redirect keeps any existing bookmarks or links working.
 */
import { Navigate } from "react-router-dom";

export function CreateTenantPage() {
  return <Navigate to="/tenants" replace />;
}
