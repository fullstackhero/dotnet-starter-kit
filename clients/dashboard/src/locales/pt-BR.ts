// Every pt-BR catalog in one module, so the language ships as a single lazy chunk that
// loadLanguage (src/i18n.ts) fetches on demand. Keyed by namespace (the file name); a namespace
// without a file here renders in English through the fallback chain.
const files = import.meta.glob<Record<string, string>>("./pt-BR/*.json", {
  eager: true,
  import: "default",
});

export default Object.fromEntries(
  Object.entries(files).map(([file, catalog]) => [file.slice("./pt-BR/".length, -".json".length), catalog]),
);
