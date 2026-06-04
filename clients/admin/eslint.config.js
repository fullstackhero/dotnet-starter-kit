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
      // autofocus is intentional on dialog search inputs (impersonate / add-members)
      // and the login email field — the first field IS the dialog's purpose.
      'jsx-a11y/no-autofocus': 'off',
      // The permission-editor checkbox labels nest their text one level deeper
      // than the rule's default search depth; the control + text are both
      // present (e.g. roles/detail.tsx), so allow the extra nesting level.
      'jsx-a11y/label-has-associated-control': ['error', { depth: 3 }],
    },
  },
);
