const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Platform-specific constants
const IS_WINDOWS = process.platform === 'win32';

/**
 * Parse command line arguments for lint-staged scripts
 * @returns {Object} { fixFlag: boolean, files: string[] }
 */
function parseArguments() {
  const knownFlags = new Set(['--fix']);
  const args = process.argv.slice(2);
  const fixFlag = args.includes('--fix');
  const rawFiles = args.filter(a => !knownFlags.has(a) && !a.startsWith('--'));

  return { fixFlag, rawFiles };
}

/**
 * Sanitize and validate file paths with security checks
 * @param {string} filePath - Raw file path from lint-staged
 * @param {string} baseDir - Base directory to validate against
 * @param {string[]} allowedExtensions - Array of allowed file extensions (e.g., ['.css', '.vue'])
 * @param {number} maxFileSizeMB - Maximum file size in MB (default: 5)
 * @returns {string|null} - Relative path for linting tools, or null if file should be skipped
 */
function sanitizeFilePath(filePath, baseDir, allowedExtensions, maxFileSizeMB = 5) {
  // Handle Windows paths passed by lint-staged - convert forward slashes to proper format
  let normalizedPath = filePath;

  // On Windows, lint-staged may pass paths like C:/path/to/file
  // Convert these to proper Windows format first
  if (IS_WINDOWS && /^[A-Za-z]:[\\\\/]/.test(filePath)) {
    normalizedPath = filePath.replace(/\//g, path.sep);
  }

  // Normalize the path to prevent directory traversal
  normalizedPath = path.normalize(normalizedPath);

  // Resolve to absolute path for security checks
  const resolvedPath = path.resolve(normalizedPath);
  const baseAbsPath = path.resolve(baseDir);

  // Ensure file is within base directory using path.relative (more secure than startsWith)
  const relativeToBase = path.relative(baseAbsPath, resolvedPath);
  if (relativeToBase.startsWith('..') || path.isAbsolute(relativeToBase)) {
    console.warn(`[lint-skip] Outside base directory: ${filePath}`);
    return null;
  }

  // Ensure it's an allowed file type
  const ext = path.extname(resolvedPath).toLowerCase();
  if (!allowedExtensions.includes(ext)) {
    console.warn(`[lint-skip] Disallowed extension: ${filePath}`);
    return null;
  }

  // If the file was removed between staging and lint run, treat as non-existent.
  if (!fs.existsSync(resolvedPath)) {
    return null;
  }

  // Check file size to prevent DoS attacks
  const MAX_FILE_SIZE_BYTES = maxFileSizeMB * 1024 * 1024;
  let stats;
  try {
    stats = fs.statSync(resolvedPath);
  } catch (err) {
    // Treat unexpected stat failures as non-existent to avoid blocking commits.
    return null;
  }

  if (stats.size > MAX_FILE_SIZE_BYTES) {
    throw new Error(`File too large (${Math.round(stats.size / 1024 / 1024)}MB > ${maxFileSizeMB}MB): ${filePath}. Large files may cause performance issues during linting. Consider excluding from commit or increasing maxFileSizeMB.`);
  }

  // Return relative path for linting tools (relative to base directory)
  // Use forward slashes for cross-platform compatibility with linting tools
  const relativePath = path.posix.normalize(path.relative(baseAbsPath, resolvedPath));
  return relativePath;
}

/**
 * Securely run a command with local binary resolution
 * @param {string} command - Command name (e.g., 'stylelint', 'eslint')
 * @param {string[]} args - Command arguments
 * @param {string} description - Description for logging
 * @param {string} cwd - Working directory
 * @returns {Object} - { success: boolean, stdout: string, stderr: string, status: number }
 */
function runCommand(command, args, description, cwd) {
  console.log(`Running ${description}...`);

  // Security: Try to resolve local binary first to avoid npx network dependency
  const binName = IS_WINDOWS ? `${command}.cmd` : command;
  const localBin = path.join(cwd, 'node_modules', '.bin', binName);

  let execPath;
  let useShell = false;
  let finalArgs = args;

  if (fs.existsSync(localBin)) {
    execPath = localBin;
    // Windows .cmd files need shell, but use it safely with individual args
    useShell = IS_WINDOWS && localBin.endsWith('.cmd');
  } else {
    execPath = 'npx';
    finalArgs = ['--no-install', command, ...args];
    useShell = false;
  }

  const result = spawnSync(execPath, finalArgs, {
    stdio: ['inherit', 'pipe', 'pipe'],
    cwd: cwd,
    shell: useShell,
    encoding: 'utf8',
    windowsHide: true
  });

  if (result.error) {
    console.error(`Failed to run ${description}:`, result.error);
    return { 
      success: false, 
      stdout: result.stdout || '', 
      stderr: result.stderr || '', 
      status: -1 
    };
  }

  return {
    success: result.status === 0,
    stdout: result.stdout || '',
    stderr: result.stderr || '',
    status: result.status
  };
}

/**
 * Check if linting should block based on environment variable
 * @returns {boolean} - True if warnings should block commits
 */
function shouldBlockOnWarnings() {
  return process.env.LINT_BLOCK_ON_WARNINGS === 'true';
}

/**
 * Create a standardized summary reporter for lint results
 * @param {string} toolName - Name of the linting tool (e.g., 'CSS', 'Vue/JS/TS')
 * @returns {Object} - Reporter functions
 */
function createSummaryReporter(toolName) {
  return {
    /**
     * Log summary of issues found
     * @param {number} totalIssues - Total number of issues
     * @param {number} criticalCount - Number of critical/error issues
     * @param {number} warningCount - Number of warnings
     */
    logSummary(totalIssues, criticalCount, warningCount) {
      console.log(`\n📊 ${toolName} Summary: ${totalIssues} total issues (${criticalCount} critical errors, ${warningCount} warnings)`);
    },

    /**
     * Log commit decision and exit with appropriate code
     * @param {boolean} hasBlockingIssues - Whether there are issues that should block
     * @param {boolean} hasWarnings - Whether there are non-blocking warnings
     * @param {string} blockingReason - Reason for blocking (e.g., 'CRITICAL ERRORS', 'FEDERAL ACCESSIBILITY COMPLIANCE')
     */
    handleCommitDecision(hasBlockingIssues, hasWarnings, blockingReason = 'CRITICAL ERRORS') {
      const blockOnWarnings = shouldBlockOnWarnings();
      const shouldBlock = hasBlockingIssues || (blockOnWarnings && hasWarnings);

      if (shouldBlock) {
        if (hasBlockingIssues) {
          console.log(`\n🛑 COMMIT BLOCKED due to ${blockingReason}.`);
          console.log('🔒 Critical issues MUST be fixed before committing.');
        }
        if (blockOnWarnings && hasWarnings && !hasBlockingIssues) {
          console.log('\n⚠️  LINTING STOPPED due to warnings.');
          console.log('💡 These warnings would not block commits in normal mode. Fix warnings above or use lint:precommit to ignore warnings.');
        }
        process.exit(1);
      } else if (hasWarnings) {
        console.log('\n✅ COMMIT ALLOWED - Only warnings detected (non-blocking).');
        console.log('💡 Consider addressing warnings to improve code quality.');
      } else {
        console.log(`\n✅ All ${toolName} checks passed!`);
      }
    }
  };
}

module.exports = {
  IS_WINDOWS,
  parseArguments,
  sanitizeFilePath,
  runCommand,
  shouldBlockOnWarnings,
  createSummaryReporter
};
