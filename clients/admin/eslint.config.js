import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
// eslint-plugin-jsx-a11y has no ESLint 10 release (jsx-eslint/eslint-plugin-jsx-a11y#1075), so this is
// the es-tooling fork: the same rules and the same recommended set, minus three long-deprecated rules.
// It is registered under the original `jsx-a11y` name so rule ids and disable comments stay unchanged,
// and moving back to upstream once it supports ESLint 10 is only this import.
import jsxA11y from 'eslint-plugin-jsx-a11y-x';
import i18next from 'eslint-plugin-i18next';
// The plugin exports no defaults object; this is 6.x's internal path, and an upgrade that moves it
// fails the lint at import rather than silently dropping the defaults.
import i18nextDefaults from 'eslint-plugin-i18next/lib/options/defaults.js';
import tseslint from 'typescript-eslint';
import { defineConfig } from 'eslint/config';

// The fork prefixes its rules `jsx-a11y-x/`; map them onto the `jsx-a11y` plugin name used below.
const jsxA11yRecommended = Object.fromEntries(
  Object.entries(jsxA11y.configs.recommended.rules).map(([rule, level]) => [
    rule.replace(/^jsx-a11y-x\//, 'jsx-a11y/'),
    level,
  ]),
);

export default defineConfig(
  { ignores: ['dist', 'node_modules', '*.config.js'] },
  {
    extends: [...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      'jsx-a11y': jsxA11y,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      ...jsxA11yRecommended,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      // autofocus is intentional on dialog search inputs (impersonate / add-members)
      // and the login email field — the first field IS the dialog's purpose.
      'jsx-a11y/no-autofocus': 'off',
      // The permission-editor checkbox labels nest their text one level deeper
      // than the rule's default search depth; the control + text are both
      // present (e.g. roles/detail.tsx), so allow the extra nesting level.
      'jsx-a11y/label-has-associated-control': ['error', { depth: 3 }],
    },
  },
  {
    // Text rendered in JSX goes through t(): an unwrapped literal would ship in English to every
    // language. Scoped to where the UI lives; tests sit outside src/ and are not linted for this.
    files: ['src/pages/**/*.tsx', 'src/components/**/*.tsx'],
    plugins: { i18next },
    rules: {
      'i18next/no-literal-string': [
        'error',
        {
          mode: 'jsx-text-only',
          // Code identifiers and key caps are not prose.
          'jsx-components': { exclude: [...i18nextDefaults['jsx-components'].exclude, 'code', 'kbd'] },
          // The plugin replaces, not merges, a list it is given, so its defaults are spread back in.
          words: {
            exclude: [
              ...i18nextDefaults.words.exclude,
              // Separators, units and glyphs (·, %, /, ✓) carry no words to translate.
              /^[\p{P}\p{S}\p{N}\s]+$/u,
              // The product name and the build tag stay as they are in every language.
              /^(fullstack|hero|fullstackhero)$/,
              /^v\d+(\.\d+)*( · admin)?$/,
            ],
          },
        },
      ],
    },
  },
);
