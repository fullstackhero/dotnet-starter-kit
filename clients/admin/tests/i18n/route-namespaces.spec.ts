import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

// Route-only translation catalogs are not in the main bundle: src/i18n.ts loads them when a route
// that lists them (lazyNamed's third argument in src/routes.tsx) is opened. A page, or a
// component it imports, that uses a namespace its route doesn't list renders guessed labels.
// This walks each lazy target's static import graph and fails on any namespace it uses that
// isn't loaded by the time it renders.
const src = fileURLToPath(new URL("../../src", import.meta.url));

const read = (file: string) => readFileSync(file, "utf8");

const files = new Map<string, string>();
(function walk(dir: string) {
  for (const name of readdirSync(dir)) {
    const full = path.join(dir, name);
    if (statSync(full).isDirectory()) walk(full);
    else if (/\.tsx?$/.test(name) && !/\.(test|spec)\./.test(name)) files.set(full, read(full));
  }
})(src);

function resolve(spec: string, from: string): string | undefined {
  let base: string;
  if (spec.startsWith("@/")) base = path.join(src, spec.slice(2));
  else if (spec.startsWith(".")) base = path.join(path.dirname(from), spec);
  else return undefined;
  for (const ext of [".tsx", ".ts", "/index.tsx", "/index.ts", ""]) {
    const candidate = path.normalize(base + ext);
    if (files.has(candidate)) return candidate;
  }
  return undefined;
}

const STATIC_IMPORT =
  /^\s*(?:import|export)\s+(?!type\b)[^;]*?from\s+["']([^"']+)["']|^\s*import\s+["']([^"']+)["']/gm;
const NAMESPACE_USE = [
  /useTranslation\(\s*["']([A-Za-z]+)["']/g,
  /useTranslation\(\s*\[([^\]]*)\]/g,
  /\bns:\s*["']([A-Za-z]+)["']/g,
  /\bt\(\s*["'`]([A-Za-z]+):/g,
];

function staticImports(file: string): string[] {
  const out: string[] = [];
  for (const m of files.get(file)!.matchAll(STATIC_IMPORT)) {
    const target = resolve(m[1] ?? m[2], file);
    if (target) out.push(target);
  }
  return out;
}

function closure(start: string): Set<string> {
  const seen = new Set<string>();
  const stack = [start];
  while (stack.length) {
    const next = stack.pop()!;
    if (seen.has(next)) continue;
    seen.add(next);
    stack.push(...staticImports(next));
  }
  return seen;
}

function namespacesUsed(modules: Iterable<string>): Map<string, string> {
  const used = new Map<string, string>(); // namespace -> first module using it
  for (const file of modules) {
    for (const rx of NAMESPACE_USE) {
      for (const m of files.get(file)!.matchAll(rx)) {
        for (const ns of m[1].match(/[A-Za-z]+/g) ?? []) {
          if (!used.has(ns)) used.set(ns, path.relative(src, file));
        }
      }
    }
  }
  return used;
}

// The eager namespaces: the keys of the eager map in src/i18n.ts.
const i18nSource = read(path.join(src, "i18n.ts"));
const eagerBlock = /const EAGER_CATALOGS = \{([^}]*)\}/.exec(i18nSource)?.[1] ?? "";
const EAGER = new Set([...eagerBlock.matchAll(/^\s*([A-Za-z]+):/gm)].map((m) => m[1]));

const initial = closure(path.join(src, "main.tsx"));

test.describe("route translation namespaces", () => {
  test("the eager namespace list was found", () => {
    expect([...EAGER].sort()).toEqual(["auth", "common", "dashboard", "nav", "notifications"]);
  });

  test("the app shell only uses eager namespaces", () => {
    const lazyInShell = [...namespacesUsed(initial)].filter(([ns]) => !EAGER.has(ns));
    expect(lazyInShell).toEqual([]);
  });

  // `lazyNamed(() => import("@/pages/x"), "XPage", ["ns", …])`
  const routes = [
    ...read(path.join(src, "routes.tsx")).matchAll(
      /lazyNamed\(\s*\(\) => import\("([^"]+)"\),\s*"[A-Za-z]+"(?:,\s*\[([^\]]*)\])?/g,
    ),
  ];

  test("routes.tsx declares its lazy routes through lazyNamed", () => {
    expect(routes.length).toBeGreaterThan(15);
  });

  for (const [, spec, listed] of routes) {
    test(`${spec} lists every namespace it uses`, () => {
      const page = resolve(spec, path.join(src, "routes.tsx"));
      expect(page, `cannot resolve ${spec}`).toBeTruthy();
      const declared = new Set(listed?.match(/[A-Za-z]+/g) ?? []);
      const missing = [...namespacesUsed(closure(page!))]
        .filter(([ns]) => !EAGER.has(ns) && !declared.has(ns))
        .map(([ns, file]) => `${ns} (used in ${file})`);
      expect(missing).toEqual([]);
    });
  }

  test("every lazy catalog file exists", () => {
    for (const m of i18nSource.matchAll(/import\("@\/locales\/en-US\/([A-Za-z]+)\.json"\)/g)) {
      expect(existsSync(path.join(src, "locales", "en-US", `${m[1]}.json`))).toBe(true);
    }
  });
});
