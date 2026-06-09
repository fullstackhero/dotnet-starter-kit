import { lazy, Suspense, type ComponentType } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteError } from "@/components/route-error";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/cn";

// ─────────────────────────────────────────────────────────────────────────
// Lazy route boundaries
//
// Each route is split into its own bundle so first paint after login only
// loads the chunk for the landing page (Overview). Catalog, Settings,
// Login, Activity, etc. fetch on demand.
//
// `lazyNamed` wraps a dynamic import that exports a *named* component
// (we don't use default exports anywhere in this codebase). Returns a
// shape compatible with React.lazy.
// ─────────────────────────────────────────────────────────────────────────

function lazyNamed<T extends Record<string, unknown>, K extends keyof T>(
  importer: () => Promise<T>,
  name: K,
) {
  return lazy(async () => {
    const mod = await importer();
    return { default: mod[name] as ComponentType<unknown> };
  });
}

const LoginPage = lazyNamed(() => import("@/pages/login"), "LoginPage");
const ForgotPasswordPage = lazyNamed(
  () => import("@/pages/auth/forgot-password"),
  "ForgotPasswordPage",
);
const ResetPasswordPage = lazyNamed(
  () => import("@/pages/auth/reset-password"),
  "ResetPasswordPage",
);
const ConfirmEmailPage = lazyNamed(
  () => import("@/pages/auth/confirm-email"),
  "ConfirmEmailPage",
);
const OverviewPage = lazyNamed(() => import("@/pages/overview"), "OverviewPage");
const ActivityPage = lazyNamed(() => import("@/pages/activity"), "ActivityPage");
const InvoicesPage = lazyNamed(() => import("@/pages/invoices"), "InvoicesPage");
const InvoiceDetailPage = lazyNamed(
  () => import("@/pages/invoice-detail"),
  "InvoiceDetailPage",
);
const SubscriptionPage = lazyNamed(
  () => import("@/pages/subscription"),
  "SubscriptionPage",
);
const BrandsPage = lazyNamed(() => import("@/pages/catalog/brands"), "BrandsPage");
const CategoriesPage = lazyNamed(() => import("@/pages/catalog/categories"), "CategoriesPage");
const ProductsPage = lazyNamed(() => import("@/pages/catalog/products"), "ProductsPage");
const ProductDetailPage = lazyNamed(
  () => import("@/pages/catalog/product-detail"),
  "ProductDetailPage",
);
const NotFoundPage = lazyNamed(() => import("@/pages/not-found"), "NotFoundPage");
const TenantDeactivatedPage = lazyNamed(
  () => import("@/pages/tenant-deactivated"),
  "TenantDeactivatedPage",
);
const ImpersonationEndedPage = lazyNamed(
  () => import("@/pages/impersonation-ended"),
  "ImpersonationEndedPage",
);
const SettingsLayout = lazyNamed(
  () => import("@/pages/settings/settings-layout"),
  "SettingsLayout",
);
const ProfileSettings = lazyNamed(() => import("@/pages/settings/profile"), "ProfileSettings");
const SecuritySettings = lazyNamed(() => import("@/pages/settings/security"), "SecuritySettings");
const AppearanceSettings = lazyNamed(
  () => import("@/pages/settings/appearance"),
  "AppearanceSettings",
);
const NotificationsSettings = lazyNamed(
  () => import("@/pages/settings/notifications"),
  "NotificationsSettings",
);
const ApiKeysSettings = lazyNamed(() => import("@/pages/settings/api-keys"), "ApiKeysSettings");
const HealthPage = lazyNamed(() => import("@/pages/health"), "HealthPage");
const AuditsPage = lazyNamed(() => import("@/pages/audits"), "AuditsPage");
const TicketsPage = lazyNamed(() => import("@/pages/tickets/tickets"), "TicketsPage");
const TicketDetailPage = lazyNamed(
  () => import("@/pages/tickets/ticket-detail"),
  "TicketDetailPage",
);
const TrashPage = lazyNamed(() => import("@/pages/system/trash"), "TrashPage");
const SessionsPage = lazyNamed(() => import("@/pages/system/sessions"), "SessionsPage");
const UsersPage = lazyNamed(() => import("@/pages/identity/users"), "UsersPage");
const UserDetailPage = lazyNamed(
  () => import("@/pages/identity/user-detail"),
  "UserDetailPage",
);
const RolesPage = lazyNamed(() => import("@/pages/identity/roles"), "RolesPage");
const RoleDetailPage = lazyNamed(
  () => import("@/pages/identity/role-detail"),
  "RoleDetailPage",
);
const GroupsPage = lazyNamed(() => import("@/pages/identity/groups"), "GroupsPage");
const GroupDetailPage = lazyNamed(
  () => import("@/pages/identity/group-detail"),
  "GroupDetailPage",
);
const MyFilesPage = lazyNamed(() => import("@/pages/files/my-files"), "MyFilesPage");
const ChatPage = lazyNamed(() => import("@/pages/chat/chat-page"), "ChatPage");

