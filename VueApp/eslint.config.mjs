// Import plugins for comprehensive linting
import eslint from "@eslint/js"
import pluginVue from "eslint-plugin-vue"
import globals from "globals"
import eslintConfigPrettier from "eslint-config-prettier"
import a11y from "eslint-plugin-vuejs-accessibility"
import quasarEslint from "@quasar/app-vite/eslint"
import tsParser from "@typescript-eslint/parser"
import vueParser from "vue-eslint-parser"
import tseslint from "@typescript-eslint/eslint-plugin"

export default [
    // Global ignores - Exclude .ts and .js files since we use oxlint for TypeScript and JavaScript
    {
        ignores: ["node_modules/**", "dist/**", "coverage/**", "*.d.ts", "**/*.ts", "**/*.js", "**/*.mjs", "**/*.cjs"],
    },

    // Quasar recommended configuration (includes Quasar-specific rules)
    ...quasarEslint.configs.recommended(),

    // ESLint recommended rules
    eslint.configs.recommended,

    // Vue strongly recommended rules
    ...pluginVue.configs["flat/strongly-recommended"],

    // Main configuration for Vue files
    {
        files: ["**/*.vue"],
        plugins: {
            "vuejs-accessibility": a11y,
            "@typescript-eslint": tseslint,
        },
        languageOptions: {
            parser: vueParser,
            ecmaVersion: "latest",
            sourceType: "module",
            parserOptions: {
                parser: tsParser,
                extraFileExtensions: [".vue"],
                ecmaFeatures: {
                    jsx: true,
                },
            },
            globals: {
                ...globals.browser,
                ...globals.node,
            },
        },
        rules: {
            // Disable the base rule and enable TypeScript-aware version
            "no-unused-vars": "off",
            "@typescript-eslint/no-unused-vars": ["error", {
                "args": "all",
                "argsIgnorePattern": "^_",
                "varsIgnorePattern": "^_",
                "caughtErrorsIgnorePattern": "^_",
                "ignoreRestSiblings": true
            }],
            // Vue security rules
            "vue/no-v-html": "error", // Prevents XSS via v-html
            "vue/no-v-text-v-html-on-component": "error", // Component XSS protection

            // CRITICAL RULES - WCAG 2.1 Level A (Required for AA compliance):
            "vuejs-accessibility/alt-text": "error", // WCAG 1.1.1 Non-text Content (A)
            "vuejs-accessibility/anchor-has-content": "error", // WCAG 2.4.4 Link Purpose (A)
            "vuejs-accessibility/form-control-has-label": "error", // WCAG 3.3.2 Labels or Instructions (A)
            "vuejs-accessibility/label-has-for": "error", // WCAG 1.3.1 Info and Relationships (A)
            "vuejs-accessibility/iframe-has-title": "error", // WCAG 2.4.1 Bypass Blocks (A)
            "vuejs-accessibility/interactive-supports-focus": "error", // WCAG 2.1.1 Keyboard (A)
            "vuejs-accessibility/click-events-have-key-events": "error", // WCAG 2.1.1 Keyboard (A)
            "vuejs-accessibility/tabindex-no-positive": "error", // WCAG 2.4.3 Focus Order (A)
            "vuejs-accessibility/aria-props": "error", // WCAG 4.1.2 Name, Role, Value (A)
            "vuejs-accessibility/aria-role": "error", // WCAG 4.1.2 Name, Role, Value (A)
            "vuejs-accessibility/aria-unsupported-elements": "error", // WCAG 4.1.2 Name, Role, Value (A)
            "vuejs-accessibility/no-distracting-elements": "error", // WCAG 2.2.2 Pause, Stop, Hide (A)
            "vuejs-accessibility/no-redundant-roles": "error", // WCAG 4.1.2 Name, Role, Value (A)
            "vuejs-accessibility/role-has-required-aria-props": "error", // WCAG 4.1.2 Name, Role, Value (A)

            // CRITICAL RULES - WCAG 2.1 Level AA:
            "vuejs-accessibility/no-autofocus": "error", // WCAG 2.4.7 Focus Visible (AA)
            "vuejs-accessibility/heading-has-content": "error", // WCAG 2.4.6 Headings and Labels (AA)

            // BEST PRACTICES - Recommended but not blocking:
            "vuejs-accessibility/media-has-caption": "warn", // WCAG 1.2.2 Captions (A) - Often handled server-side
            "vuejs-accessibility/mouse-events-have-key-events": "warn", // WCAG 2.1.1 Keyboard (A) - Broader than click events
            "vuejs-accessibility/no-access-key": "warn", // Best practice - Access keys can conflict with AT
            "vuejs-accessibility/no-onchange": "warn", // WCAG 3.2.2 On Input (A) - Context changes should be user-initiated

            // Turn off rules that go against Vue.js project practices
            "no-null": "off", // Vue.js ecosystem uses null extensively
            "no-eq-null": "off", // Vue.js community standard - != null is widely accepted
            "no-undefined": "off", // Vue.js apps commonly check for undefined explicitly
            "vue/multi-word-component-names": "off", // Allow single-word area components
        },
    },

    // TypeScript files - removed since we use oxlint for .ts files

    // Browser specific Vue files - stricter console rules
    {
        files: ["src/**/*.vue"],
        languageOptions: {
            globals: globals.browser,
        },
        rules: {
            "no-console": "error", // Strict no console in production code
        },
    },

    // Prettier config - must be last to override formatting rules
    eslintConfigPrettier,
]
