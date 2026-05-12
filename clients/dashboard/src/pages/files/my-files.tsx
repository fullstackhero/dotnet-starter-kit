import { useQuery, useQueryClient } from "@tanstack/react-query";
import { FolderOpen } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { listMyFiles, Visibility } from "@/api/files";
import { FileDropzone } from "@/components/file/file-dropzone";
import { FileGallery } from "@/components/file/file-gallery";
import { EmptyState, ErrorBand, PageHero } from "@/components/list";

const QUERY_KEY = ["files", "mine"] as const;

// Mirrors Files:Categories in appsettings.json. Each entry binds an allowed extension to the
// server-side category key that controls extension/size validation. The dropzone accepts the
// union of all extensions; the upload hook picks the category per-file from this map.
//
// Keep in sync manually with appsettings — the dashboard doesn't codegen against server config.
const EXTENSION_TO_CATEGORY: Record<string, string> = {
  // Image
  ".jpg": "Image",
  ".jpeg": "Image",
  ".png": "Image",
  ".webp": "Image",
  ".gif": "Image",
  ".ico": "Image",
  // Document
  ".pdf": "Document",
  ".docx": "Document",
  ".xlsx": "Document",
  ".pptx": "Document",
  ".txt": "Document",
  ".csv": "Document",
  // Archive
  ".zip": "Archive",
};

const ALL_EXTENSIONS = Object.keys(EXTENSION_TO_CATEGORY);

// Permissive client-side max (50 MB = Archive cap). Server enforces tighter per-category limits.
const CLIENT_MAX_BYTES = 50 * 1024 * 1024;

function categoryFor(file: File): string {
  const dot = file.name.lastIndexOf(".");
  const ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
  // Fallback to Document so the server rejects with a useful 400 rather than us silently
  // sending an unknown category — the server already gives a clean "Unknown category" 400.
  return EXTENSION_TO_CATEGORY[ext] ?? "Document";
}

export function MyFilesPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: QUERY_KEY,
    queryFn: () => listMyFiles(1, 50),
  });

  const onUploaded = () => {
    void queryClient.invalidateQueries({ queryKey: QUERY_KEY });
  };

  return (
    <div className="space-y-6 pb-12">
      <PageHero
        eyebrow="My Files"
        tenant={user?.tenant ?? "—"}
        title="Your files"
        subtitle="Drop images, documents, or archives. The server picks the right category from the file extension; uploads are private to you by default."
      />

      <FileDropzone
        options={{
          ownerType: "MyFiles",
          ownerId: null,
          category: categoryFor,
          visibility: Visibility.Private,
          allowedExtensions: ALL_EXTENSIONS,
          maxBytes: CLIENT_MAX_BYTES,
        }}
        accept={ALL_EXTENSIONS.join(",")}
        onUploaded={onUploaded}
      />

      {isError ? (
        <ErrorBand message={error instanceof Error ? error.message : "Couldn't load your files."} />
      ) : (data && data.length === 0) ? (
        <EmptyState
          eyebrow="My Files"
          headline="No files yet."
          body="Drop a file above to get started. Your uploads are private by default."
          icon={<FolderOpen className="h-5 w-5 text-[var(--color-primary)]" />}
          primaryAction={{ label: "Refresh", onClick: () => void refetch() }}
        />
      ) : (
        <FileGallery files={data} isLoading={isLoading} queryKey={QUERY_KEY} groupByKind />
      )}
    </div>
  );
}
