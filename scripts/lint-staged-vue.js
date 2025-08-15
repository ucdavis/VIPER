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

// VueApp directory path
const vueAppDir = path.join(__dirname, '..', 'VueApp');

// Sanitize and validate file paths
function sanitizeFilePath(filePath) {
  // Handle Windows paths passed by lint-staged - convert forward slashes to proper format
  let normalizedPath = filePath;

  // On Windows, lint-staged may pass paths like C:/path/to/file
  // Convert these to proper Windows format first
  if (IS_WINDOWS && /^[A-Za-z]:[\\/]/.test(filePath)) {
    normalizedPath = filePath.replace(/\//g, path.sep);
  }

  // Normalize the path to prevent directory traversal
  normalizedPath = path.normalize(normalizedPath);

  // Resolve to absolute path for security checks
  const resolvedPath = path.resolve(normalizedPath);
  const vueAppAbsPath = path.resolve(vueAppDir);

  // Ensure file is within VueApp directory using path.relative (more secure than startsWith)
  const relativeToVueApp = path.relative(vueAppAbsPath, resolvedPath);
  if (relativeToVueApp.startsWith('..') || path.isAbsolute(relativeToVueApp)) {
    throw new Error(`File outside VueApp directory: ${filePath}`);
  }

  // Ensure it's a Vue, JS, TS, JSX, or TSX file
  const allowedExtensions = ['.vue', '.js', '.ts', '.jsx', '.tsx'];
  const ext = path.extname(resolvedPath).toLowerCase();
  if (!allowedExtensions.includes(ext)) {
    throw new Error(`File type not allowed: ${filePath}`);
  }

  // If the file was removed between staging and lint run, treat as non-existent.
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
    // Treat unexpected stat failures as non-existent to avoid blocking commits.
    return null;
  }
  
  if (stats.size > MAX_FILE_SIZE_BYTES) {
    throw new Error(`File too large (${Math.round(stats.size / 1024 / 1024)}MB > ${MAX_FILE_SIZE_MB}MB): ${filePath}`);
  }

  // Return relative path for eslint (relative to VueApp directory)
  // Use forward slashes for ESLint compatibility across platforms
  const relativePath = path.posix.normalize(path.relative(vueAppDir, resolvedPath));
  return relativePath;
}

// Sanitize all file paths and filter out null results (missing files)
const files = rawFiles.map(sanitizeFilePath).filter(file => file !== null);

// Helper function to run a command and capture output
function runCommand(command, args, description) {
  console.log(`Running ${description}...`);

  // On Windows, npx needs shell to work properly
  const useShell = IS_WINDOWS;
  
  const result = spawnSync('npx', [command, ...args], {
    stdio: ['inherit', 'pipe', 'pipe'],
    cwd: vueAppDir,
    shell: useShell,
    encoding: 'utf8'
  });

  if (result.error) {
    console.error(`Failed to run ${description}:`, result.error);
    return { success: false, stdout: '', stderr: '' };
  }

  return {
    success: result.status === 0,
    stdout: result.stdout || '',
    stderr: result.stderr || '',
    status: result.status
  };
}

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
      
      // Separate security rules from quality issues
      // Security rules start with 'security/' or are Vue security rules
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
  let hasTypeErrors = false;
  
  // Check if we should block on warnings (for lint:staged vs lint:precommit)
  const blockOnWarnings = process.env.LINT_BLOCK_ON_WARNINGS === 'true';

  // 1. Run ESLint (without --max-warnings to get all output)
  const eslintArgs = [
    ...(fixFlag ? ['--fix'] : []),
    '--ignore-path', '.gitignore',
    '--format', 'json', // Use JSON format for reliable parsing
    ...files
  ];

  console.log(`🔍 Running ESLint security and quality checks on ${files.length} Vue/JS/TS files...`);
  const eslintResult = runCommand('eslint', eslintArgs, 'ESLint');
  
  // Check if ESLint command had a fatal error (only if exit code > 1)
  if (eslintResult.status > 1) {
    // Exit codes 0 = no issues, 1 = linting errors found, >1 = fatal error
    console.error('\n❌ ESLint command failed:');
    if (eslintResult.stdout) console.error(eslintResult.stdout);
    if (eslintResult.stderr) console.error(eslintResult.stderr);
    console.error('\n🛑 COMMIT BLOCKED - ESLint execution failed');
    process.exit(1);
  }
  
  // Analyze ESLint output for security vs quality issues
  const { securityErrors, qualityIssues, output } = analyzeESLintOutput(
    eslintResult.stdout, 
    eslintResult.stderr
  );

  // Display results with clear separation
  if (securityErrors.length > 0) {
    console.log(`\n🚨 CRITICAL ERRORS (${securityErrors.length}) - MUST FIX:`);
    securityErrors.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
    hasSecurityErrors = true;
  }

  if (qualityIssues.length > 0) {
    console.log(`\n⚠️  CODE QUALITY WARNINGS (${qualityIssues.length}):`);
    qualityIssues.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (securityErrors.length === 0 && qualityIssues.length === 0) {
    console.log('✅ No ESLint issues found in staged Vue/JS/TS files');
  }

  // 2. Run TypeScript type checking on the files using tsc-files
  // Filter to only .ts, .tsx, .vue files that need type checking
  const tsFiles = files.filter(file => /\.(ts|tsx|vue)$/.test(file));

  if (tsFiles.length > 0) {
    // Convert relative paths back to absolute paths for tsc-files
    const absoluteTsFiles = tsFiles.map(file => path.resolve(vueAppDir, file));

    // Use tsc-files for type checking only the staged files
    const tscArgs = ['--noEmit', ...absoluteTsFiles];

    console.log('\n🔍 Running TypeScript type checking...');
    const tscResult = runCommand('tsc-files', tscArgs, 'TypeScript type checking');
    
    if (!tscResult.success) {
      console.log('\n❌ TypeScript type checking failed:');
      console.log(tscResult.stdout);
      console.log(tscResult.stderr);
      hasTypeErrors = true;
    } else {
      console.log('✅ TypeScript type checking passed');
    }
  }

  // Summary for developer visibility 
  const totalIssues = securityErrors.length + qualityIssues.length;
  const criticalCount = securityErrors.length;
  const warningCount = qualityIssues.length;
  
  console.log(`\n📊 Vue/JS/TS Summary: ${totalIssues} total issues (${criticalCount} critical errors, ${warningCount} warnings)`);

  // Determine what should block the commit
  const shouldBlock = hasSecurityErrors || hasTypeErrors || (blockOnWarnings && qualityIssues.length > 0);

  if (shouldBlock) {
    if (hasSecurityErrors || hasTypeErrors) {
      console.log('\n🛑 COMMIT BLOCKED due to CRITICAL ERRORS.');
      console.log('🔒 Security and type errors MUST be fixed before committing.');
    }
    if (blockOnWarnings && qualityIssues.length > 0 && !hasSecurityErrors && !hasTypeErrors) {
      console.log('\n⚠️  LINTING STOPPED due to code quality warnings.');
      console.log('💡 These warnings would not block commits in normal mode. Fix warnings above or use lint:precommit to ignore warnings.');
    }
    process.exit(1);
  } else if (qualityIssues.length > 0) {
    console.log('\n✅ COMMIT ALLOWED - Only warnings detected (non-blocking).');
    console.log('💡 Run `npm run lint:staged` to see and fix all warnings.');
  } else {
    console.log('\n✅ All Vue/JS/TS checks passed!');
  }

} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}