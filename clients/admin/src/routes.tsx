import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { LoginPage } from "@/pages/login";
import { DashboardPage } from "@/pages/dashboard";
import { TenantsListPage } from "@/pages/tenants/list";
import { NotFoundPage } from "@/pages/not-found";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: "tenants", element: <TenantsListPage /> },
          { path: "billing", element: <Navigate to="/" replace /> },
          { path: "quota", element: <Navigate to="/" replace /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
