#!/usr/bin/env node
/* eslint-env node */

import { spawn } from 'child_process';
import chokidar from 'chokidar';
import { debounce } from 'lodash-es';

// Try to import tree-kill for better process cleanup, fallback to basic kill
let treeKill = null;
try {
  const treeKillModule = await import('tree-kill');
  treeKill = treeKillModule.default || treeKillModule;
} catch (err) {
  console.warn('tree-kill not available, using basic process termination');
}

let buildProcess = null;

// Helper function to safely kill process trees
function killProcess(childProcess, callback) {
  if (!childProcess) {
    if (callback) callback();
    return;
  }
  
  if (treeKill) {
    treeKill(childProcess.pid, 'SIGTERM', callback);
  } else {
    // Fallback for basic process termination
    try {
      childProcess.kill('SIGTERM');
      if (callback) callback();
    } catch (err) {
      console.warn('Failed to kill process:', err.message);
      if (callback) callback();
    }
  }
}

// Debounced build function to prevent multiple rapid builds
const debouncedBuild = debounce(() => {
  if (buildProcess) {
    killProcess(buildProcess, () => {
      startBuild();
    });
  } else {
    startBuild();
  }
}, 500); // Wait 500ms after last change

// Function to start the build process
function startBuild() {
  console.log('Building Vue app...');
  buildProcess = spawn('npm', ['run', 'build-dev'], {
    stdio: 'inherit',
    shell: true
  });
  
  buildProcess.on('error', (err) => {
    console.error('Failed to start build process:', err.message);
    buildProcess = null;
  });
  
  buildProcess.on('exit', (code) => {
    if (code === 0) {
      console.log('Build completed successfully');
    } else {
      console.error('Build failed with code:', code);
    }
    buildProcess = null;
  });
}

// Watch for changes with error handling
let watcher;
try {
  watcher = chokidar.watch('./src/**/*.{vue,ts,js,css,scss,sass}', {
    ignored: /node_modules/,
    persistent: true
  });
} catch (err) {
  console.error('Failed to initialize file watcher:', err.message);
  process.exit(1);
}

watcher
  .on('change', (path) => {
    console.log(`File changed: ${path}`);
    debouncedBuild();
  })
  .on('add', (path) => {
    console.log(`File added: ${path}`);
    debouncedBuild();
  })
  .on('unlink', (path) => {
    console.log(`File deleted: ${path}`);
    debouncedBuild();
  });

console.log('Watching for changes...');

// Initial build
debouncedBuild();

// Graceful shutdown function
function gracefulShutdown(signal) {
  console.log(`Received ${signal}, shutting down watcher...`);
  if (buildProcess) {
    killProcess(buildProcess, () => {
      if (watcher) watcher.close();
      process.exit(0);
    });
  } else {
    if (watcher) watcher.close();
    process.exit(0);
  }
}

// Handle multiple shutdown signals for robust cleanup
process.on('SIGINT', () => gracefulShutdown('SIGINT'));
process.on('SIGTERM', () => gracefulShutdown('SIGTERM'));
process.on('exit', () => {
  // Final cleanup on process exit (for CI scenarios)
  if (buildProcess) {
    killProcess(buildProcess);
  }
});