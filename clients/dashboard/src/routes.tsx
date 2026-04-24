import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { RouteError } from "@/components/route-error";
import { LoginPage } from "@/pages/login";
import { OverviewPage } from "@/pages/overview";
import { ActivityPage } from "@/pages/activity";
import { InvoicesPage } from "@/pages/invoices";
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
          { index: true, element: <OverviewPage /> },
          { path: "activity", element: <ActivityPage /> },
          { path: "invoices", element: <InvoicesPage /> },
        ],
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