/**
 * RouteFallback — what shows while a lazy chunk is downloading. Mirrors
 * the resolved page's shape: a thin header line, an atmospheric hero
 * placeholder, and three card placeholders. Uses the modernized
 * `.skeleton` shimmer so it feels like part of the same surface family.
 */
function RouteFallback() {
  return (
    <div className={cn("space-y-6 fsh-enter")} role="status" aria-busy="true">
      <span className="sr-only">Loading…</span>
      <div className="space-y-2">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-3 w-96 max-w-full" />
      </div>
      <Skeleton className="h-32 w-full rounded-[20px]" />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <Skeleton className="h-24 rounded-xl" />
        <Skeleton className="h-24 rounded-xl" />
        <Skeleton className="h-24 rounded-xl" />
      </div>
    </div>
  );
}

/**
 * Wrap a lazy route element in Suspense so each route boundary has its
 * own loading state. Returns a fresh Suspense per call — important so
 * that two routes sharing the same fallback don't share the same
 * suspended chunk's resolution timing.
 */
function withSuspense(node: React.ReactNode) {
  return <Suspense fallback={<RouteFallback />}>{node}</Suspense>;
}

export const router = createBrowserRouter([
  {
    path: "/login",
    element: withSuspense(<LoginPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/forgot-password",
    element: withSuspense(<ForgotPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/reset-password",
    element: withSuspense(<ResetPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: "/confirm-email",
    element: withSuspense(<ConfirmEmailPage />),
    errorElement: <RouteError />,
  },
  {
    // Terminal state when the signed-in user's tenant is deactivated mid-session.
    // Top-level (outside ProtectedRoute/AppShell) — the token is still valid but
    // every request 403s, so there is no shell to render. query-client.ts routes
    // here on detecting the deactivated-tenant 403.
    path: "/tenant-deactivated",
    element: withSuspense(<TenantDeactivatedPage />),
    errorElement: <RouteError />,
  },
  {
    // Terminal state when an operator's impersonation grant is revoked (or its
    // short-lived token expires) mid-session. Top-level (outside
    // ProtectedRoute/AppShell) — the token still decodes but every request
    // 401s, so there is no shell to render. query-client.ts routes here on
    // detecting the impersonation-revoked 401.
    path: "/impersonation-ended",
    element: withSuspense(<ImpersonationEndedPage />),
    errorElement: <RouteError />,
  },
  {
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        element: <AppShell />,
        errorElement: <RouteError />,
        children: [
          { index: true, element: withSuspense(<OverviewPage />) },
          { path: "activity", element: withSuspense(<ActivityPage />) },
          { path: "subscription", element: withSuspense(<SubscriptionPage />) },
          { path: "invoices", element: withSuspense(<InvoicesPage />) },
          { path: "invoices/:id", element: withSuspense(<InvoiceDetailPage />) },
          { path: "system/health", element: withSuspense(<HealthPage />) },
          { path: "system/audits", element: withSuspense(<AuditsPage />) },
          { path: "system/trash", element: withSuspense(<TrashPage />) },
          { path: "system/sessions", element: withSuspense(<SessionsPage />) },
          { path: "files", element: withSuspense(<MyFilesPage />) },
          { path: "chat", element: withSuspense(<ChatPage />) },
          { path: "chat/:channelId", element: withSuspense(<ChatPage />) },
          { path: "tickets", element: withSuspense(<TicketsPage />) },
          { path: "tickets/:ticketId", element: withSuspense(<TicketDetailPage />) },
          { path: "identity", element: <Navigate to="/identity/users" replace /> },
          { path: "identity/users", element: withSuspense(<UsersPage />) },
          { path: "identity/users/:userId", element: withSuspense(<UserDetailPage />) },
          { path: "identity/roles", element: withSuspense(<RolesPage />) },
          { path: "identity/roles/:roleId", element: withSuspense(<RoleDetailPage />) },
          { path: "identity/groups", element: withSuspense(<GroupsPage />) },
          { path: "identity/groups/:groupId", element: withSuspense(<GroupDetailPage />) },
          { path: "catalog", element: <Navigate to="/catalog/brands" replace /> },
          { path: "catalog/brands", element: withSuspense(<BrandsPage />) },
          { path: "catalog/categories", element: withSuspense(<CategoriesPage />) },
          { path: "catalog/products", element: withSuspense(<ProductsPage />) },
          {
            path: "catalog/products/:productId",
            element: withSuspense(<ProductDetailPage />),
          },
          {
            path: "settings",
            element: withSuspense(<SettingsLayout />),
            children: [
              { index: true, element: <Navigate to="profile" replace /> },
              { path: "profile", element: withSuspense(<ProfileSettings />) },
              { path: "security", element: withSuspense(<SecuritySettings />) },
              { path: "appearance", element: withSuspense(<AppearanceSettings />) },
              { path: "notifications", element: withSuspense(<NotificationsSettings />) },
              { path: "api-keys", element: withSuspense(<ApiKeysSettings />) },
            ],
          },
        ],
      },
    ],
  },
  { path: "*", element: withSuspense(<NotFoundPage />) },
]);
