#!/usr/bin/env node

const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Platform-specific constants
const IS_WINDOWS = process.platform === 'win32';

// Check if --fix flag is present
const fixFlag = process.argv.includes('--fix');

// Get the files passed by lint-staged (excluding --fix if present)
const rawFiles = process.argv.slice(2).filter(arg => arg !== '--fix');

if (rawFiles.length === 0) {
  process.exit(0);
}

// VueApp directory path (where ESLint config is located)
const vueAppDir = path.join(__dirname, '..', 'VueApp');

// Sanitize and validate file paths
function sanitizeFilePath(filePath) {
  // Handle Windows paths passed by lint-staged
  let normalizedPath = filePath;

  if (IS_WINDOWS && /^[A-Za-z]:[\\/]/.test(filePath)) {
    normalizedPath = filePath.replace(/\//g, path.sep);
  }

  // Normalize the path
  normalizedPath = path.normalize(normalizedPath);

  // Resolve to absolute path
  const resolvedPath = path.resolve(normalizedPath);
  const projectRoot = path.resolve(__dirname, '..');

  // Ensure file is within project directory using path.relative (more secure than startsWith)
  const relativePath = path.relative(projectRoot, resolvedPath);
  if (relativePath.startsWith('..') || path.isAbsolute(relativePath)) {
    throw new Error(`File outside project directory: ${filePath}`);
  }

  // Ensure it's a .cshtml file
  if (!resolvedPath.toLowerCase().endsWith('.cshtml')) {
    throw new Error(`File is not a .cshtml file: ${filePath}`);
  }

  // Check if file exists
  if (!fs.existsSync(resolvedPath)) {
    return null;
  }

  // Check file size to prevent DoS attacks (limit to 5MB)
  const MAX_FILE_SIZE_MB = 5;
  const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;
  let stats;
  try {
    stats = fs.statSync(resolvedPath);
  } catch (err) {
    return null;
  }
  
  if (stats.size > MAX_FILE_SIZE_BYTES) {
    throw new Error(`File too large (${Math.round(stats.size / 1024 / 1024)}MB > ${MAX_FILE_SIZE_MB}MB): ${filePath}`);
  }

  return resolvedPath;
}

// Sanitize all file paths and filter out null results
const files = rawFiles.map(sanitizeFilePath).filter(file => file !== null);

// Function to analyze ESLint output and separate security vs quality issues
function analyzeESLintOutput(stdout, stderr) {
  const securityErrors = [];
  const qualityIssues = [];
  
  // Parse JSON output from ESLint
  let eslintResults = [];
  try {
    if (stdout.trim()) {
      eslintResults = JSON.parse(stdout);
    }
  } catch (error) {
    // Fallback to stderr if stdout parsing fails
    try {
      if (stderr.trim()) {
        eslintResults = JSON.parse(stderr);
      }
    } catch (fallbackError) {
      console.warn('Failed to parse ESLint JSON output, continuing with empty results');
      return { securityErrors, qualityIssues, output: stdout + stderr };
    }
  }
  
  // Process each file's results
  eslintResults.forEach(fileResult => {
    fileResult.messages.forEach(message => {
      const issue = {
        file: fileResult.filePath,
        line: message.line,
        col: message.column,
        level: message.severity === 2 ? 'error' : 'warning',
        message: message.message,
        rule: message.ruleId || 'unknown'
      };
      
      // Only treat actual security-related rules as security issues
      // Don't use severity level since many non-security rules can be errors
      const isSecurityRule = issue.rule.startsWith('security/') || 
                           issue.rule === 'vue/no-v-html' ||
                           issue.rule === 'vue/no-v-text-v-html-on-component';
      
      if (isSecurityRule) {
        securityErrors.push(issue);
      } else {
        qualityIssues.push(issue);
      }
    });
  });
  
  return { securityErrors, qualityIssues, output: stdout + stderr };
}

try {
  let hasSecurityErrors = false;
  
  // Check if we should block on warnings (for lint:staged vs lint:precommit)
  const blockOnWarnings = process.env.LINT_BLOCK_ON_WARNINGS === 'true';

  if (files.length === 0) {
    console.log('✅ No .cshtml files to check');
    process.exit(0);
  }

  // Run ESLint on .cshtml files using VueApp's config
  const eslintArgs = [
    'eslint',
    '--config', path.join(vueAppDir, '.eslintrc.cjs'),
    ...(fixFlag ? ['--fix'] : []),
    '--format', 'json',
    ...files
  ];

  console.log(`🔍 Running ESLint security and quality checks on ${files.length} .cshtml files...`);
  
  // On Windows, npx needs shell to work properly  
  const eslintResult = spawnSync('npx', eslintArgs, {
    stdio: 'pipe',
    cwd: vueAppDir,
    shell: IS_WINDOWS,
    encoding: 'utf8'
  });

  if (eslintResult.error) {
    console.error('Failed to run ESLint on .cshtml files:', eslintResult.error);
    process.exit(1);
  }
  
  // Check if ESLint command had a fatal error (only if no JSON output produced)  
  if (eslintResult.status !== 0 && eslintResult.status !== 1) {
    // Exit codes 0 = no issues, 1 = linting errors found, >1 = fatal error
    console.error('\n❌ ESLint command failed with exit code:', eslintResult.status);
    if (eslintResult.stdout) console.error('STDOUT:', eslintResult.stdout);
    if (eslintResult.stderr) console.error('STDERR:', eslintResult.stderr);
    console.error('\n🛑 COMMIT BLOCKED - ESLint execution failed');
    process.exit(1);
  }
  
  // Analyze ESLint output for security vs quality issues
  const { securityErrors, qualityIssues } = analyzeESLintOutput(
    eslintResult.stdout, 
    eslintResult.stderr
  );

  // Display results with clear separation
  if (securityErrors.length > 0) {
    console.log(`\n🚨 SECURITY ERRORS (${securityErrors.length}) - BLOCKING COMMIT:`);
    securityErrors.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
    hasSecurityErrors = true;
  }

  if (qualityIssues.length > 0) {
    console.log(`\n⚠️  CODE QUALITY ISSUES (${qualityIssues.length}):`);
    qualityIssues.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (securityErrors.length === 0 && qualityIssues.length === 0) {
    console.log('✅ No ESLint issues found in staged .cshtml files');
  }

  // Summary for developer visibility
  const totalIssues = securityErrors.length + qualityIssues.length;
  console.log(`\n📊 .cshtml Summary: ${totalIssues} total issues (${securityErrors.length} security, ${qualityIssues.length} quality)`);

  // Determine what should block the commit
  const shouldBlock = hasSecurityErrors || (blockOnWarnings && qualityIssues.length > 0);

  if (shouldBlock) {
    if (hasSecurityErrors) {
      console.log('\n🛑 COMMIT BLOCKED due to security errors in .cshtml files.');
      console.log('🔒 Security errors MUST be fixed before committing.');
    }
    if (blockOnWarnings && qualityIssues.length > 0 && !hasSecurityErrors) {
      console.log('\n⚠️  LINTING STOPPED due to code quality issues in .cshtml files.');
      console.log('💡 These issues would not block commits in normal mode. Fix issues above or use lint:precommit to ignore warnings.');
    }
    process.exit(1);
  } else if (qualityIssues.length > 0) {
    console.log('\n✅ COMMIT ALLOWED - Only code quality issues detected in .cshtml files (non-blocking).');
    console.log('💡 Run `npm run lint:staged` to see and fix all warnings.');
  } else {
    console.log('\n✅ All .cshtml checks passed!');
  }

} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}