import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import jsxA11y from 'eslint-plugin-jsx-a11y';
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
      //   where the destructive action should be the default focus.
      'jsx-a11y/no-autofocus': 'off',
      // - role="tooltip" on a span is supplemented by the parent link's
      //   `title=` attribute; the popup is a visual hint only.
      'jsx-a11y/no-noninteractive-element-interactions': 'warn',
    },
  },
);
