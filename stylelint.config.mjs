// oxlint-disable-next-line import/no-default-export, import/no-anonymous-default-export -- Stylelint config requires default export
export default {
    // The a11y plugin's own "recommended" tier is what we enforce. Its "strict" tier
    // turns on nine further rules which between them produced 132 findings here and
    // no real defects: dark-theme demands (we ship a single light theme whose
    // contrast pairings are verified against WCAG AA, see DESIGN.md), baseline-grid
    // line heights, and display:none inside print and responsive blocks. Every
    // genuine WCAG failure found so far came from a recommended rule. Revisit strict,
    // media-prefers-color-scheme in particular, if we implement dark mode.
    extends: ["stylelint-config-standard", "@double-great/stylelint-a11y/recommended"],
    // Must stay scoped, never top-level: postcss-html returns an empty document for a
    // plain .css file, so a global customSyntax silently skips every CSS file in the repo.
    overrides: [
        {
            files: ["**/*.vue", "**/*.html"],
            customSyntax: "postcss-html",
        },
    ],
    ignoreFiles: [
        "**/bin/**", // .NET build output directories
        "dist/**", // .NET publish output (gitignored)
        "jscpd-report/**", // Generated jscpd report, vendors tailwind.css/prism.css (gitignored)
        "**/node_modules/**", // Node.js package dependencies
        "**/obj/**", // .NET intermediate build files
        "**/scopedcss/**", // .NET scoped CSS build artifacts
        "**/*.bundle.css", // Bundled CSS files
        "**/*.min.css", // Minified CSS files
        "web/wwwroot/css/site-*.css", // Environment-specific CSS files (often empty)
        "web/wwwroot/lib/**", // Third-party libraries (Quasar, etc.)
        "web/wwwroot/vue/**", // Built Vue assets
    ],
    rules: {
        // IGNORED STYLE RULES
        "custom-property-pattern": null, // CSS custom property naming patterns
        "selector-class-pattern": null, // Legacy class names like .assessmentBubble5_1
        "selector-id-pattern": null, // Legacy ID patterns
        "selector-pseudo-class-no-unknown": null, // Vue pseudo-classes like :deep, :global
        "no-descending-specificity": null, // Too strict for complex legacy CSS
    },
}
