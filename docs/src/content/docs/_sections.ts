export type Section = {
  dir: string;
  label: string;
  order: number;
};

export const sections: Section[] = [
  { dir: 'getting-started', label: 'Getting Started', order: 1 },
  { dir: 'concepts',        label: 'Concepts',        order: 2 },
  { dir: 'modules',         label: 'Modules',         order: 3 },
  { dir: 'recipes',         label: 'Recipes',         order: 4 },
  { dir: 'reference',       label: 'Reference',       order: 5 },
  { dir: 'contributing',    label: 'Contributing',    order: 6 },
];
