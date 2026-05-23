export type Section = {
  dir: string;
  label: string;
  order: number;
};

export const sections: Section[] = [
  { dir: 'getting-started',         label: 'Getting Started',         order: 1  },
  { dir: 'architecture',            label: 'Architecture',            order: 2  },
  { dir: 'modules',                 label: 'Modules',                 order: 3  },
  { dir: 'building-blocks',         label: 'Building Blocks',         order: 4  },
  { dir: 'cross-cutting-concerns',  label: 'Cross-Cutting Concerns',  order: 5  },
  { dir: 'security',                label: 'Security',                order: 6  },
  { dir: 'frontend',                label: 'Frontend',                order: 7  },
  { dir: 'ai-development',          label: 'AI-Driven Development',    order: 8  },
  { dir: 'cli',                     label: 'CLI',                     order: 9  },
  { dir: 'testing',                 label: 'Testing',                 order: 10 },
  { dir: 'guides',                  label: 'Guides',                  order: 11 },
  { dir: 'deployment',              label: 'Deployment',              order: 12 },
  { dir: 'compare',                 label: 'Compare',                 order: 13 },
  { dir: 'contributing',            label: 'Contributing',            order: 14 },
  { dir: 'changelog',               label: 'Changelog',               order: 15 },
];
