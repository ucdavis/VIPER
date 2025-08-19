#!/usr/bin/env node

const { execFileSync } = require('child_process');
const path = require('path');
const {
  parseArguments,
  createSummaryReporter,
  shouldBlockOnWarnings,
  IS_WINDOWS
} = require('./lib/lint-staged-common');

// Platform-specific path patterns
// Regex patterns for robust path classification
const WEB_PATH_REGEX = /(^|[\\\/])web[\\\/]/;
const TEST_PATH_REGEX = /(^|[\\\/])test[\\\/]/;

// Parse command line arguments using shared utility
const { fixFlag, rawFiles } = parseArguments();

// Write debug info to a file only if DEBUG environment variable is set
if (process.env.DEBUG) {
  require('fs').writeFileSync('debug-lint-staged.log', `Files passed to script: ${JSON.stringify(rawFiles, null, 2)}, fixFlag: ${fixFlag}\n`, 'utf8');
}

if (rawFiles.length === 0) {
  console.log('✅ No .cs files staged.');
  process.exit(0);
}

// Separate files by project (web vs test) - handle absolute and relative paths
const webFiles = rawFiles.filter(f => WEB_PATH_REGEX.test(f));
const testFiles = rawFiles.filter(f => TEST_PATH_REGEX.test(f));

// Function to run dotnet format on a specific project
const runFormat = (projectPath, projectFiles) => {
  if (projectFiles.length === 0) return { hasErrors: false, hasWarnings: false };

  // Convert absolute paths to relative paths for dotnet format
  const projectRoot = process.cwd();
  const relativePaths = projectFiles.map(f => {
    // Use path.relative to get project-root-relative paths reliably
    const resolved = path.resolve(f);
    const relative = path.relative(projectRoot, resolved);

    // Verify the file is within the project (security check)
    if (relative.startsWith('..') || path.isAbsolute(relative)) {
      throw new Error(`File outside project directory: ${f}`);
    }

    return relative;
  });

  // Build argument array to avoid shell injection
  const args = ['format'];
  if (!fixFlag) args.push('--verify-no-changes');
  args.push('--severity', 'warn', projectPath);
  for (const p of relativePaths) {
    args.push('--include', p);
  }


  try {
    const result = execFileSync('dotnet', args, {
      encoding: 'utf8',
      timeout: 180000 // 3 minutes should be plenty
    });

    if (fixFlag) {
      // In fix mode, only show output if there were changes
      if (result && result.trim()) {
        console.log(`✅ Fixed formatting issues in ${projectPath}/ files:`);
        console.log(result);
      }
      return { hasErrors: false, hasWarnings: false };
    } else {
      // In check mode, analyze output for errors vs warnings
      const hasWarnings = result && (result.includes('warning ') || result.includes(': warning'));
      const blockOnWarnings = shouldBlockOnWarnings();

      if (hasWarnings) {
        console.log(`\n⚠️  DOTNET FORMAT WARNINGS for ${projectPath}/ - ${blockOnWarnings ? 'BLOCKING' : 'NON-BLOCKING'}:`);
        console.log(result);

        // Don't throw error here - let caller decide based on dual-mode
        return { hasErrors: false, hasWarnings: true };
      }

      return { hasErrors: false, hasWarnings: false };
    }
  } catch (error) {
    // If execSync failed, capture stderr and check for warnings there too
    if (error.stdout || error.stderr) {
      const output = (error.stdout || '') + (error.stderr || '');

      // Check for warnings vs errors in the error output
      const hasWarnings = output.includes('warning ') || output.includes(': warning');
      const hasErrors = output.includes('error ') || output.includes(': error');
      const blockOnWarnings = shouldBlockOnWarnings();

      if (hasErrors && hasWarnings) {
        console.error(`\n❌ DOTNET FORMAT ERRORS for ${projectPath}/:`);
        console.error(output);
        return { hasErrors: true, hasWarnings: true };
      } else if (hasErrors) {
        console.error(`\n❌ DOTNET FORMAT ERRORS for ${projectPath}/:`);
        console.error(output);
        return { hasErrors: true, hasWarnings: false };
      } else if (hasWarnings) {
        console.log(`\n⚠️  DOTNET FORMAT WARNINGS for ${projectPath}/ - ${blockOnWarnings ? 'BLOCKING' : 'NON-BLOCKING'}:`);
        console.log(output);
        return { hasErrors: false, hasWarnings: true };
      }
    }

    // Other types of errors (timeout, etc.) are always blocking
    console.error(`\n❌ DOTNET FORMAT ERROR for ${projectPath}/: ${error.message}`);
    return { hasErrors: true, hasWarnings: false };
  }
};

try {
  // Create shared summary reporter
  const reporter = createSummaryReporter('.NET');

  let hasErrors = false;
  let hasWarnings = false;

  // Run format on web project if there are web files
  if (webFiles.length > 0) {
    const result = runFormat('web', webFiles);
    hasErrors = hasErrors || result.hasErrors;
    hasWarnings = hasWarnings || result.hasWarnings;
  }

  // Run format on test project if there are test files
  if (testFiles.length > 0) {
    const result = runFormat('test', testFiles);
    hasErrors = hasErrors || result.hasErrors;
    hasWarnings = hasWarnings || result.hasWarnings;
  }

  // Use shared summary reporting
  const totalIssues = (hasErrors ? 1 : 0) + (hasWarnings ? 1 : 0);
  const criticalCount = hasErrors ? 1 : 0;
  const warningCount = hasWarnings ? 1 : 0;

  reporter.logSummary(totalIssues, criticalCount, warningCount);

  // Handle critical errors first
  if (hasErrors) {
    console.error('\n🛑 COMMIT BLOCKED due to dotnet format ERRORS.');
    console.error('🔒 Code formatting errors MUST be fixed before committing.');
    process.exit(1);
  }

  // Use shared reporter for warning handling
  reporter.handleCommitDecision(false, hasWarnings);

  // If we get here and no errors, show success message
  if (!hasWarnings) {
    console.log(`\n✅ All C# files ${fixFlag ? 'have been fixed' : 'pass linting checks'}`);
  }

} catch (err) {
  if (err.code === 'ETIMEDOUT') {
    console.error('[ERROR] dotnet format timed out after 3 minutes');
  }
  process.exit(1);
}
