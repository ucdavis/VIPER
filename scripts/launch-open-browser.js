const { spawn } = require('child_process');
const fs = require('fs');
const { getDevServerEnv, openBrowser } = require('./lib/script-utils');

// Cross-platform script for launching browser with debugging

// Get backend port from environment or use default
function getBackendPort() {
    const env = getDevServerEnv();
    
    // Use ASPNETCORE_HTTPS_PORT from env, with fallback to legacy default
    return env.ASPNETCORE_HTTPS_PORT || 7157;
}

// Wait for backend to be ready then open browser
async function waitForBackendAndOpenBrowser() {
    const port = getBackendPort();
    const url = `https://localhost:${port}`;
    
    console.log(`Waiting for backend on port ${port}...`);
    
    // Wait longer for servers to start
    await new Promise(resolve => setTimeout(resolve, 8000));
    
    // Check if backend is ready (simple approach)
    const maxRetries = 60;
    let isReady = false;
    
    for (let retries = 0; retries < maxRetries; retries++) {
        try {
            // Use a simple TCP connection check instead of HTTP request
            const net = require('net');
            
            const isConnected = await new Promise((resolve) => {
                const socket = new net.Socket();
                socket.setTimeout(1000);
                
                socket.on('connect', () => {
                    socket.destroy();
                    resolve(true);
                });
                
                socket.on('error', () => {
                    resolve(false);
                });
                
                socket.on('timeout', () => {
                    socket.destroy();
                    resolve(false);
                });
                
                socket.connect(port, 'localhost');
            });
            
            if (isConnected) {
                isReady = true;
                break;
            }
        } catch (error) {
            // Continue to next retry
        }
        
        // Only log every 10 retries to be less noisy
        if ((retries + 1) % 10 === 0) {
            console.log(`Still waiting for backend... (${retries + 1}/${maxRetries})`);
        }
        
        if (retries < maxRetries - 1) {
            await new Promise(resolve => setTimeout(resolve, 2000));
        }
    }
    
    if (isReady) {
        console.log(`Opening browser at ${url}`);
        
        // Launch Chrome with remote debugging enabled (Windows)
        // Need separate user data dir for debugging when regular Chrome is running
        const chromeArgs = [
            '--remote-debugging-port=9222',
            '--user-data-dir=C:\\temp\\chrome-debug-viper',
            url
        ];
        
        try {
            // Try common Chrome installation paths
            const chromePaths = [
                'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
                'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe'
            ];
            
            let chromeFound = false;
            for (const chromePath of chromePaths) {
                if (fs.existsSync(chromePath)) {
                    const child = spawn(chromePath, chromeArgs, { detached: true });
                    if (typeof child.unref === 'function') child.unref();
                    chromeFound = true;
                    break;
                }
            }
            
            if (!chromeFound) {
                // Fallback to default browser (cross-platform)
                await openBrowser(url);
            }
        } catch (error) {
            // Fallback to default browser (cross-platform)
            await openBrowser(url);
        }
    } else {
        console.log(`Backend failed to start on port ${port} after ${maxRetries} retries`);
    }
}

waitForBackendAndOpenBrowser().catch(console.error);