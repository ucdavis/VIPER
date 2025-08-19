// Import plugins for comprehensive linting
import eslint from '@eslint/js';
import security from 'eslint-plugin-security';
import html from 'eslint-plugin-html';

export default [
  // Global ignores
  {
    ignores: [
      'node_modules/**',
      'bin/**',
      'obj/**',
      'web/wwwroot/**',
      '**/*.min.js',
      '**/*.bundle.js',
      'VueApp/node_modules/**',
      'VueApp/dist/**',
      '**/dist',
      '**/coverage',
      '*.d.ts'
    ]
  },

  // Main configuration for JS files (non-Vue)
  {
    files: ['scripts/**/*.{js,ts}'],
    ...eslint.configs.recommended,
    plugins: {
      security: security
    },
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module'
    },
    rules: {
      // Security plugin recommended rules
      ...security.configs.recommended.rules,

      // Allow console and process.exit in scripts
      'no-console': 'off',
      'no-process-exit': 'off'
    }
  },

  // Configuration for CSHTML files (JavaScript in <script> tags)
  {
    files: ['web/**/*.cshtml'],
    plugins: {
      html,
      security
    },
    settings: {
      'html/html-extensions': ['.cshtml'],
      'html/xml-extensions': [],
      'html/indent': '+4',
      'html/report-bad-indent': 'error'
    },
    rules: {
      // Apply same basic quality rules as Vue.js
      ...eslint.configs.recommended.rules,
      
      // Security rules (from Vue.js config)
      ...security.configs.recommended.rules,
      
      // Project formatting standards
      'semi': ['error', 'never'],          // Match Prettier: no semicolons
      'quotes': ['error', 'double'],       // Match Prettier: double quotes
      'indent': ['error', 4],              // Match EditorConfig: 4 spaces for HTML/CSHTML
      'no-console': 'error',               // Match Vue.js: strict no console
      
      // Additional JavaScript quality rules from Vue config (not in recommended)
      'no-alert': 'warn',
      'eqeqeq': 'error',
      'curly': 'error',
      'no-eval': 'error',
      'no-implied-eval': 'error',
      'no-new-func': 'error'
    }
  }
];
