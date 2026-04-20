import { createBrowserRouter } from "react-router-dom";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/auth/protected-route";
import { LoginPage } from "@/pages/login";
import { OverviewPage } from "@/pages/overview";
import { ActivityPage } from "@/pages/activity";
import { InvoicesPage } from "@/pages/invoices";
import { NotFoundPage } from "@/pages/not-found";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
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
