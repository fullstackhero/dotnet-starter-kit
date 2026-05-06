import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteError } from "@/components/route-error";
import { LoginPage } from "@/pages/login";
import { DashboardPage } from "@/pages/dashboard";
import { TenantsListPage } from "@/pages/tenants/list";
import { CreateTenantPage } from "@/pages/tenants/create";
import { TenantDetailPage } from "@/pages/tenants/detail";
import { UsersListPage } from "@/pages/users/list";
import { CreateUserPage } from "@/pages/users/create";
import { UserDetailPage } from "@/pages/users/detail";
import { NotFoundPage } from "@/pages/not-found";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage />, errorElement: <RouteError /> },
  {
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    children: [
      {
        element: <AppShell />,
        errorElement: <RouteError />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: "tenants", element: <TenantsListPage /> },
          { path: "tenants/new", element: <CreateTenantPage /> },
          { path: "tenants/:id", element: <TenantDetailPage /> },
          { path: "users", element: <UsersListPage /> },
          { path: "users/new", element: <CreateUserPage /> },
          { path: "users/:id", element: <UserDetailPage /> },
          { path: "billing", element: <Navigate to="/" replace /> },
          { path: "quota", element: <Navigate to="/" replace /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
