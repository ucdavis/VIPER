#!/usr/bin/env node

const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

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
  if (process.platform === 'win32' && /^[A-Za-z]:\//.test(filePath)) {
    normalizedPath = filePath.replace(/\//g, path.sep);
  }
  
  // Normalize the path to prevent directory traversal
  normalizedPath = path.normalize(normalizedPath);
  
  // Resolve to absolute path for security checks
  const resolvedPath = path.resolve(normalizedPath);
  const vueAppAbsPath = path.resolve(vueAppDir);
  const projectRoot = path.resolve(__dirname, '..');
  
  // Ensure file is within our project directory
  if (!resolvedPath.startsWith(projectRoot)) {
    throw new Error(`File outside project directory: ${filePath}`);
  }
  
  // Ensure file is within VueApp directory specifically
  if (!resolvedPath.startsWith(vueAppAbsPath)) {
    throw new Error(`File outside VueApp directory: ${filePath}`);
  }
  
  // Ensure it's a Vue, JS, or TS file
  const allowedExtensions = ['.vue', '.js', '.ts', '.jsx', '.tsx'];
  const ext = path.extname(resolvedPath).toLowerCase();
  if (!allowedExtensions.includes(ext)) {
    throw new Error(`File type not allowed: ${filePath}`);
  }
  
  // Return relative path for eslint (relative to VueApp directory)
  // Use forward slashes for ESLint compatibility across platforms
  const relativePath = path.relative(vueAppDir, resolvedPath).replace(/\\/g, '/');
  return relativePath;
}

// Sanitize all file paths
const files = rawFiles.map(sanitizeFilePath);

// Helper function to run a command
function runCommand(command, args, description) {
  const npxCommand = 'npx';
  const useShell = process.platform === 'win32';
  
  console.log(`Running ${description}...`);
  
  const result = spawnSync(npxCommand, [command, ...args], { 
    stdio: 'inherit',
    cwd: vueAppDir,
    shell: useShell
  });
  
  if (result.error) {
    console.error(`Failed to run ${description}:`, result.error);
    return false;
  }
  
  if (result.status !== 0) {
    console.error(`${description} failed with exit code ${result.status}`);
    return false;
  }
  
  console.log(`${description} passed.`);
  return true;
}

try {
  let success = true;
  
  // 1. Run ESLint
  const eslintArgs = [
    ...(fixFlag ? ['--fix'] : []),
    '--ignore-path', '.gitignore',
    '--max-warnings', '0', // Treat warnings as errors
    ...files
  ];
  
  if (!runCommand('eslint', eslintArgs, 'ESLint')) {
    success = false;
  }
  
  // 2. Run TypeScript type checking on the files
  // Filter to only .ts, .tsx, .vue files that need type checking
  const tsFiles = files.filter(file => /\.(ts|tsx|vue)$/.test(file));
  
  if (tsFiles.length > 0) {
    // Use vue-tsc for type checking since this is a Vue project
    const tscArgs = ['--noEmit', ...tsFiles];
    
    if (!runCommand('vue-tsc', tscArgs, 'TypeScript type checking')) {
      success = false;
    }
  }
  
  if (!success) {
    process.exit(1);
  }
  
} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}