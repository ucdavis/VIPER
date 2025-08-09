/* eslint-env node */
require('@rushstack/eslint-patch/modern-module-resolution')

module.exports = {
  root: true,
  'extends': [
    'plugin:vue/vue3-essential',
    'eslint:recommended',
    '@vue/eslint-config-typescript'
  ],
  parserOptions: {
    ecmaVersion: 'latest'
  },
  rules: {
    // Disable multi-word component names rule for area-based architecture
    // Components like "Computing", "Students" represent functional areas
    'vue/multi-word-component-names': 'off'
  }
}
