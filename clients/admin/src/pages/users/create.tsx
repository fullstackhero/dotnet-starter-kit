import { Navigate } from "react-router-dom";

// User creation is now a dialog launched from the list page.
// This page is kept (and the route lazily loads it) so that any bookmarked
// /users/new links continue to work — they redirect seamlessly to /users.
export function CreateUserPage() {
  return <Navigate to="/users" replace />;
}
