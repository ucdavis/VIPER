
const { execFileSync } = require('child_process');

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

console.log(`${fixFlag ? 'Fixing' : 'Checking'} ${files.length} .cs files with dotnet format`);
if (files.length === 0) {
  console.log('No .cs files staged.');
  process.exit(0);
}

// Separate files by project (web vs test) - handle absolute paths
const webFiles = files.filter(f => f.includes(WEB_PATH_PATTERN) || f.includes('/web/'));
const testFiles = files.filter(f => f.includes(TEST_PATH_PATTERN) || f.includes('/test/'));

console.log('Web files:', webFiles);
console.log('Test files:', testFiles);

// Function to run dotnet format on a specific project
const runFormat = (projectPath, projectFiles) => {
  if (projectFiles.length === 0) return;
  
  // Convert absolute paths to relative paths for dotnet format
  const relativePaths = projectFiles.map(f => {
    // Extract the relative path from the absolute path
    const match = f.match(/VIPER2[/\\](.+)/);
    return match ? match[1] : f;
  });
  
  // Build argument array to avoid shell injection
  const args = ['format'];
  if (!fixFlag) args.push('--verify-no-changes');
  args.push('--severity', 'warn', projectPath);
  for (const p of relativePaths) {
    args.push('--include', p);
  }
  
  console.log(`${fixFlag ? 'Fixing' : 'Checking'} ${projectFiles.length} files in ${projectPath}/`);
  
  try {
    const result = execFileSync('dotnet', args, {
      encoding: 'utf8',
      timeout: 180000, // 3 minutes should be plenty
      shell: IS_WINDOWS
    });
    
    if (fixFlag) {
      // In fix mode, just show the output and succeed
      console.log(`[OK] Fixed issues in ${projectPath}/ files`);
      if (result && result.trim()) {
        console.log(result);
      }
    } else {
      // In check mode, fail if there are warnings
      if (result && (result.includes('warning ') || result.includes(': warning'))) {
        console.error('[ERROR] Code analysis warnings found:');
        console.error(result);
        throw new Error('Code analysis warnings detected');
      }
    }
  } catch (error) {
    // If execSync failed, capture stderr and check for warnings there too
    if (error.stdout || error.stderr) {
      const output = (error.stdout || '') + (error.stderr || '');
      console.error('[ERROR] Code analysis issues found:');
      console.error(output);
      
      // Check for warnings in the error output as well
      if (output.includes('warning ') || output.includes(': warning')) {
        console.error('[ERROR] Code analysis warnings detected - failing lint check');
      }
      
      // Re-throw to fail the process
      throw error;
    }
    throw error;
  }
};

try {
  // Run format on web project if there are web files
  runFormat('web', webFiles);
  
  // Run format on test project if there are test files  
  runFormat('test', testFiles);
  
  console.log(`[OK] All C# files ${fixFlag ? 'have been fixed' : 'pass linting checks'}`);
} catch (err) {
  if (err.code === 'ETIMEDOUT') {
    console.error('[ERROR] dotnet format timed out after 3 minutes');
  }
  process.exit(1);
}
