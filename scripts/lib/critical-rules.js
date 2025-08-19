/**
 * Security and accessibility rule definitions for lint-staged scripts
 * Centralized configuration to ensure consistency across all linting tools
 */

/**
 * Critical accessibility rules that MUST block commits (WCAG 2.1 AA compliance)
 * NOTE: Only includes rules that are actually enabled in stylelint.config.mjs
 * Based on WCAG 2.1 AA analysis - only true Level AA requirements are treated as critical
 */
const CRITICAL_ACCESSIBILITY_RULES = [
  // CSS Accessibility - WCAG 2.1 AA Required:
  'a11y/no-outline-none',                              // WCAG 2.4.7 Focus Visible (AA) - Enforces visible keyboard focus
  'a11y/selector-pseudo-class-focus',                  // WCAG 2.4.7 Focus Visible (AA) - Enforces visible keyboard focus

  // Vue.js Accessibility - WCAG 2.1 Level A (Required for AA compliance):
  'vuejs-accessibility/alt-text',                      // WCAG 1.1.1 Non-text Content (A) - Images must have alt text
  'vuejs-accessibility/anchor-has-content',            // WCAG 2.4.4 Link Purpose (A) - Links must have descriptive content
  'vuejs-accessibility/form-control-has-label',        // WCAG 3.3.2 Labels or Instructions (A) - Form inputs must have labels
  'vuejs-accessibility/label-has-for',                 // WCAG 1.3.1 Info and Relationships (A) - Labels must be associated with controls
  'vuejs-accessibility/iframe-has-title',              // WCAG 2.4.1 Bypass Blocks (A) - iframes must have descriptive titles
  'vuejs-accessibility/interactive-supports-focus',    // WCAG 2.1.1 Keyboard (A) - Interactive elements must be keyboard accessible
  'vuejs-accessibility/click-events-have-key-events',  // WCAG 2.1.1 Keyboard (A) - Mouse events must have keyboard equivalents
  'vuejs-accessibility/tabindex-no-positive',          // WCAG 2.4.3 Focus Order (A) - Avoid positive tabindex
  'vuejs-accessibility/aria-props',                    // WCAG 4.1.2 Name, Role, Value (A) - ARIA properties must be valid
  'vuejs-accessibility/aria-role',                     // WCAG 4.1.2 Name, Role, Value (A) - ARIA roles must be valid
  'vuejs-accessibility/aria-unsupported-elements',     // WCAG 4.1.2 Name, Role, Value (A) - No ARIA on unsupported elements
  'vuejs-accessibility/no-distracting-elements',       // WCAG 2.2.2 Pause, Stop, Hide (A) - No marquee or blink elements
  'vuejs-accessibility/no-redundant-roles',            // WCAG 4.1.2 Name, Role, Value (A) - Avoid redundant ARIA roles
  'vuejs-accessibility/role-has-required-aria-props',  // WCAG 4.1.2 Name, Role, Value (A) - Roles must have required ARIA props

  // Vue.js Accessibility - WCAG 2.1 Level AA:
  'vuejs-accessibility/no-autofocus',                  // WCAG 2.4.7 Focus Visible (AA) - Autofocus can disorient users
  'vuejs-accessibility/heading-has-content',           // WCAG 2.4.6 Headings and Labels (AA) - Headings must have content
];

/**
 * Security rules that indicate critical vulnerabilities
 * These rules should block commits to prevent security issues
 */
const CRITICAL_SECURITY_RULES = [
  // ESLint security plugin rules
  'security/detect-unsafe-regex',
  'security/detect-buffer-noassert',
  'security/detect-child-process',
  'security/detect-disable-mustache-escape',
  'security/detect-eval-with-expression',
  'security/detect-no-csrf-before-method-override',
  'security/detect-non-literal-fs-filename',
  'security/detect-non-literal-regexp',
  'security/detect-non-literal-require',
  'security/detect-object-injection',
  'security/detect-possible-timing-attacks',
  'security/detect-pseudoRandomBytes',

  // Vue-specific security rules
  'vue/no-v-html',                           // Prevents XSS through v-html
  'vue/no-v-text-v-html-on-component',       // Prevents XSS on custom components

  // Core JavaScript security rules
  'no-eval',                                 // Prevents code injection
  'no-implied-eval',                         // Prevents indirect eval
  'no-new-func',                            // Prevents Function constructor
];

