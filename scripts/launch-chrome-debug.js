const { spawn, execSync } = require('child_process');
const path = require('path');

// Windows-only script for Chrome debugging support

// Check if Chrome is running with debugging port 9222
function isChromeDebuggingRunning() {
    try {
        // Check if port 9222 is listening using Windows netstat
        const output = execSync('netstat -ano | findstr :9222', { encoding: 'utf8' });
        return output.includes('LISTENING');
    } catch (error) {
        return false;
    }
}

// Launch Chrome with debugging if not already running
async function ensureChromeDebugging() {
    if (isChromeDebuggingRunning()) {
        console.log('Chrome debugging is already running on port 9222');
        return true;
    }
    
    console.log('Chrome debugging not found, launching Chrome with debugging enabled...');
    
    // Launch Chrome with debugging using the launch-open-browser script
    try {
        const browserScript = path.join(__dirname, 'launch-open-browser.js');
        const child = spawn('node', [browserScript], {
            detached: true,
            stdio: 'inherit'
        });
        // Allow parent to exit without waiting for the detached child
        if (typeof child.unref === 'function') child.unref();
        
        // Wait a moment for Chrome to start
        await new Promise(resolve => setTimeout(resolve, 3000));
        
        // Check if Chrome debugging is now available
        if (isChromeDebuggingRunning()) {
            console.log('Chrome debugging launched successfully');
            return true;
        } else {
            console.log('Warning: Chrome may still be starting up');
            return true; // Return true anyway, let VS Code handle the attachment
        }
    } catch (error) {
        console.error('Failed to launch Chrome with debugging:', error.message);
        return false;
    }
}

// Main function
async function main() {
    const success = await ensureChromeDebugging();
    process.exit(success ? 0 : 1);
}

main();