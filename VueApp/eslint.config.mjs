// Import plugins for comprehensive linting
import eslint from "@eslint/js"
import pluginVue from "eslint-plugin-vue"
import tseslint from "typescript-eslint"
import { defineConfigWithVueTs, vueTsConfigs } from "@vue/eslint-config-typescript"
import globals from "globals"
import eslintConfigPrettier from "eslint-config-prettier"
import a11y from "eslint-plugin-vuejs-accessibility"
import quasarEslint from "@quasar/app-vite/eslint"

export default defineConfigWithVueTs(
    // Global ignores
    {
        ignores: ["node_modules/**", "dist/**", "coverage/**", "*.d.ts"],
    },

    // Quasar recommended configuration (includes Quasar-specific rules)
    ...quasarEslint.configs.recommended(),

    // Main configuration for Vue + TypeScript files
    {
        files: ["**/*.{js,ts,jsx,tsx,vue}"],
        extends: [
            eslint.configs.recommended,
            ...tseslint.configs.recommended,
            ...pluginVue.configs["flat/strongly-recommended"],
            eslintConfigPrettier,
        ],
        plugins: {
            "vuejs-accessibility": a11y,
        },
        languageOptions: {
            ecmaVersion: "latest",
            sourceType: "module",
            globals: {
                ...globals.browser,
                ...globals.node,
            },
            parserOptions: {
                parser: tseslint.parser,
            },
        },
        rules: {
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
            // Architectural overrides
            "vue/multi-word-component-names": "off", // Allow single-word area components
        },
    },

    // Node.js specific files (config files)
    {
        files: ["*.{js,ts}", "**/*.config.{js,ts}"],
        languageOptions: {
            globals: globals.node,
        },
        rules: {
            "no-console": "off", // Allow console in Node.js config files
        },
    },

    // Browser specific files - stricter console rules
    {
        files: ["src/**/*.{js,ts,jsx,tsx,vue}"],
        languageOptions: {
            globals: globals.browser,
        },
        rules: {
            "no-console": "error", // Strict no console in production code
        },
    },

    // Type-aware rules for Vue + TypeScript
    vueTsConfigs.recommendedTypeChecked,
)
