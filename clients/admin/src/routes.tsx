import { lazy } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteGuard } from "@/auth/route-guard";
import { RouteError } from "@/components/route-error";
import { LoginPage } from "@/pages/login";
import { DashboardPage } from "@/pages/dashboard";
import { NotFoundPage } from "@/pages/not-found";
import {
  AuditingPermissions,
  BillingPermissions,
  IdentityPermissions,
  MultitenancyPermissions,
  WebhooksPermissions,
} from "@/lib/permissions";

// Lazy-loaded pages — each `import()` becomes its own bundle chunk so the
// initial paint only ships the shell + dashboard. We re-export the named
// page component as the chunk's `default` via the wrapper-import trick.
const lazyNamed = <T extends string>(
  loader: () => Promise<Record<string, unknown>>,
  named: T,
) =>
  lazy(async () => {
    const mod = await loader();
    return { default: mod[named] as React.ComponentType };
  });

const TenantsListPage = lazyNamed(() => import("@/pages/tenants/list"), "TenantsListPage");
const TenantDetailPage = lazyNamed(() => import("@/pages/tenants/detail"), "TenantDetailPage");
const UsersListPage = lazyNamed(() => import("@/pages/users/list"), "UsersListPage");
const UserDetailPage = lazyNamed(() => import("@/pages/users/detail"), "UserDetailPage");
const RolesListPage = lazyNamed(() => import("@/pages/roles/list"), "RolesListPage");
const RoleDetailPage = lazyNamed(() => import("@/pages/roles/detail"), "RoleDetailPage");
const BillingLayout = lazyNamed(() => import("@/pages/billing/layout"), "BillingLayout");
const PlansListPage = lazyNamed(() => import("@/pages/billing/plans-list"), "PlansListPage");
const InvoicesListPage = lazyNamed(() => import("@/pages/billing/invoices-list"), "InvoicesListPage");
const InvoiceDetailPage = lazyNamed(() => import("@/pages/billing/invoice-detail"), "InvoiceDetailPage");
const AuditsListPage = lazyNamed(() => import("@/pages/audits/list"), "AuditsListPage");
const HealthPage = lazyNamed(() => import("@/pages/health/page"), "HealthPage");
const ImpersonationListPage = lazyNamed(() => import("@/pages/impersonation/list"), "ImpersonationListPage");
const WebhooksListPage = lazyNamed(() => import("@/pages/webhooks/list"), "WebhooksListPage");
const WebhookDetailPage = lazyNamed(() => import("@/pages/webhooks/detail"), "WebhookDetailPage");
const NotificationsInboxPage = lazyNamed(() => import("@/pages/notifications/inbox"), "NotificationsInboxPage");
const SettingsLayout = lazyNamed(() => import("@/pages/settings/layout"), "SettingsLayout");
const ProfileSettings = lazyNamed(() => import("@/pages/settings/profile"), "ProfileSettings");
const SecuritySettings = lazyNamed(() => import("@/pages/settings/security"), "SecuritySettings");
const SessionsSettings = lazyNamed(() => import("@/pages/settings/sessions"), "SessionsSettings");
const AppearanceSettings = lazyNamed(() => import("@/pages/settings/appearance"), "AppearanceSettings");
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

