import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import i18next from 'eslint-plugin-i18next';
// The plugin exports no defaults object; this is 6.x's internal path, and an upgrade that moves it
// fails the lint at import rather than silently dropping the defaults.
import i18nextDefaults from 'eslint-plugin-i18next/lib/options/defaults.js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
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
      ...jsxA11y.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      // Project-specific deviations from jsx-a11y/recommended:
      // - autofocus is intentionally used on confirmation dialogs (sign-out)
      //   where the destructive action should be the default focus, and on
      //   the command-palette search input which is its only purpose.
      'jsx-a11y/no-autofocus': 'off',
      // Promote from warn → error now that the four outstanding warnings
      // have been resolved (img onError is excluded explicitly below so
      // legitimate fallback handlers don't trip the rule).
      'jsx-a11y/no-noninteractive-element-interactions': [
        'error',
        {
          handlers: [
            'onClick',
            'onMouseDown',
            'onMouseUp',
            'onKeyPress',
            'onKeyDown',
            'onKeyUp',
          ],
        },
      ],
      'jsx-a11y/no-noninteractive-element-to-interactive-role': 'error',
      'jsx-a11y/no-aria-hidden-on-focusable': 'error',
      'jsx-a11y/anchor-has-content': 'error',
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
              /^v\d+(\.\d+)*( · dashboard)?$/,
            ],
          },
        },
      ],
    },
  },
);
