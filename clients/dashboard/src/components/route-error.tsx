import { isRouteErrorResponse, useNavigate, useRouteError } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

/**
 * Route-level error element. React Router v7 passes any error thrown during rendering,
 * loading, or an action to the nearest <Route errorElement={...}>. Without one, a render
 * error white-screens the whole app — this replaces that with a recoverable view.
 *
 * Stack traces and raw error payloads are only rendered in DEV builds. Production
 * users see a recoverable view with the high-level error and Reload/Go home actions;
 * exposing the call stack to end users gives nothing actionable and leaks internals.
 */
export function RouteError() {
  const error = useRouteError();
  const navigate = useNavigate();

  const { title, detail } = describe(error);
  const showDetail = import.meta.env.DEV && detail;

  return (
    <div className="flex min-h-[60vh] items-center justify-center p-6">
      <Card className="w-full max-w-lg">
        <CardHeader>
          <CardTitle>Something went wrong</CardTitle>
          <CardDescription>{title}</CardDescription>
        </CardHeader>
        {showDetail && (
          <CardContent>
            <pre className="max-h-60 overflow-auto rounded-md bg-muted p-3 text-xs">
              {detail}
            </pre>
          </CardContent>
        )}
        <CardFooter className="gap-2">
          <Button onClick={() => navigate(0)}>Reload</Button>
          <Button variant="outline" onClick={() => navigate("/")}>Go home</Button>
        </CardFooter>
      </Card>
    </div>
  );
}

function describe(error: unknown): { title: string; detail?: string } {
  if (isRouteErrorResponse(error)) {
    return {
      title: `${error.status} ${error.statusText}`,
      detail: typeof error.data === "string" ? error.data : JSON.stringify(error.data, null, 2),
    };
  }
  if (error instanceof Error) {
    return { title: error.message, detail: error.stack };
  }
  return { title: "Unexpected error", detail: String(error) };
}