// Each route's element is wrapped in RouteGuard with the same permissions the
// server endpoint requires, so the UI mirrors server-side authorization. Auth
// itself is enforced one layer up by <ProtectedRoute />.

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage />, errorElement: <RouteError /> },
  { path: "/forgot-password", element: <ForgotPasswordPage />, errorElement: <RouteError /> },
  { path: "/reset-password", element: <ResetPasswordPage />, errorElement: <RouteError /> },
  { path: "/confirm-email", element: <ConfirmEmailPage />, errorElement: <RouteError /> },
  {
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        element: <AppShell />,
        errorElement: <RouteError />,
        children: [
          { index: true, element: <DashboardPage /> },

          // Tenants — root-only
          {
            path: "tenants",
            element: (
              <RouteGuard perms={[MultitenancyPermissions.Tenants.View]}>
                <TenantsListPage />
              </RouteGuard>
            ),
          },
          {
            // /tenants/new — creation is now a dialog on the list page.
            // Redirect any bookmarked links back to /tenants.
            path: "tenants/new",
            element: <Navigate to="/tenants" replace />,
          },
          {
            path: "tenants/:id",
            element: (
              <RouteGuard perms={[MultitenancyPermissions.Tenants.View]}>
                <TenantDetailPage />
              </RouteGuard>
            ),
          },

          // Users
          {
            path: "users",
            element: (
              <RouteGuard perms={[IdentityPermissions.Users.View]}>
                <UsersListPage />
              </RouteGuard>
            ),
          },
          {
            // /users/new — creation is now a dialog on the list page.
            // Redirect any bookmarked links back to /users.
            path: "users/new",
            element: <Navigate to="/users" replace />,
          },
          {
            path: "users/:id",
            element: (
              <RouteGuard perms={[IdentityPermissions.Users.View]}>
                <UserDetailPage />
              </RouteGuard>
            ),
          },

          // Roles
          {
            path: "roles",
            element: (
              <RouteGuard perms={[IdentityPermissions.Roles.View]}>
                <RolesListPage />
              </RouteGuard>
            ),
          },
          {
            // /roles/new — creation is now a dialog on the list page.
            // Redirect any bookmarked links back to /roles.
            path: "roles/new",
            element: <Navigate to="/roles" replace />,
          },
          {
            path: "roles/:id",
            element: (
              <RouteGuard perms={[IdentityPermissions.Roles.View]}>
                <RoleDetailPage />
              </RouteGuard>
            ),
          },

          // Billing
          {
            path: "billing",
            element: (
              <RouteGuard perms={[BillingPermissions.View]}>
                <BillingLayout />
              </RouteGuard>
            ),
            children: [
              { index: true, element: <Navigate to="/billing/invoices" replace /> },
              { path: "plans", element: <PlansListPage /> },
              { path: "invoices", element: <InvoicesListPage /> },
              { path: "invoices/:invoiceId", element: <InvoiceDetailPage /> },
            ],
          },

          // Impersonation
          {
            path: "impersonation",
            element: (
              <RouteGuard perms={[IdentityPermissions.Impersonation.View]}>
                <ImpersonationListPage />
              </RouteGuard>
            ),
          },

          // Audits — detail opens as a side sheet on the list page.
          // Redirect any bookmarked /audits/:id links back to /audits.
          {
            path: "audits",
            element: (
              <RouteGuard perms={[AuditingPermissions.AuditTrails.View]}>
                <AuditsListPage />
              </RouteGuard>
            ),
          },
          {
            path: "audits/:id",
            element: <Navigate to="/audits" replace />,
          },

          // Webhooks — list/detail both read subscriptions, which the server
          // gates on Webhooks.View (granted to Basic by default).
          {
            path: "webhooks",
            element: (
              <RouteGuard perms={[WebhooksPermissions.Subscriptions.View]}>
                <WebhooksListPage />
              </RouteGuard>
            ),
          },
          {
            path: "webhooks/:id",
            element: (
              <RouteGuard perms={[WebhooksPermissions.Subscriptions.View]}>
                <WebhookDetailPage />
              </RouteGuard>
            ),
          },

          // Notifications inbox — available to every signed-in user
          { path: "notifications", element: <NotificationsInboxPage /> },

          // Health — public probes; signed-in users only see this from inside the app
          { path: "health", element: <HealthPage /> },

          // Settings — account-scoped; any signed-in user can manage their own profile + sessions + 2FA
          {
            path: "settings",
            element: <SettingsLayout />,
            children: [
              { index: true, element: <Navigate to="/settings/profile" replace /> },
              { path: "profile", element: <ProfileSettings /> },
              { path: "security", element: <SecuritySettings /> },
              { path: "sessions", element: <SessionsSettings /> },
              { path: "appearance", element: <AppearanceSettings /> },
            ],
          },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