/**
 * .NET/SonarAnalyzer security rules that should block commits
 * Uses category-based detection for Microsoft CA rules and explicit SonarAnalyzer rules
 */
const DOTNET_SECURITY_RULES = [
  // SonarAnalyzer security rules (explicit list needed as they don't use categories)
  'S2083',  // Path traversal
  'S2091',  // XPath injection
  'S2092',  // Cookie security
  'S2245',  // Insecure random
  'S3649',  // SQL injection
  'S4426',  // Weak crypto
  'S5131',  // CSRF protection
  'S2068',  // Hardcoded credentials
  'S2612',  // File permissions
  'S4423',  // Weak SSL/TLS
  'S4507',  // Debugging enabled
  'S5042',  // Zip slip
  'S5122',  // CORS policy
  'S5144',  // Server certificates
  'S5443',  // Operating system command injection
  'S5659',  // JWT signature verification
  'S5693',  // XML validation
  'S5738',  // Hash algorithm
  'S5766',  // Deserialization
  'S5856',  // Regex injection
];

/**
 * Microsoft Code Analysis security rule categories and number ranges
 * Based on official Microsoft documentation
 */
const CA_SECURITY_RANGES = [
  // CA21xx - Security rules (legacy)
  { min: 2100, max: 2199, category: 'Legacy Security' },
  // CA30xx - Security review rules (injection vulnerabilities)  
  { min: 3000, max: 3099, category: 'Security Review' },
  // CA31xx - Security misconfiguration
  { min: 3100, max: 3199, category: 'Security Configuration' },
  // CA53xx-CA54xx - Cryptography and security protocols
  { min: 5300, max: 5499, category: 'Cryptography & Protocols' }
];

/**
 * Check if a rule is a critical accessibility rule
 * @param {string} ruleId - The rule identifier
 * @returns {boolean} - True if it's a critical accessibility rule
 */
function isCriticalAccessibilityRule(ruleId) {
  return CRITICAL_ACCESSIBILITY_RULES.includes(ruleId);
}

/**
 * Check if a rule is a critical security rule
 * @param {string} ruleId - The rule identifier
 * @returns {boolean} - True if it's a critical security rule
 */
function isCriticalSecurityRule(ruleId) {
  return CRITICAL_SECURITY_RULES.some(rule => ruleId === rule || ruleId.startsWith('security/'));
}

/**
 * Check if a .NET rule is a security-related rule
 * @param {string} ruleId - The rule identifier (e.g., 'S2083', 'CA1234')
 * @returns {boolean} - True if it's a .NET security rule
 */
function isDotNetSecurityRule(ruleId) {
  // SonarAnalyzer security rules (explicit list)
  if (DOTNET_SECURITY_RULES.includes(ruleId)) {
    return true;
  }
  
  // Microsoft CA security rules (category-based detection)
  if (ruleId.startsWith('CA')) {
    const ruleNumber = parseInt(ruleId.substring(2));
    if (!isNaN(ruleNumber)) {
      return CA_SECURITY_RANGES.some(range => 
        ruleNumber >= range.min && ruleNumber <= range.max
      );
    }
  }
  
  return false;
}

/**
 * Check if a rule is an accessibility-related rule (critical or supportive)
 * @param {string} ruleId - The rule identifier
 * @returns {boolean} - True if it's any accessibility rule
 */
function isAccessibilityRule(ruleId) {
  return ruleId.startsWith('a11y/') || ruleId.startsWith('vuejs-accessibility/');
}

/**
 * Categorize a linting issue based on its rule
 * @param {string} ruleId - The rule identifier
 * @returns {string} - Category: 'critical-accessibility', 'critical-security', 'accessibility-warning', 'quality'
 */
function categorizeRule(ruleId) {
  if (isCriticalAccessibilityRule(ruleId)) {
    return 'critical-accessibility';
  }
  if (isCriticalSecurityRule(ruleId)) {
    return 'critical-security';
  }
  if (isAccessibilityRule(ruleId)) {
    return 'accessibility-warning';
  }
  return 'quality';
}

module.exports = {
  CRITICAL_ACCESSIBILITY_RULES,
  CRITICAL_SECURITY_RULES,
  DOTNET_SECURITY_RULES,
  CA_SECURITY_RANGES,
  isCriticalAccessibilityRule,
  isCriticalSecurityRule,
  isDotNetSecurityRule,
  isAccessibilityRule,
  categorizeRule
};
