export default {
  plugins: ["@double-great/stylelint-a11y"],
  extends: [
    "stylelint-config-standard",
    "@double-great/stylelint-a11y/strict"
  ],
  ignoreFiles: [
    "**/bin/**",                    // .NET build output directories
    "**/node_modules/**",           // Node.js package dependencies
    "**/obj/**",                    // .NET intermediate build files
    "**/scopedcss/**",              // .NET scoped CSS build artifacts
    "**/*.bundle.css",              // Bundled CSS files
    "**/*.min.css",                 // Minified CSS files
    "web/wwwroot/css/site-*.css",   // Environment-specific CSS files (often empty)
    "web/wwwroot/lib/**",           // Third-party libraries (Quasar, etc.)
    "web/wwwroot/vue/**",           // Built Vue assets
  ],
  rules: {
    // IGNORED STYLE RULES
    "custom-property-pattern": null,                // CSS custom property naming patterns
    "selector-class-pattern": null,                 // Legacy class names like .assessmentBubble5_1
    "selector-id-pattern": null,                    // Legacy ID patterns
    "selector-pseudo-class-no-unknown": null,       // Vue pseudo-classes like :deep, :global
    "no-descending-specificity": null,              // Too strict for complex legacy CSS
  }
};