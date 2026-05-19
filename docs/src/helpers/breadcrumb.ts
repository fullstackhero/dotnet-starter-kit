/**
 * Build schema.org BreadcrumbList items from a docs page URL.
 *
 * Input  : "/docs/getting-started/install/"
 * Output : [
 *   { name: "Home",            url: "https://docs.fullstackhero.net/" },
 *   { name: "Docs",            url: "https://docs.fullstackhero.net/docs/" },
 *   { name: "Getting Started", url: "https://docs.fullstackhero.net/docs/getting-started/" },
 *   { name: "Install",         url: "https://docs.fullstackhero.net/docs/getting-started/install/" },
 * ]
 *
 * The final crumb (the current page) overrides `name` with the page's
 * actual title when supplied — so "Install" becomes "Install the .NET 10
 * Starter Kit" if that's the frontmatter title.
 */
export interface BreadcrumbItem {
  name: string;
  url: string;
}

const titleCase = (segment: string): string =>
  segment
    .split('-')
    .map((w) => (w.length <= 2 ? w.toUpperCase() : w[0].toUpperCase() + w.slice(1)))
    .join(' ');

export function buildBreadcrumbItems(
  pathname: string,
  siteUrl: string,
  currentPageTitle?: string,
): BreadcrumbItem[] {
  const trimmed = pathname.replace(/^\/+|\/+$/g, '');
  const segments = trimmed === '' ? [] : trimmed.split('/');

  const items: BreadcrumbItem[] = [{ name: 'Home', url: new URL('/', siteUrl).toString() }];

  let acc = '';
  segments.forEach((segment, i) => {
    acc += `/${segment}`;
    const isLast = i === segments.length - 1;
    items.push({
      name: isLast && currentPageTitle ? currentPageTitle : titleCase(segment),
      url: new URL(`${acc}/`, siteUrl).toString(),
    });
  });

  return items;
}

/**
 * Convert BreadcrumbItem[] into the schema.org BreadcrumbList JSON-LD
 * structure ready to JSON.stringify into a <script>.
 */
export function buildBreadcrumbSchema(items: BreadcrumbItem[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, idx) => ({
      '@type': 'ListItem',
      position: idx + 1,
      name: item.name,
      item: item.url,
    })),
  };
}
