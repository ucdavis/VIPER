module.exports = {
  root: true,
  env: {
    node: true,
    es2022: true
  },
  extends: ['eslint:recommended'],
  parserOptions: {
    ecmaVersion: 'latest',
    sourceType: 'module'
  },
  rules: {
    'no-console': 'off',        // Scripts need console output
    'no-process-exit': 'off',   // Scripts need to exit with codes
    'prefer-const': 'error',
    'no-var': 'error',
    'no-unused-vars': 'warn'
  }
};