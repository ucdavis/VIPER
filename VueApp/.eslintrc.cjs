/* eslint-env node */
require('@rushstack/eslint-patch/modern-module-resolution')

module.exports = {
  root: true,
  'extends': [
    'eslint:recommended',
    'plugin:vue/vue3-strongly-recommended',
    '@vue/eslint-config-typescript',
    'plugin:security/recommended-legacy'
  ],
  plugins: [
    'security',
    'html'
  ],
  parserOptions: {
    ecmaVersion: 'latest'
  },
  overrides: [
    {
      files: ['**/*.{html,cshtml}'],
      plugins: ['html'],
      rules: {
        // Security rules for HTML files with embedded JS/Vue
        'security/detect-unsafe-regex': 'error',
        'security/detect-non-literal-regexp': 'error',
        'vue/no-v-html': 'error'
      }
    }
  ],
  rules: {
    // =============================================================================
    // SECURITY RULES 
    // =============================================================================
    // Security rules are handled by 'plugin:security/recommended-legacy' preset
    // All security/ rules are automatically configured as errors
    
    // Vue.js specific security concerns
    'vue/no-v-html': 'error',                              // Prevents XSS via v-html
    'vue/no-v-text-v-html-on-component': 'error',          // Component XSS protection

    // =============================================================================
    // CODE QUALITY OVERRIDES
    // =============================================================================
    
    // Disable multi-word component names rule for area-based architecture
    // Components like "Computing", "Students" represent functional areas
    'vue/multi-word-component-names': 'off'
  }
}
