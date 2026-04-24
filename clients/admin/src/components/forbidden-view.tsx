import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

type ForbiddenViewProps = {
  /** Permission strings the caller required that the user doesn't hold. Shown for operator clarity. */
  missing?: string[];
};

export function ForbiddenView({ missing }: ForbiddenViewProps) {
  return (
    <div className="flex min-h-[60vh] items-center justify-center p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Access denied</CardTitle>
          <CardDescription>
            Your account doesn't have permission to view this area.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {missing && missing.length > 0 && (
            <div className="rounded-md bg-muted p-3 text-xs font-mono text-muted-foreground">
              Missing: {missing.join(", ")}
            </div>
          )}
          <Button asChild variant="outline" className="w-full">
            <Link to="/">Back to home</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
