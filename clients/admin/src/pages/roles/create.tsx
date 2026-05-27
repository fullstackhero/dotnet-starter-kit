import { Navigate } from "react-router-dom";

// Role creation is now a dialog launched from the list page.
// This page is kept (and the route lazily loads it) so that any bookmarked
// /roles/new links continue to work — they redirect seamlessly to /roles.
export function CreateRolePage() {
  return <Navigate to="/roles" replace />;
}
