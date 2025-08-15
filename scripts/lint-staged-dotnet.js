
const { execFileSync } = require('child_process');
const path = require('path');

// Platform-specific constants
const IS_WINDOWS = process.platform === 'win32';
const WEB_PATH_PATTERN = IS_WINDOWS ? '\\web\\' : '/web/';
const TEST_PATH_PATTERN = IS_WINDOWS ? '\\test\\' : '/test/';

// Check if --fix flag is present
const fixFlag = process.argv.includes('--fix');

// Get the files passed by lint-staged (excluding --fix if present)
const files = process.argv.slice(2).filter(arg => arg !== '--fix');

// Write debug info to a file only if DEBUG environment variable is set
if (process.env.DEBUG) {
  require('fs').writeFileSync('debug-lint-staged.log', `Files passed to script: ${JSON.stringify(files, null, 2)}, fixFlag: ${fixFlag}\n`, 'utf8');
}

if (files.length === 0) {
  console.log('✅ No .cs files staged.');
  process.exit(0);
}

// Separate files by project (web vs test) - handle absolute paths
const webFiles = files.filter(f => f.includes(WEB_PATH_PATTERN) || f.includes('/web/'));
const testFiles = files.filter(f => f.includes(TEST_PATH_PATTERN) || f.includes('/test/'));

// Function to run dotnet format on a specific project
const runFormat = (projectPath, projectFiles, blockOnWarnings) => {
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

  // Removed redundant console.log - already shown above

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

      if (hasWarnings) {
        console.log(`\n⚠️  DOTNET FORMAT WARNINGS for ${projectPath}/ - ${blockOnWarnings ? 'BLOCKING COMMIT' : 'NON-BLOCKING'}:`);
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
      const hasErrors = !hasWarnings; // If execSync failed and it's not just warnings, treat as error

      if (hasErrors) {
        console.error(`\n❌ DOTNET FORMAT ERRORS for ${projectPath}/:`);
        console.error(output);
        return { hasErrors: true, hasWarnings: false };
      } else if (hasWarnings) {
        console.log(`\n⚠️  DOTNET FORMAT WARNINGS for ${projectPath}/ - ${blockOnWarnings ? 'BLOCKING COMMIT' : 'NON-BLOCKING'}:`);
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
  // Check if we should block on warnings (for lint:staged vs lint:precommit)
  const blockOnWarnings = process.env.LINT_BLOCK_ON_WARNINGS === 'true';

  let hasErrors = false;
  let hasWarnings = false;

  // Run format on web project if there are web files
  if (webFiles.length > 0) {
    const result = runFormat('web', webFiles, blockOnWarnings);
    hasErrors = hasErrors || result.hasErrors;
    hasWarnings = hasWarnings || result.hasWarnings;
  }

  // Run format on test project if there are test files
  if (testFiles.length > 0) {
    const result = runFormat('test', testFiles, blockOnWarnings);
    hasErrors = hasErrors || result.hasErrors;
    hasWarnings = hasWarnings || result.hasWarnings;
  }

  // Summary and blocking logic
  if (hasErrors || (blockOnWarnings && hasWarnings)) {
    if (hasErrors) {
      console.error('\n🛑 COMMIT BLOCKED due to dotnet format ERRORS.');
      console.error('🔒 Code formatting errors MUST be fixed before committing.');
    } else if (blockOnWarnings && hasWarnings) {
      console.error('\n⚠️  LINTING STOPPED due to dotnet format warnings.');
      console.error('💡 These warnings would not block commits in normal mode. Fix warnings above or use lint:precommit to ignore warnings.');
    }
    process.exit(1);
  } else if (hasWarnings) {
    console.log('\n✅ COMMIT ALLOWED - Only dotnet format warnings detected (non-blocking).');
    console.log('💡 Run `npm run lint:staged` to see and fix all warnings.');
  } else {
    console.log(`\n✅ All C# files ${fixFlag ? 'have been fixed' : 'pass linting checks'}`);
  }
} catch (err) {
  if (err.code === 'ETIMEDOUT') {
    console.error('[ERROR] dotnet format timed out after 3 minutes');
  }
  process.exit(1);
}
