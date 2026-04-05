// Import plugins for comprehensive linting
import eslint from "@eslint/js"
import security from "eslint-plugin-security"
import html from "eslint-plugin-html"

// oxlint-disable-next-line import/no-default-export, import/no-anonymous-default-export -- ESLint flat config requires default export
export default [
    // Global ignores
    {
        ignores: [
            "node_modules/**",
            "bin/**",
            "obj/**",
            "web/wwwroot/**",
            "**/*.min.js",
            "**/*.bundle.js",
            "VueApp/node_modules/**",
            "VueApp/dist/**",
            "**/dist",
            "**/coverage",
            "*.d.ts",
        ],
    },

    // Main configuration for JS files (non-Vue)
    {
        files: ["scripts/**/*.{js,ts}"],
        ...eslint.configs.recommended,
        plugins: {
            security: security,
        },
        languageOptions: {
            ecmaVersion: "latest",
            sourceType: "module",
        },
        rules: {
            // Security plugin recommended rules
            ...security.configs.recommended.rules,

            // Allow console and process.exit in scripts
            "no-console": "off",
            "no-process-exit": "off",
        },
    },

    // Exclude cshtml files with Razor syntax (@Url.Content, ~) inside <script> blocks
    // that breaks the JS parser — no rules can run on these anyway
    {
        ignores: [
            "web/Areas/Directory/Views/Card.cshtml",
            "web/Areas/Directory/Views/Table.cshtml",
            "web/Areas/Students/Views/StudentClassYear.cshtml",
            "web/Areas/Students/Views/StudentClassYearImport.cshtml",
        ],
    },

    // Configuration for CSHTML files (JavaScript in <script> tags)
    {
        files: ["web/**/*.cshtml"],
        plugins: {
            html,
            security,
        },
        languageOptions: {
            globals: {
                // Browser globals (cshtml scripts run in browser context)
                fetch: "readonly",
                URL: "readonly",
                URLSearchParams: "readonly",
                location: "readonly",
                history: "readonly",
                window: "readonly",
                document: "readonly",
                console: "readonly",
                setTimeout: "readonly",
                setInterval: "readonly",
                // Project globals (loaded via shared script tags in _VIPERLayout.cshtml)
                createVueApp: "readonly",
                viperFetch: "readonly",
                quasarTable: "readonly",
                formatDate: "readonly",
                formatDateForDateInput: "readonly",
                getItemFromStorage: "readonly",
                putItemInStorage: "readonly",
                Quasar: "readonly",
            },
        },
        settings: {
            "html/html-extensions": [".cshtml"],
            "html/xml-extensions": [],
            "html/indent": "+4",
            "html/report-bad-indent": "error",
        },
        rules: {
            // Apply same basic quality rules as Vue.js
            ...eslint.configs.recommended.rules,

            // Security rules (from Vue.js config)
            ...security.configs.recommended.rules,

            // Additional JavaScript quality rules from Vue config (not in recommended)
            "no-alert": "warn",

            // Keep these as errors - they prevent real bugs
            eqeqeq: "error",
            curly: "error",
            "no-eval": "error",
            "no-implied-eval": "error",
            "no-new-func": "error",
        },
    },
]
