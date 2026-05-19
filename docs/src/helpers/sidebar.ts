import { getCollection, type CollectionEntry } from 'astro:content';
import { sections, type Section } from '../content/docs/_sections';

export type SidebarPage = {
  title: string;
  href: string;
  order: number;
};

export type SidebarSection = {
  dir: string;
  label: string;
  order: number;
  indexHref: string | null;
  pages: SidebarPage[];
};

/**
 * `entry.id` shape varies by Astro version + loader:
 *   - Astro 5: 'getting-started/install.mdx'
 *   - Astro 6 glob loader: 'getting-started/install' (no extension)
 * Normalize defensively.
 */
function normalizeId(id: string): string {
  return id.replace(/\.mdx$/, '');
}

function pathFromId(id: string): string {
  const norm = normalizeId(id);
  if (norm === 'index') return '/docs/';
  const trimmed = norm.replace(/\/index$/, '');
  return `/docs/${trimmed}/`;
}

function isIndexEntry(id: string, dir: string): boolean {
  const norm = normalizeId(id);
  return norm === `${dir}/index`;
}

function isRootIndex(id: string): boolean {
  return normalizeId(id) === 'index';
}

export async function getRootPage(): Promise<SidebarPage | null> {
  const all = await getCollection('docs');
  const root = all.find((e) => isRootIndex(e.id));
  if (!root || root.data.sidebar.hidden) return null;
  return {
    title: root.data.sidebar.label ?? root.data.title,
    href: pathFromId(root.id),
    order: root.data.sidebar.order,
  };
}

export async function buildSidebar(): Promise<SidebarSection[]> {
  const all = await getCollection('docs');
  const bySection = new Map<string, CollectionEntry<'docs'>[]>();

  for (const entry of all) {
    if (isRootIndex(entry.id)) continue;
    const [dir] = normalizeId(entry.id).split('/');
    if (!bySection.has(dir)) bySection.set(dir, []);
    bySection.get(dir)!.push(entry);
  }

  const result: SidebarSection[] = sections.map((sec: Section) => {
    const entries = bySection.get(sec.dir) ?? [];
    const indexEntry = entries.find((e) => isIndexEntry(e.id, sec.dir));
    const pages = entries
      .filter((e) => !isIndexEntry(e.id, sec.dir))
      .filter((e) => !e.data.sidebar.hidden)
      .map<SidebarPage>((e) => ({
        title: e.data.sidebar.label ?? e.data.title,
        href: pathFromId(e.id),
        order: e.data.sidebar.order,
      }))
      .sort((a, b) => a.order - b.order || a.title.localeCompare(b.title));

    return {
      dir: sec.dir,
      label: sec.label,
      order: sec.order,
      indexHref: indexEntry ? pathFromId(indexEntry.id) : null,
      pages,
    };
  });

  return result.sort((a, b) => a.order - b.order);
}

export function isActivePath(currentPath: string, href: string): boolean {
  const normalize = (p: string) => (p.endsWith('/') ? p : `${p}/`);
  return normalize(currentPath) === normalize(href);
}
