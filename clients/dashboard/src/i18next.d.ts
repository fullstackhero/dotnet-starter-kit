import "i18next";
import type { Catalogs } from "@/i18n";

// Types every t() key against the en-US catalogs, so a key its namespace lacks fails `tsc -b`
// instead of rendering missingKeyFallback's guessed label in every language.
declare module "i18next" {
  interface CustomTypeOptions {
    defaultNS: "common";
    resources: Catalogs;
  }
}
