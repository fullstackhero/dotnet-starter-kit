import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { FolderOpen } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { listMyFiles, Visibility } from "@/api/files";
import { FileDropzone } from "@/components/file/file-dropzone";
import { FileGallery } from "@/components/file/file-gallery";
import { EmptyState, ErrorBand, PageHero } from "@/components/list";

const QUERY_KEY = ["files", "mine"] as const;

const IMAGE_EXTS = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"];
const DOC_EXTS = [".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv"];
const ARCHIVE_EXTS = [".zip"];

type CategoryKey = "Image" | "Document" | "Archive";

const CATEGORIES: ReadonlyArray<{
  key: CategoryKey;
  label: string;
  accept: string;
  exts: string[];
  maxBytes: number;
}> = [
  // Caps mirror Files:Categories in appsettings.json — kept in sync manually because the
  // dashboard does no codegen against the server config.
  { key: "Image", label: "Image", accept: "image/*", exts: IMAGE_EXTS, maxBytes: 10 * 1024 * 1024 },
  { key: "Document", label: "Document", accept: ".pdf,.docx,.xlsx,.pptx,.txt,.csv", exts: DOC_EXTS, maxBytes: 25 * 1024 * 1024 },
  { key: "Archive", label: "Archive", accept: ".zip", exts: ARCHIVE_EXTS, maxBytes: 50 * 1024 * 1024 },
];

export function MyFilesPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [category, setCategory] = useState<CategoryKey>("Document");

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: QUERY_KEY,
    queryFn: () => listMyFiles(1, 50),
  });

  const current = CATEGORIES.find((c) => c.key === category) ?? CATEGORIES[1];

  const onUploaded = () => {
    void queryClient.invalidateQueries({ queryKey: QUERY_KEY });
  };

  return (
    <div className="space-y-6 pb-12">
      <PageHero
        eyebrow="My Files"
        tenant={user?.tenant ?? "—"}
        title="Your files"
        subtitle="Upload images, documents, and archives. Files are private to you by default — share by minting a short-lived download URL."
      />

      <section className="space-y-3">
        <nav
          aria-label="Upload categories"
          className="-mx-1 flex flex-wrap gap-1"
        >
          {CATEGORIES.map(({ key, label }) => {
            const active = category === key;
            return (
              <button
                key={key}
                type="button"
                onClick={() => setCategory(key)}
                className={`inline-flex h-9 cursor-pointer items-center gap-1.5 rounded-full px-3.5 text-sm font-medium transition-colors duration-[var(--duration-fast)] ${
                  active
                    ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                    : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-surface-3)]"
                }`}
              >
                {label}
              </button>
            );
          })}
        </nav>

        <FileDropzone
          options={{
            ownerType: "MyFiles",
            ownerId: null,
            category: current.key,
            visibility: Visibility.Private,
            allowedExtensions: current.exts,
            maxBytes: current.maxBytes,
          }}
          accept={current.accept}
          onUploaded={onUploaded}
        />
      </section>

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
        <FileGallery files={data} isLoading={isLoading} queryKey={QUERY_KEY} />
      )}
    </div>
  );
}
