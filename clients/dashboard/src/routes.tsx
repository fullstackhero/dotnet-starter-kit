import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteError } from "@/components/route-error";
import { LoginPage } from "@/pages/login";
import { OverviewPage } from "@/pages/overview";
import { ActivityPage } from "@/pages/activity";
import { InvoicesPage } from "@/pages/invoices";
import { BrandsPage } from "@/pages/catalog/brands";
import { CategoriesPage } from "@/pages/catalog/categories";
import { ProductsPage } from "@/pages/catalog/products";
import { ProductDetailPage } from "@/pages/catalog/product-detail";
import { NotFoundPage } from "@/pages/not-found";
import { SettingsLayout } from "@/pages/settings/settings-layout";
import { ProfileSettings } from "@/pages/settings/profile";
import { SecuritySettings } from "@/pages/settings/security";
import { AppearanceSettings } from "@/pages/settings/appearance";
import { NotificationsSettings } from "@/pages/settings/notifications";
import { ApiKeysSettings } from "@/pages/settings/api-keys";

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
          { index: true, element: <OverviewPage /> },
          { path: "activity", element: <ActivityPage /> },
          { path: "invoices", element: <InvoicesPage /> },
          { path: "catalog", element: <Navigate to="/catalog/brands" replace /> },
          { path: "catalog/brands", element: <BrandsPage /> },
          { path: "catalog/categories", element: <CategoriesPage /> },
          { path: "catalog/products", element: <ProductsPage /> },
          { path: "catalog/products/:productId", element: <ProductDetailPage /> },
          {
            path: "settings",
            element: <SettingsLayout />,
            children: [
              { index: true, element: <Navigate to="profile" replace /> },
              { path: "profile", element: <ProfileSettings /> },
              { path: "security", element: <SecuritySettings /> },
              { path: "appearance", element: <AppearanceSettings /> },
              { path: "notifications", element: <NotificationsSettings /> },
              { path: "api-keys", element: <ApiKeysSettings /> },
            ],
          },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
